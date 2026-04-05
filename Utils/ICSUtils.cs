using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using VSuiteLab.Models;

namespace VSuiteLab.Utils;

public class ICSUtils
{
    /// <summary>
    /// Downloads the ICS (iCalendar) content from the specified URI using the provided HttpClient.
    /// </summary>
    /// <param name="client">An instance of <see cref="HttpClient"/> used to make the HTTP request.</param>
    /// <param name="uri">The URI of the ICS file to download.</param>
    /// <returns>
    /// A <see cref="Task{String}"/> containing the raw ICS content as a string.
    /// </returns>
    public async Task<string> DownloadICS(HttpClient client, string uri)
    {
        using var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    
    
    /// <summary>
    /// Builds the ICS (iCalendar) content for the specified CalDavItem.
    /// </summary>
    /// <param name="item">The CalDavItem instance to build the ICS content for.</param>
    /// <returns> A <see cref="Task{String}"/> containing the ICS content as a string. </returns>
    public string BuildICS(CalDavItem item)
    {
        return item switch
        {
            CalDavTask task       => BuildVTodoICS(task),
            CalDavJournal journal => BuildVJournalICS(journal),
            CalDavNote note       => BuildVJournalNoteICS(note),
            _ => throw new NotSupportedException($"Unsupported type: {item.GetType().Name}")
        };
    }
    
    /// <summary>
    ///  Parses the provided ICS content and returns a CalDavItem instance.
    /// </summary>
    /// <param name="ics">The raw ICS content to parse as a string.</param>
    /// <returns> A <see cref="Task{CalDavItem}"/> containing the parsed CalDavItem instance. </returns>
    public CalDavItem? ParseICS(string ics)
    {
        var calendar = Calendar.Load(ics);

        if (calendar.Todos?.Any() == true)
            return ParseICSVTodo(calendar);

        if (calendar.Journals?.Any() == true)
            return ParseICSVJournal(calendar);

        return null;
    }
    
    
    private CalDavTask ParseICSVTodo(Calendar ics)
    {
        var note = new CalDavTask();
        
        var vTodo = ics?.Todos.FirstOrDefault();
        if (vTodo is null)
            return new CalDavTask();
       
        note.Uid = vTodo.Uid ?? Guid.NewGuid().ToString();
        note.LastModified = vTodo.LastModified?.AsUtc;
        note.StartDate = vTodo.Start?.Value;
        note.DueDate = vTodo.Due?.Value;
        note.CompletedDate = vTodo.Completed?.Value;
        note.Priority = vTodo.Priority;
        note.Classification = vTodo.Class ?? string.Empty;
        note.Sequence =  vTodo.Sequence;
       
        note.Summary = vTodo.Summary ?? string.Empty;
        note.Description = vTodo.Description ?? string.Empty;
        note.Status = vTodo.Status switch
        {
            Ical.Net.TodoStatus.Completed => Models.TodoStatus.Completed,
            Ical.Net.TodoStatus.InProcess => Models.TodoStatus.InProgress,
            Ical.Net.TodoStatus.Cancelled => Models.TodoStatus.Cancelled,
            _ => Models.TodoStatus.NeedsAction
        };
        note.Location = vTodo.Location ?? string.Empty;
        note.Url = vTodo.Url?.ToString() ?? string.Empty;

        note.Contact = vTodo.Organizer?.CommonName ?? string.Empty;

        foreach (var attendee in vTodo.Attendees)
        {
            note.Attendees.Add(new CalDavAttendee
            {
                Name = attendee.CommonName ?? string.Empty, Email = attendee.Members.ToString(), Role = attendee.Role ?? string.Empty
            });
        }
        foreach (var category in vTodo.Categories)
        {
            note.Categories.Add(new CalDavCategory { Value = category});
        }
        foreach (var alarm in vTodo.Alarms)
        {
            DateTimeOffset? triggerDate = null;

            if (alarm.Trigger?.DateTime != null)
            {
                triggerDate = alarm.Trigger.DateTime.AsUtc;
            }
            else if (alarm.Trigger?.Duration != null)
            {
                var reference = vTodo.Start?.Value ?? DateTime.UtcNow;
                var duration = alarm.Trigger.Duration.Value.ToTimeSpan(new CalDateTime(reference));
                var triggerTime = reference + duration;
                triggerDate = new DateTimeOffset(triggerTime, TimeSpan.Zero);
            }

            if (triggerDate.HasValue)
            {
                note.Alarms.Add(new CalDavAlarm
                {
                    Action = alarm.Action,
                    Description = alarm.Description ?? string.Empty,
                    Summary = alarm.Summary ?? string.Empty,
                    SelectedDate = triggerDate,
                    Repeat = alarm.Repeat
                });
            }
        }
        foreach (var attachment in vTodo.Attachments)
        {
            note.Attachments.Add(new CalDavAttachment {Uri = attachment.Data ?? Array.Empty<byte>(), Title = attachment.Parameters.Get("FILENAME"), ContentType = attachment.FormatType ?? string.Empty});
        }
        foreach (var comment in vTodo.Comments)
        {
            note.Comments.Add(new CalDavComment {Value = comment});
        }
       
        return note;
    }


    private string BuildVTodoICS(CalDavTask item)
    {
        var vTodoCalendar = new Calendar();

        var vTodo = new Todo
        {
            Uid = string.IsNullOrWhiteSpace(item.Uid)
                ? Guid.NewGuid().ToString()
                : item.Uid,
            Summary = string.IsNullOrWhiteSpace(item.Summary) 
                ? null 
                : item.Summary,
            Description = string.IsNullOrWhiteSpace(item.Description) 
                ? null 
                : item.Description,
            LastModified = new CalDateTime(item.LastModified?.UtcDateTime ?? DateTime.UtcNow),
            Start = new CalDateTime(item.StartDate?.UtcDateTime ?? DateTime.UtcNow),
            Due = new CalDateTime(item.DueDate?.UtcDateTime ?? DateTime.UtcNow),
            Status = item.Status switch
            {
                Models.TodoStatus.Completed => Ical.Net.TodoStatus.Completed,
                Models.TodoStatus.InProgress => Ical.Net.TodoStatus.InProcess,
                Models.TodoStatus.Cancelled => Ical.Net.TodoStatus.Cancelled,
                _ => Ical.Net.TodoStatus.NeedsAction
            },
            Priority = item.Priority,
            DtStamp = new CalDateTime(DateTime.UtcNow),
            Sequence = item.Sequence
        };

        if (!string.IsNullOrWhiteSpace(item.Contact))
            vTodo.Organizer = new Organizer()
            {
                CommonName = item.Contact,

            };

        if (!string.IsNullOrWhiteSpace(item.Classification))
            vTodo.Class = item.Classification;

        if (!string.IsNullOrWhiteSpace(item.Location))
            vTodo.Location = item.Location;
        
        if (!string.IsNullOrWhiteSpace(item.Url)) vTodo.Url = new Uri(item.Url);
        
        if (item.Status == Models.TodoStatus.Completed)
        {
            vTodo.Completed = new CalDateTime(item.CompletedDate?.UtcDateTime ?? DateTime.UtcNow);
        }

        foreach (var attendee in item.Attendees)
            vTodo.Attendees.Add(new Attendee(new Uri($"mailto:{attendee.Email}")) { CommonName = attendee.Name, Role = attendee.Role });

        foreach (var category in item.Categories)
            vTodo.Categories.Add(category.Value);

        foreach (var alarm in item.Alarms)
        {
            if (!alarm.SelectedDate.HasValue) continue;

            var trigger = new Trigger
            {
                DateTime = new CalDateTime(alarm.SelectedDate.Value.DateTime),
            };
            trigger.Parameters.Add("VALUE", "DATE-TIME");

            vTodo.Alarms.Add(new Alarm
            {
                Action = alarm.Action,
                Description = alarm.Description,
                Summary = alarm.Summary,
                Trigger = trigger,
                Repeat = alarm.Repeat ?? 0
            });
        }

        foreach (var attachment in item.Attachments)
        {
            var icalAttachment = new Attachment(attachment.Uri)
            {
                FormatType = attachment.ContentType,
            };

            icalAttachment.Parameters.Add("ENCODING", "BASE64");
            icalAttachment.Parameters.Add("FILENAME", attachment.Title);
            icalAttachment.Parameters.Add("VALUE", "BINARY");
            icalAttachment.Parameters.Add("X-LABEL", attachment.Title);

            vTodo.Attachments.Add(icalAttachment);
        }

        foreach (var comment in item.Comments)
            vTodo.Comments.Add(comment.Value);

        vTodoCalendar.Todos.Add(vTodo);

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(vTodoCalendar);
    }
    
    
    private string BuildVJournalICS(CalDavJournal item)
    {
        var calendar = new Calendar();

        var journal = new Journal
        {
            Uid = item.Uid ?? Guid.NewGuid().ToString(),
            Summary = item.Summary,
            Description = item.Description,
            DtStamp = new CalDateTime(DateTime.UtcNow),
            DtStart = new CalDateTime(item.PublishedDate?.UtcDateTime ?? DateTime.UtcNow),
            LastModified = new CalDateTime(item.LastModified?.UtcDateTime ?? DateTime.UtcNow),
        };
        journal.Status = item.Status switch
        {
            Models.TodoStatus.Completed => Ical.Net.TodoStatus.Completed,
            Models.TodoStatus.InProgress => Ical.Net.TodoStatus.InProcess,
            Models.TodoStatus.Cancelled => Ical.Net.TodoStatus.Cancelled,
            _ => Ical.Net.TodoStatus.NeedsAction
        };
        
        
        if (!string.IsNullOrWhiteSpace(item.Contact))
            journal.Organizer = new Organizer()
            {
                CommonName = item.Contact,

            };

        if (!string.IsNullOrWhiteSpace(item.Classification))
            journal.Class = item.Classification;
        
        if (!string.IsNullOrWhiteSpace(item.Url)) journal.Url = new Uri(item.Url);
        
        foreach (var attendee in item.Attendees)
            journal.Attendees.Add(new Attendee(new Uri($"mailto:{attendee.Email}")) { CommonName = attendee.Name, Role = attendee.Role });
        
        
        ApplyCommonJournalFields(journal, item);

        calendar.Journals.Add(journal);

        return new CalendarSerializer().SerializeToString(calendar);
    }

    private string BuildVJournalNoteICS(CalDavNote item)
    {
        var calendar = new Calendar();

        var journal = new Journal
        {
            Uid = item.Uid ?? Guid.NewGuid().ToString(),
            Summary = item.Summary,
            Description = item.Description,
            LastModified = new CalDateTime(item.LastModified?.UtcDateTime ?? DateTime.UtcNow),
            
        };
        journal.Status = item.Status switch
        {
            Models.TodoStatus.Completed => Ical.Net.TodoStatus.Completed,
            Models.TodoStatus.InProgress => Ical.Net.TodoStatus.InProcess,
            Models.TodoStatus.Cancelled => Ical.Net.TodoStatus.Cancelled,
            _ => Ical.Net.TodoStatus.NeedsAction
        };
        
        ApplyCommonJournalFields(journal, item);
        
        calendar.Journals.Add(journal);

        return new CalendarSerializer().SerializeToString(calendar);
    }

    private void ApplyCommonJournalFields<T>(Journal journal, T item)
        where T : CalDavItem
    {
        if (item is not CalDavJournal && item is not CalDavNote)
            return;

        dynamic target = item;
        
        if (!string.IsNullOrWhiteSpace(target.Contact))
            journal.Organizer = new Organizer()
            {
                CommonName = target.Contact,

            };

        if (!string.IsNullOrWhiteSpace(target.Classification))
            journal.Class = target.classification;
        
        if (!string.IsNullOrWhiteSpace(target.Url)) journal.Url = new Uri(target.Url);

        foreach (var c in target.Categories)
            journal.Categories.Add(c.Value);

        foreach (var c in target.Comments)
            journal.Comments.Add(c.Value);

        foreach (var a in target.Attachments)
        {
            var att = new Attachment(a.Uri)
            {
                FormatType = a.ContentType
            };

            if (!string.IsNullOrWhiteSpace(a.Title))
                att.Parameters.Add("FILENAME", a.Title);

            journal.Attachments.Add(att);
        }

        foreach (var a in target.Attendees)
        {
            if (string.IsNullOrWhiteSpace(a.Email)) continue;

            journal.Attendees.Add(new Attendee(new Uri($"mailto:{a.Email}"))
            {
                CommonName = a.Name,
                Role = a.Role
            });
        }
        
        foreach (var alarm in target.Alarms)
        {
            if (!alarm.SelectedDate.HasValue) continue;

            var trigger = new Trigger
            {
                DateTime = new CalDateTime(alarm.SelectedDate.Value.DateTime),
            };
            trigger.Parameters.Add("VALUE", "DATE-TIME");

            journal.Alarms.Add(new Alarm
            {
                Action = alarm.Action,
                Description = alarm.Description,
                Summary = alarm.Summary,
                Trigger = trigger,
                Repeat = alarm.Repeat ?? 0
            });
        }
    }
    
    /// <summary>
    /// Builds the XML request body for a sync collection request.
    /// </summary>
    /// <param name="syncToken">The last synchronization token received from the server, or null for the initial fetch.</param>
    /// <returns>
    /// A <see cref="String"/> containing the raw XML request content as a string.
    /// </returns>
    public string BuildSyncCollectionXml(string syncToken)
    {
        var xml = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
            <D:sync-collection xmlns:D=""DAV:"">
              {(string.IsNullOrEmpty(syncToken) 
                  ? "" 
                  : $"<D:sync-token>{syncToken}</D:sync-token>")}
              <D:sync-level>1</D:sync-level>
              <D:prop>
                <D:getetag/>
                <D:getcontenttype/>
              </D:prop>
            </D:sync-collection>";
        return xml;
    }

    /// <summary>
    /// Parses the Sync Collection response XML and extracts the synchronization token,
    /// changed resources, and deleted resources based on the specified vCalendar type.
    /// </summary>
    /// <param name="response">An <see cref="XDocument"/> representing the Sync Collection XML response.</param>
    /// <param name="vCalTYpe">A string specifying the vCalendar type (e.g., "VTODO", "VEVENT").</param>
    /// <returns>
    /// A <see cref="SyncCollectionResult"/> containing the parsed synchronization token,
    /// a list of changed resources, and a list of deleted resources.
    /// </returns>
    public SyncCollectionResult ParseSyncCollectionResponse(XDocument response, string vCalTYpe)
    {
        var result = new SyncCollectionResult();

        XNamespace dav = "DAV:";

        var root = response.Root;
        if (root == null)
            return result; 
        
        result.SyncToken = root.Element(dav + "sync-token")?.Value;
        
        foreach (var responseElement in root.Elements(dav + "response"))
        {
            var href = responseElement.Element(dav + "href")?.Value;
            if (string.IsNullOrWhiteSpace(href))
                continue;
            
            var topStatus = responseElement.Element(dav + "status");
            if (topStatus != null && topStatus.Value.Contains("404"))
            {
                result.DeletedResources.Add(href);
                continue;
            }
            
            foreach (var propstat in responseElement.Elements(dav + "propstat"))
            {
                var status = propstat.Element(dav + "status")?.Value;
                if (status == null || !status.Contains("200"))
                    continue;

                var prop = propstat.Element(dav + "prop");
                if (prop == null)
                    continue;
                
                var contentType = prop.Element(dav + "getcontenttype")?.Value;
                if (contentType == null ||
                    !contentType.Contains($"component={vCalTYpe}",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var etag = prop.Element(dav + "getetag")?.Value;

                result.ChangedResources.Add(new SyncItem
                {
                    Uri = href,
                    Etag = etag,
                    ContentType = contentType
                });
            }
        }

        return result;
    }
    
    private void PopulateCommonJournalFields(
        Journal j,
        dynamic target) // works for both Journal + Note
    {
        foreach (var cat in j.Categories)
            target.Categories.Add(new CalDavCategory { Value = cat });

        foreach (var c in j.Comments)
            target.Comments.Add(new CalDavComment { Value = c });

        foreach (var a in j.Attachments)
        {
            target.Attachments.Add(new CalDavAttachment
            {
                Uri = a.Data ?? Array.Empty<byte>(),
                Title = a.Parameters.Get("FILENAME"),
                ContentType = a.FormatType ?? string.Empty
            });
        }

        foreach (var a in j.Attendees)
        {
            target.Attendees.Add(new CalDavAttendee
            {
                Name = a.CommonName ?? string.Empty,
                Email = a.Value?.ToString(),
                Role = a.Role ?? string.Empty
            });
        }

        foreach (var alarm in j.Alarms)
        {
            DateTimeOffset? triggerDate = null;
            if (alarm.Trigger?.DateTime != null)
            {
                triggerDate = alarm.Trigger.DateTime.AsUtc;
            }
            else if (alarm.Trigger?.Duration != null)
            {
                var reference = j.DtStart?.Value ?? DateTime.UtcNow;
                var duration = alarm.Trigger.Duration.Value.ToTimeSpan(new CalDateTime(reference));
                var triggerTime = reference + duration;
                triggerDate = new DateTimeOffset(triggerTime, TimeSpan.Zero);
            }
            if (triggerDate.HasValue)
            {
                target.Alarms.Add(new CalDavAlarm
                {
                    Action = alarm.Action,
                    Description = alarm.Description ?? string.Empty,
                    Summary = alarm.Summary ?? string.Empty,
                    SelectedDate = triggerDate,
                    Repeat = alarm.Repeat
                });
            }
        }
        
        if (!string.IsNullOrWhiteSpace(target.Contact))
            j.Organizer = new Organizer()
            {
                CommonName = target.Contact,

            };

        if (!string.IsNullOrWhiteSpace(target.Classification))
            j.Class = target.Classification;
        
        if (!string.IsNullOrWhiteSpace(target.Url)) j.Url = new Uri(target.Url);
    }
    
    private CalDavItem ParseICSVJournal(Calendar ics)
    {
        var vJournal = ics?.Journals.FirstOrDefault();
        if (vJournal is null)
            return new CalDavJournal();

        return vJournal.DtStart != null
            ? ParseVJournalCalDavJournal(vJournal)
            : ParseVJournalCalDavNote(vJournal);
    }
    
        
    private CalDavJournal ParseVJournalCalDavJournal(Journal j)
    {
        var journal = new CalDavJournal
        {
            Uid = j.Uid ?? Guid.NewGuid().ToString(),
            Summary = j.Summary ?? string.Empty,
            Description = j.Description ?? string.Empty,
            PublishedDate = j.DtStart?.Value,
            LastModified = j.LastModified?.Value,
            Url = j.Url?.ToString() ?? string.Empty,
            Classification = j.Class ?? string.Empty,
        };
        journal.Status = j.Status switch
        {
            Ical.Net.TodoStatus.Completed => Models.TodoStatus.Completed,
            Ical.Net.TodoStatus.InProcess => Models.TodoStatus.InProgress,
            Ical.Net.TodoStatus.Cancelled => Models.TodoStatus.Cancelled,
            _ => Models.TodoStatus.NeedsAction
        };
        
        PopulateCommonJournalFields(j, journal);

        return journal;
    }
    
    private CalDavNote ParseVJournalCalDavNote(Journal j)
    {
        var note = new CalDavNote
        {
            Uid = j.Uid ?? Guid.NewGuid().ToString(),
            Summary = j.Summary ?? string.Empty,
            Description = j.Description ?? string.Empty,
            LastModified = j.LastModified?.Value,
            Url = j.Url?.ToString() ?? string.Empty,
            Classification = j.Class ?? string.Empty,
        };
        note.Status = j.Status switch
        {
            Ical.Net.TodoStatus.Completed => Models.TodoStatus.Completed,
            Ical.Net.TodoStatus.InProcess => Models.TodoStatus.InProgress,
            Ical.Net.TodoStatus.Cancelled => Models.TodoStatus.Cancelled,
            _ => Models.TodoStatus.NeedsAction
        };
        
        PopulateCommonJournalFields(j, note);

        return note;
    }
    

}