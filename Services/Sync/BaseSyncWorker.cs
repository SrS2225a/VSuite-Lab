using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Utils;

namespace VSuiteLab.Services.Sync;

/// <summary>
/// Serves as a foundational class for handling data synchronization between a local database
/// and a remote DAV server. This class is responsible for overseeing the communication with the
/// server, managing synchronization processes, and coordinating the push of local changes as well as
/// the pull of updates from the server.
/// </summary>
public class BaseSyncWorker
{
    private readonly IcsUtils _icsUtils;
    private readonly DatabaseService _databaseService;

    public BaseSyncWorker()
    {
        _databaseService = new DatabaseService();
        _icsUtils = new IcsUtils();
    }

    /// <summary>
    /// Creates and returns an HttpClient instance for DAV-related operations if a network connection is available.
    /// </summary>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <returns>A <see cref="VSuiteLab.Models.Helpers.StatusResponse{T}"/></returns>
    private async Task<StatusResponse<HttpClient>> GetDavClientWithNetworkAsync(DavConfig config)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return new StatusResponse<HttpClient>()
            {
                Success = false,
                Message = "No network connection available"
            };

        // Wrap potentially blocking synchronous call
        var client = DavMiddlewareService.GetDavClient(config);
        return StatusResponse<HttpClient>.Ok(client);
    }
    
    /// <summary>
    /// Executes the synchronization process for a given DAV configuration.
    /// </summary>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <param name="message">A <see cref="VSuiteLab.Models.Helpers.SyncProgress"/> instance to update with progress information.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while awaiting the tasks.</param>
    public async Task ExecuteAsync(DavConfig config, SyncProgress message, CancellationToken cancellationToken)
    {
        try
        {
            // main sync loop
            message.Update("Connecting to server...");
            var clientResponse = await GetDavClientWithNetworkAsync(config);
            if (!clientResponse.Success)
            {
                message.Update(clientResponse.Message);
                message.Complete(false);
                return;
            }

            var client = clientResponse.Value;
            var pushActions = new List<Func<Task>>();
            var pullActions = new List<Func<XDocument, Task>>();

            if (config.SupportsVtodo)
            {
                pushActions.Add(async () =>
                {
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
                });

                pullActions.Add(async report =>
                {
                    var syncResult = _icsUtils.ParseSyncCollectionResponse(report, "VTODO");

                    message.Update("Downloading tasks...");
                    await PullVTodoItems(
                        _icsUtils,
                        client,
                        syncResult,
                        config,
                        cancellationToken);
                });
            }

            if (config.SupportsVjournal)
            {
                pushActions.Add(async () =>
                {
                    if (await HasLocalChanges<CalDavJournal>(config.Id))
                    {
                        message.Update("Pushing journals...");
                        await PushLocalChanges<CalDavJournal>(
                            db => db.Journals,
                            _icsUtils,
                            client,
                            config,
                            cancellationToken);
                    }

                    if (await HasLocalChanges<CalDavNote>(config.Id))
                    {
                        message.Update("Pushing notes...");
                        await PushLocalChanges<CalDavNote>(
                            db => db.Notes,
                            _icsUtils,
                            client,
                            config,
                            cancellationToken);
                    }
                });

                pullActions.Add(async report =>
                {
                    var syncResult = _icsUtils.ParseSyncCollectionResponse(report, "VJOURNAL");

                    message.Update("Downloading journals...");
                    await PullVJournalItems(
                        _icsUtils,
                        client,
                        syncResult,
                        config,
                        cancellationToken);
                });
            }

            message.Update("Pushing local changes...");
            foreach (var action in pushActions)
                await action();

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

            var report = reportResponse.Value;
            foreach (var action in pullActions)
                await action(report!);

            message.Update("Completed");
            message.Complete(true);
        } catch (Exception ex)
        {
            message.Update(ex.Message);
            message.Complete(false);
        }
    }

    /// <summary>
    /// Determines if there are any local changes for the specified configuration
    /// </summary>
    /// <typeparam name="T">The type of the entity to check for local changes.</typeparam>
    /// <param name="configId">The ID of the configuration to check for local changes.</param>
    /// <returns>A <see cref="Task{Boolean}"/> indicating whether there are any local changes.</returns>
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
    /// <typeparam name="T">The type of the entity to push.</typeparam>
    /// <param name="setSelector">A function that selects the DbSet for the specified entity type.</param>
    /// <param name="icsUtils">Utility class for handling ICS data generation and parsing.</param>
    /// <param name="client">The HTTP client used to interact with the remote server.</param>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <param name="token">A cancellation token to observe while awaiting the tasks.</param>
    private async Task PushLocalChanges<T>(
        Func<DatabaseContext, DbSet<T>> setSelector,
        IcsUtils icsUtils,
        HttpClient? client,
        DavConfig config,
        CancellationToken token)
        where T : CalDavItem
    {
        await using var db = new DatabaseContext();

        var dbSet = setSelector(db);

        var query = dbSet
            .Include(e => EF.Property<object>(e, "Categories"))
            .Include(e => EF.Property<object>(e, "Attachments"))
            .Include(e => EF.Property<object>(e, "Comments"))
            .Include(e => EF.Property<object>(e, "Alarms"))
            .Include(e => EF.Property<object>(e, "Attendees"))
            .Where(x => x.DavConfigId == config.Id && (x.IsDirty || x.IsDeleted));

        var dirtyItems = await query.ToListAsync(token);

        if (dirtyItems.Count == 0)
            return;

        var semaphore = new SemaphoreSlim(3);

        var tasks = dirtyItems.Select(async item =>
        {
            await semaphore.WaitAsync(token);
            try
            {
                if (item.PreviousDavConfigId != null && item.PreviousDavConfigId != config.Id)
                {
                    var oldConfig = await db.DavConfigs
                        .FirstOrDefaultAsync(c => c.Id == item.PreviousDavConfigId, token);
                    
                    if (oldConfig != null)
                    {
                        var oldClient = DavMiddlewareService.GetDavClient(oldConfig);
                        await DavMiddlewareService.DeleteRemoteItem(oldClient, item, token);
                    }
                    
                    
                    var fullUri = new Uri(
                        new Uri(config?.httpUrl!),
                        $"{item.Id}.ics");
                    item.Uri = fullUri.ToString();
                    item.Uid = Guid.NewGuid().ToString();
                    item.PreviousDavConfigId = null;
                    dbSet.Update(item);
                }
                
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
        IcsUtils icsUtils,
        HttpClient? client,
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

        var existingByUri = existingNotes
            .GroupBy(n => n.Uri)
            .ToDictionary(g => g.Key, g => g.First());
        
        var deletedSet = syncResult.DeletedResources.ToHashSet();
        db.Tasks.RemoveRange(existingNotes.Where(x => x.Uri != null && deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));

        // Process changed/added resources
        foreach (var resource in syncResult.ChangedResources)
        {
            token.ThrowIfCancellationRequested();

            var remoteUri = new Uri(client?.BaseAddress!, resource.Uri).ToString();
            var remoteETag = resource.Etag;
            
            if ((existingByUri.TryGetValue(remoteUri, out var existing) && existing.Etag == remoteETag) || string.IsNullOrEmpty(remoteUri))
                continue;

            var ics = await icsUtils.DownloadIcs(client, remoteUri);
            var parsed = icsUtils.ParseIcs(ics);
            
            if (parsed is not CalDavTask task)
                continue;

            if (remoteETag != null) parsed.Etag = remoteETag;
            parsed.Uri = remoteUri;
            parsed.DavConfigId = config.Id;
            
            if (existing != null)
            {
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

    /// <summary>
    /// Pulls any VJOURNAL remote changes from the server
    /// </summary>
    /// <param name="icsUtils">Utility class for handling ICS data generation and parsing.</param>
    /// <param name="client">The HTTP client used to interact with the remote server.</param>
    /// <param name="syncResult">The result of the synchronization report containing  parsed synchronization token,
    /// a list of changed resources, and a list of deleted resources.</param>
    /// <param name="config">The DAV configuration containing connection details and credentials.</param>
    /// <param name="token">A cancellation token to observe while awaiting the tasks.</param>
    private async Task PullVJournalItems(
        IcsUtils icsUtils,
        HttpClient? client,
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
        db.Journals.RemoveRange(existingJournals.Where(x => x.Uri != null && deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));
        db.Notes.RemoveRange(existingNotes.Where(x => x.Uri != null && deletedSet.Contains(x.Uri) && x.DavConfigId == config.Id));

        foreach (var resource in syncResult.ChangedResources)
        {
            token.ThrowIfCancellationRequested();

            var remoteUri = new Uri(client?.BaseAddress!, resource.Uri).ToString();
            var remoteETag = resource.Etag;
            
            if ((journalsByUri.TryGetValue(remoteUri, out var existingJournal) && existingJournal.Etag == remoteETag) ||
                (notesByUri.TryGetValue(remoteUri, out var existingNote) && existingNote.Etag == remoteETag) || string.IsNullOrEmpty(remoteUri))
            {
                continue;
            }

            var ics = await icsUtils.DownloadIcs(client, remoteUri);
            var parsedItem = icsUtils.ParseIcs(ics);
            

            if (parsedItem == null) continue;

            parsedItem.Uri = remoteUri;
            parsedItem.Etag = remoteETag;
            parsedItem.DavConfigId = config.Id;

            if (parsedItem is CalDavJournal journal)
            {
                if (existingJournal != null)
                {
                    journal.Id = existingJournal.Id;
                    db.Entry(existingJournal).CurrentValues.SetValues(journal);
                }
                else
                {
                    db.Journals.Add(journal);
                }
            }
            else if (parsedItem is CalDavNote note)
            {
                if (existingNote != null)
                {
                    note.Id = existingNote.Id;
                    db.Entry(existingNote).CurrentValues.SetValues(note);
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