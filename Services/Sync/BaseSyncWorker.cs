using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Utils;

namespace VSuiteLab.Services;

public class BaseSyncWorker
{
    private readonly ICSUtils _icsUtils = new();
    private readonly DatabaseService _databaseService = new();
    
    public BaseSyncWorker()
    {
        _databaseService = new DatabaseService();
        _icsUtils = new ICSUtils();
    }
    
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
        var client = DavMiddlewareService.getDavClient(config);
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
                client, _icsUtils);

            if (!reportResponse.Success)
            {
                message.Update(reportResponse.Message);
                message.Complete(false);
                return;
            }

            if (config.SupportsVtodo)
            {
                var syncResult = _icsUtils.ParseSyncCollectionResponse(reportResponse.Value, "VTODO");

                message.Update("Downloading tasks...");
                await PullVTodoItems(_icsUtils, client, syncResult, config, cancellationToken);

                if (await HasLocalChanges<CalDavTask>(config.Id))
                {
                    message.Update("Pushing tasks...");
                    await PushLocalChanges<CalDavTask>(
                        db => db.Tasks,
                        _icsUtils,
                        client,
                        config,
                        cancellationToken);
                }
            }

            if (config.SupportsVjournal)
            {
                var syncResult = _icsUtils.ParseSyncCollectionResponse(reportResponse.Value, "VJOURNAL");

                message.Update("Downloading journals...");
                await PullVJournalItems(_icsUtils, client, syncResult, config, cancellationToken);

                message.Update("Pushing journals...");
                if (await HasLocalChanges<CalDavJournal>(config.Id))
                {

                    await PushLocalChanges<CalDavJournal>(
                        db => db.Journals,
                        _icsUtils,
                        client,
                        config,
                        cancellationToken);
                }

                if (await HasLocalChanges<CalDavNote>(config.Id))
                {
                    await PushLocalChanges<CalDavNote>(
                        db => db.Notes,
                        _icsUtils,
                        client,
                        config,
                        cancellationToken);
                }
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
    private async Task<bool> HasLocalChanges<T>(Guid configId)
        where T : CalDavItem
    {
        
        var result = await _databaseService.ReadWhereAsync<T>(x => x.DavConfigId == configId &&
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
    private async Task PushLocalChanges<T>(
        Func<DatabaseContext, DbSet<T>> setSelector,
        ICSUtils icsUtils,
        HttpClient client,
        DavConfig config,
        CancellationToken token)
        where T : CalDavItem
    {
        await using var db = new DatabaseContext();

        var dbSet = setSelector(db);

        var dirtyItems = await dbSet
            .Where(x => x.DavConfigId == config.Id &&
                        (x.IsDirty || x.IsDeleted))
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
                    dbSet.Remove(item);
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

        var existingByUri = existingNotes.ToDictionary(n => n.Uri);
        
        var deletedSet = syncResult.DeletedResources.ToHashSet();
        db.Tasks.RemoveRange(existingNotes.Where(x => deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));

        // Process changed/added resources
        foreach (var resource in syncResult.ChangedResources)
        {
            token.ThrowIfCancellationRequested();

            var remoteUri = new Uri(client.BaseAddress!, resource.Uri).ToString();
            var remoteETag = resource.Etag;

            var ics = await icsUtils.DownloadICS(client, remoteUri);
            var parsed = icsUtils.ParseICS(ics, false);

            parsed.Etag = remoteETag;
            parsed.Uri = remoteUri;
            parsed.DavConfigId = config.Id;

            if (parsed is not CalDavTask task) continue;
            if (existingByUri.TryGetValue(remoteUri, out var existing))
            {
                if (existing.Etag == remoteETag)
                {
                    continue;
                }

                parsed.Id = existing.Id;
                db.Entry(existing).CurrentValues.SetValues(parsed);

                existing.Comments = new ObservableCollection<CalDavComment>(task.Comments);
                existing.Alarms = new ObservableCollection<CalDavAlarm>(task.Alarms);
                existing.Categories = new ObservableCollection<CalDavCategory>(task.Categories);
                existing.Attendees = new ObservableCollection<CalDavAttendee>(task.Attendees);
                existing.Attachments = new ObservableCollection<CalDavAttachment>(task.Attachments);
            }
            else
            {
                db.Tasks.Add(task);
            }
        }

        // Update last sync token
        await db.DavConfigs
            .Where(c => c.Id == config.Id)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.LastSyncToken, syncResult.SyncToken),
                token);

        // Save all changes in one batch
        await db.SaveChangesAsync();
    }

    private async Task PullVJournalItems(
        ICSUtils icsUtils,
        HttpClient client,
        SyncCollectionResult syncResult,
        DavConfig config,
        CancellationToken token)
    {
        await using var db = new DatabaseContext();

        var existingJournals = await db.Journals.Where(x => x.DavConfigId == config.Id).ToListAsync(token);
        var existingNotes = await db.Notes.Where(x => x.DavConfigId == config.Id).ToListAsync(token);

        var journalsByUri = existingJournals.ToDictionary(x => x.Uri);
        var notesByUri = existingNotes.ToDictionary(x => x.Uri);

        var deletedSet = syncResult.DeletedResources.ToHashSet();
        db.Journals.RemoveRange(existingJournals.Where(x => deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));
        db.Notes.RemoveRange(existingNotes.Where(x => deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));

        foreach (var resource in syncResult.ChangedResources)
        {
            token.ThrowIfCancellationRequested();

            var remoteUri = new Uri(client.BaseAddress!, resource.Uri).ToString();
            var remoteETag = resource.Etag;

            var ics = await icsUtils.DownloadICS(client, remoteUri);
            var parsedItem = icsUtils.ParseICS(ics, true);

            if (parsedItem == null) continue;

            parsedItem.Uri = remoteUri;
            parsedItem.Etag = remoteETag;
            parsedItem.DavConfigId = config.Id;

            if (parsedItem is CalDavJournal journal)
            {
                if (journalsByUri.TryGetValue(remoteUri, out var existing))
                {
                    if (existing.Etag == remoteETag) continue;
                    journal.Id = existing.Id;
                    db.Entry(existing).CurrentValues.SetValues(journal);
                }
                else
                {
                    db.Journals.Add(journal);
                }
            }
            else if (parsedItem is CalDavNote note)
            {
                if (notesByUri.TryGetValue(remoteUri, out var existing))
                {
                    if (existing.Etag == remoteETag) continue;
                    note.Id = existing.Id;
                    db.Entry(existing).CurrentValues.SetValues(note);
                }
                else
                {
                    db.Notes.Add(note);
                }
            }
        }

        await db.SaveChangesAsync(token);
    }
}