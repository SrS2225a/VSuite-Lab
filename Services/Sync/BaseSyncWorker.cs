using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Utils;

namespace VSuiteLab.Services;

public class BaseSyncWorker
{
    /// <summary>
    /// Creates and returns an HttpClient instance for DAV-related operations if a network connection is available.
    /// </summary>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <returns>A <see cref="StatusResponse{T}"/></returns>
    private async Task<StatusResponse<HttpClient>> GetDavClientWithNetworkAsync(DavConfig config)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return new StatusResponse<HttpClient>()
            {
                Success = false,
                Message = "No network connection available"
            };

        // Wrap potentially blocking synchronous call
        var client = await Task.Run(() => DavMiddlewareService.getDavClient(config));
        return StatusResponse<HttpClient>.Ok(client);
    }

    /// <summary>
    /// Executes the synchronization process by connecting to the server
    /// </summary>
    /// <param name="config">The configuration containing DAV-related settings, such as connection details and credentials.</param>
    /// <param name="message">The banner message instance used to update the user interface with the progress of the synchronization.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests and stop the execution if requested.</param>
    public async Task ExecuteAsync(DavConfig config, SyncProgress message, CancellationToken cancellationToken)
    {
        try
        {
            ICSUtils ICSUtils = new();
            
            message.Update("Connecting to server...");
            var clientResponse = await GetDavClientWithNetworkAsync(config);
            if (!clientResponse.Success)
            {
                message.Update(clientResponse.Message);
                message.Complete(false);
                return;
            }

            var client = clientResponse.Value;
            
            message.Update("Checking for server changes...");
            
            var reportResponse = await DavMiddlewareService.SyncCollectionReportAsync(
                config,
                config.LastSyncToken,
                client,
                ICSUtils);

            if (!reportResponse.Success)
            {
                message.Update(reportResponse.Message);
                message.Complete(false);
                return;
            }
            
            if (config.SupportsVtodo)
            {
                var syncResult = ICSUtils.ParseSyncCollectionResponse(reportResponse.Value, "VTODO");

                message.Update("Downloading tasks...");
                await PullVTodoItems(ICSUtils, client, syncResult, config, cancellationToken);
            }

            // if (config.SupportsVjournal)
            //     await ProcessVJournalItems(ICSUtils, client, syncResult, config, cancellationToken);
            
            if (await HasLocalChanges(config.Id))
            {
                message.Update("Uploading tasks...");
                await PushVTodoLocalChanges(ICSUtils, client, config, cancellationToken);
            }


            message.Update("Completed");
            message.Complete(true);
        }
        catch (Exception ex)
        {
            message.Update(ex.Message);
            message.Complete(false);
        }
    }

    /// <summary>
    /// Determines if there are any local changes for the specified configuration
    /// </summary>
    /// <param name="configId">The Dav configuration to check for local changes.</param>
    private async Task<bool> HasLocalChanges(Guid configId)
    {
        var DatabaseService = new DatabaseService();
        var result = await DatabaseService.ReadWhereAsync<CalDavTask>(
            x => x.DavConfigId == configId &&
                 (x.IsDirty || x.IsDeleted));
        
        return result.Success && result.Value != null && result.Value.Any();
    }

    /// <summary>
    /// Pushes any VTODO local changes to the server
    /// </summary>
    /// <param name="icsUtils">Utility class for handling ICS data generation and parsing.</param>
    /// <param name="client">The HTTP client used to interact with the remote server.</param>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <param name="token">A cancellation token to observe while awaiting the tasks.</param>
    private async Task PushVTodoLocalChanges(
        ICSUtils icsUtils,
        HttpClient client,
        DavConfig config,
        CancellationToken token)
    {
        await using var db = new DatabaseContext();

        var dirtyItems = await db.Tasks
            .Where(x =>
                x.DavConfigId == config.Id &&
                (x.IsDirty || x.IsDeleted))
            .Include(x => x.Alarms)
            .Include(x => x.Categories)
            .Include(x => x.Attendees)
            .Include(x => x.Attachments)
            .Include(x => x.Comments)
            .AsSplitQuery()
            .ToListAsync(token);

        if (dirtyItems.Count == 0)
            return;

        var semaphore = new SemaphoreSlim(3);

        var tasks = dirtyItems.Select(async item =>
        {
            await semaphore.WaitAsync(token);

            try
            {
                if (item.IsDeleted)
                {
                    await DavMiddlewareService.DeleteRemoteItem(client, item, token);

                    db.Tasks.Remove(item);
                    return;
                }
                
                await DavMiddlewareService.UploadOrUpdateRemoteItem(
                    db,
                    icsUtils,
                    client,
                    item,
                    token);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        await db.SaveChangesAsync(token);
    }
    
    /// <summary>
    /// Pulls any VTODO remote changes from the server
    /// </summary>
    /// <param name="icsUtils">Utility class for handling ICS data generation and parsing.</param>
    /// <param name="client">The HTTP client used to interact with the remote server.</param>
    /// <param name="syncResult">The result of the synchronization report containing  parsed synchronization token,
    /// a list of changed resources, and a list of deleted resources.</param>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <param name="token">A cancellation token to observe while awaiting the tasks.</param>
    private async Task PullVTodoItems(
        ICSUtils icsUtils,
        HttpClient client,
        SyncCollectionResult syncResult,
        DavConfig config,
        CancellationToken token)
    {
        await using var db = new DatabaseContext();

        var existingNotes = await db.Tasks
            .Include(n => n.Alarms)
            .Include(n => n.Categories)
            .Include(n => n.Attendees)
            .Include(n => n.Attachments)
            .Include(n => n.Comments)
            .AsSplitQuery()
            .Where(n => n.DavConfigId == config.Id)
            .ToListAsync(token);

        var existingByUri = existingNotes.ToDictionary(n => n.uriUrl);

        var deleted = await db.Tasks
            .Where(n => syncResult.DeletedResources.Contains(n.uriUrl)
                        && n.DavConfigId == config.Id)
            .ToListAsync(token);
        
        // Delete any deleted resources
        if (deleted.Count > 0)
        {
            db.Tasks.RemoveRange(deleted);
        }

        // Process changed/added resources
        foreach (var resource in syncResult.ChangedResources)
        {
            token.ThrowIfCancellationRequested();

            var remoteUri = new Uri(client.BaseAddress!, resource.Uri).ToString();
            var remoteETag = resource.Etag;

            var ics = await icsUtils.DownloadICS(client, remoteUri);
            var parsed = icsUtils.ParseICSVTodo(ics);

            parsed.Etag = remoteETag;
            parsed.uriUrl = remoteUri;
            parsed.DavConfigId = config.Id;

            if (existingByUri.TryGetValue(remoteUri, out var existing))
            {
                if (existing.Etag == remoteETag)
                {
                    continue;
                }

                parsed.Id = existing.Id;
                db.Entry(existing).CurrentValues.SetValues(parsed);

                // Replaces children cleanly
                existing.Comments.Clear();
                foreach (var c in parsed.Comments)
                    existing.Comments.Add(c);

                existing.Categories.Clear();
                foreach (var c in parsed.Categories)
                    existing.Categories.Add(c);

                existing.Alarms.Clear();
                foreach (var a in parsed.Alarms)
                    existing.Alarms.Add(a);

                existing.Attendees.Clear();
                foreach (var a in parsed.Attendees)
                    existing.Attendees.Add(a);

                existing.Attachments.Clear();
                foreach (var a in parsed.Attachments)
                    existing.Attachments.Add(a);
            }
            else
            {
                db.Tasks.Add(parsed);
            }
        }

        // Update last sync token
        config.LastSyncToken = syncResult.SyncToken;

        // Save all changes in one batch
        await db.SaveChangesAsync();
    }
}