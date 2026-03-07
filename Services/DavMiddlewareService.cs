using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VSuiteLab.Models;
using VSuiteLab.Utils;

namespace VSuiteLab.Services;

public class DavMiddlewareService
{
    /// <summary>
    /// Creates and returns an HTTP client configured with the specified DAV server settings,
    /// including the base address and authentication credentials.
    /// </summary>
    /// <param name="davConfig">The configuration details of the DAV server, including the HTTP URL and credentials.</param>
    /// <returns>An instance of <see cref="HttpClient"/> configured for interacting with the DAV server.</returns>
    public static HttpClient getDavClient(DavConfig davConfig)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(
                davConfig.username,
                davConfig.password),
            AllowAutoRedirect = true
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(davConfig.httpUrl)
        };

        return client;
    }

    /// <summary>
    /// Determines if the specified DAV server supports CalDAV by performing a PROPFIND request
    /// on the provided resource URL and evaluating the response.
    /// </summary>
    /// <param name="client">The HTTP client used to communicate with the DAV server.</param>
    /// <param name="davConfig">The configuration details of the DAV server, including the resource URL to inspect.</param>
    /// <returns>A status response indicating success or failure, along with an optional message.</returns>
    public static async Task<StatusResponse<string>> hasCalDav(HttpClient client, DavConfig davConfig)
    {
        var statusMessage = new StatusResponse<string>();
        try
        {
            // Perform a PROPFIND request to check for CalDAV support (use the calendar folder path)
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), davConfig.httpUrl);
            request.Headers.Add("Depth", "0");
            request.Content = new StringContent(@"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <D:propfind xmlns:D=""DAV:"">
                    <D:prop>
                        <D:resourcetype/>
                    </D:prop>
                </D:propfind>",
                Encoding.UTF8,
                "application/xml");
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusResponse<string>.Error(
                    $"PROPFIND failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);

            XNamespace dav = "DAV:";
            XNamespace cal = "urn:ietf:params:xml:ns:caldav";

            var resourceTypes = doc.Descendants(dav + "resourcetype");

            foreach (var rt in resourceTypes)
            {
                var isCollection = rt.Element(dav + "collection") != null;
                var isCalendar = rt.Element(cal + "calendar") != null;

                if (isCollection && isCalendar)
                {
                    return StatusResponse<string>.Ok(davConfig.httpUrl);
                }
            }

            return StatusResponse<string>.Error(
                "The specified resource is not a CalDAV calendar collection.");
        }
        catch (Exception ex)
        {
            statusMessage = new StatusResponse<string>
            {
                Message = $"An error occurred: {ex.Message}",
                Success = false,
                Value = null
            };
        }

        return statusMessage;
    }

    /// <summary>
    /// Validates the DAV server's features by performing a PROPFIND request to identify support for VTODO and/or VJOURNAL components.
    /// </summary>
    /// <param name="client">An instance of <see cref="HttpClient"/> used to send requests to the DAV server.</param>
    /// <param name="davConfig">The configuration details of the DAV server, including URL and feature support flags.</param>
    /// <returns>A <see cref="StatusResponse{DavConfig}"/> object containing the updated configuration with detected features, or an error status if the validation fails.</returns>
    public static async Task<StatusResponse<DavConfig>> checkDavFeatures(HttpClient client, DavConfig davConfig)
    {
        var statusMessage = new StatusResponse<DavConfig>();
        try
        {
            // Perform a PROPFIND request to check for CalDAV support (use the calendar folder path)
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), davConfig.httpUrl);
            request.Headers.Add("Depth", "0");
            request.Content = new StringContent(@"<?xml version=""1.0"" encoding=""utf-8"" ?>
                <D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:caldav"">
                    <D:prop>
                        <C:supported-calendar-component-set/>
                    </D:prop>
                </D:propfind>",
                Encoding.UTF8,
                "application/xml");
            var response = await client.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return StatusResponse<DavConfig>.Error(
                    $"PROPFIND failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var xmlString = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xmlString);
            
            XNamespace cal = "urn:ietf:params:xml:ns:caldav";

            var compElements = doc
                .Descendants(cal + "supported-calendar-component-set")
                .Descendants(cal + "comp");

            var components = compElements
                .Select(e => (string)e.Attribute("name"))
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            davConfig.SupportsVtodo = components.Contains("VTODO");
            davConfig.SupportsVjournal = components.Contains("VJOURNAL");

            return StatusResponse<DavConfig>.Ok(davConfig);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            statusMessage = new StatusResponse<DavConfig>
            {
                Message = $"An error occurred: {ex.Message}",
                Success = false,
                Value = null
            };
        }

        return statusMessage;
    }

    /// <summary>
    /// Sends a REPORT request to the specified CalDAV server to synchronize collections
    /// and returns the parsed XML response document.
    /// </summary>
    /// <param name="config">The configuration details of the CalDAV server.</param>
    /// <param name="syncToken">The last synchronization token received from the server, or null for the initial fetch.</param>
    /// <param name="client">The HTTP client used to send the request to the CalDAV server.</param>
    /// <param name="ICSUtils">An instance of the utility class to generate the REPORT XML payload.</param>
    /// <returns>A status response containing the XML document from the server if successful, or an error message otherwise.</returns>
    public static async Task<StatusResponse<XDocument>> SyncCollectionReportAsync(
        DavConfig config,
        string? syncToken,
        HttpClient client,
        ICSUtils ICSUtils)
    {
        var reportXml = ICSUtils.BuildSyncCollectionXml(syncToken);

        var request = new HttpRequestMessage(new HttpMethod("REPORT"), config.httpUrl);
        request.Content = new StringContent(reportXml, Encoding.UTF8, "application/xml");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return StatusResponse<XDocument>.Error("REPORT failed");
        
        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);

        return StatusResponse<XDocument>.Ok(doc);
    }

    /// <summary>
    /// Uploads or updates a remote calendar item based on its current state.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="icsUtils">The utility class instance used to handle ICS parsing and operations.</param>
    /// <param name="client">The HTTP client used to perform the request to the remote CalDAV server.</param>
    /// <param name="item">The CalDAV task item to be uploaded or updated on the remote server.</param>
    /// <param name="token">The cancellation token that can be used to cancel the operation.</param>
    public static async Task UploadOrUpdateRemoteItem(
        DatabaseContext db,
        ICSUtils icsUtils,
        HttpClient client,
        CalDavTask item,
        CancellationToken token = default)
    {
        item.Sequence++;
        var icsContent = icsUtils.BuildVTodoICS(item);

        using var request = new HttpRequestMessage(HttpMethod.Put, item.uriUrl)
        {
            Content = new StringContent(icsContent, Encoding.UTF8, "text/calendar")
        };

        if (!string.IsNullOrWhiteSpace(item.Etag))
            request.Headers.TryAddWithoutValidation("If-Match", item.Etag);
        else
            request.Headers.TryAddWithoutValidation("If-None-Match", "*");

        using var response = await client.SendAsync(request, token);

        if (response.StatusCode == HttpStatusCode.PreconditionFailed || response.StatusCode == HttpStatusCode.Conflict)
        {
            await ResolveConflict(db, client, item, icsUtils, token);
            return;
        }

        response.EnsureSuccessStatusCode();

        if (response.Headers.ETag != null)
            item.Etag = response.Headers.ETag.Tag;

        if (response.Headers.Location != null)
            item.uriUrl = new Uri(client.BaseAddress!, response.Headers.Location.ToString()).ToString();

        item.IsDirty = false;
    }


    /// <summary>
    /// Resolves conflicts between a local calendar task and its remote counterpart
    /// based on the defined conflict resolution strategy.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="client">The HTTP client used to fetch the remote task from the server.</param>
    /// <param name="localItem">The local CalDav task that potentially conflicts with the remote version.</param>
    /// <param name="icsUtils">The utility class instance used to handle ICS parsing and operations.</param>
    /// <param name="token">A cancellation token to propagate task cancellation notifications.</param>
    private static async Task ResolveConflict(
        DatabaseContext db,
        HttpClient client,
        CalDavTask localItem,
        ICSUtils icsUtils,
        CancellationToken token)
    {
        var settings = db.Settings.FirstOrDefault();
        
        using var response = await client.GetAsync(localItem.uriUrl, token);
        response.EnsureSuccessStatusCode();

        var remoteIcs = await response.Content.ReadAsStringAsync(token);
        var parsed = icsUtils.ParseICSVTodo(remoteIcs);
        
        switch (settings.ConflictStrategy)
        {
            case ConflictStrategy.PreferServer:
                SolveConflictRemoteWins(db, parsed, localItem, response);
                return;
            case ConflictStrategy.PreferClient:
                SolveConflictLocalWins(localItem, icsUtils, client, response);
                return;
            default:
                if (localItem.LastModified > parsed.LastModified)
                {
                    await SolveConflictLocalWins(localItem, icsUtils, client, response);
                }
                else
                {
                    SolveConflictRemoteWins(db, parsed, localItem, response);
                }
                return;
        }
    }
    
    private static async Task SolveConflictLocalWins(CalDavTask localItem, ICSUtils icsUtils, HttpClient client,
        HttpResponseMessage response)
    {
        // Local wins -> overwrite remote tracked entity
        var remoteETag = response.Headers.ETag?.Tag;
        var ics = icsUtils.BuildVTodoICS(localItem);
        using var retry = new HttpRequestMessage(HttpMethod.Put, localItem.uriUrl)
        {
            Content = new StringContent(
                ics,
                Encoding.UTF8,
                "text/calendar")
        };
            
        retry.Headers.TryAddWithoutValidation("If-Match", remoteETag);

        using var retryResponse = await client.SendAsync(retry);
        retryResponse.EnsureSuccessStatusCode();

        localItem.IsDirty = false;
        localItem.Etag = retryResponse.Headers.ETag?.Tag;
    }

    private static void SolveConflictRemoteWins(DatabaseContext db, CalDavTask parsed, CalDavTask localItem, HttpResponseMessage response)
    {
        // Remote wins -> overwrite local tracked entity
        parsed.Id = localItem.Id;
        parsed.DavConfigId = localItem.DavConfigId;
        parsed.IsDirty = false;
        parsed.Etag = response.Headers.ETag?.Tag;

        // Update scalar properties
        db.Entry(localItem).CurrentValues.SetValues(parsed);
        
        localItem.Comments.Clear();
        foreach (var c in parsed.Comments) localItem.Comments.Add(c);

        localItem.Categories.Clear();
        foreach (var c in parsed.Categories) localItem.Categories.Add(c);

        localItem.Alarms.Clear();
        foreach (var a in parsed.Alarms) localItem.Alarms.Add(a);

        localItem.Attendees.Clear();
        foreach (var a in parsed.Attendees) localItem.Attendees.Add(a);

        localItem.Attachments.Clear();
        foreach (var a in parsed.Attachments) localItem.Attachments.Add(a);
    }

    public static async Task DeleteRemoteItem(
        HttpClient client,
        CalDavTask item,
        CancellationToken token = default)
    {
        // Delete remote item
        using var request = new HttpRequestMessage(HttpMethod.Delete, item.uriUrl);
        using var response = await client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
    }
}