using System.Collections.Generic;
using VSuiteLab.Models;
using VSuiteLab.Utils.Query;

namespace VSuiteLab.Utils;

public class QueryValueResolver
{
    public static class JournalSchema
    {
        public static List<QueryFieldDescriptor> Fields => new()
        {
            new() { Path = "Status", Type = QueryFieldType.Enum, Label = "Status", EnumType = typeof(JournalStatus) },
            new() { Path = "Classification", Type = QueryFieldType.Enum, Label = "Classification" },
            new() { Path = "Url", Type = QueryFieldType.Text, Label = "Url" },
            new() { Path = "Contact", Type = QueryFieldType.Text, Label = "Contact" },

            new() { Path = "PublishedDate", Type = QueryFieldType.Date, Label = "Published Date" },
            new() { Path = "LastModified", Type = QueryFieldType.Date, Label = "Last Modified" },

            new() { Path = "Summary", Type = QueryFieldType.Text, Label = "Summary" },
            new() { Path = "Description", Type = QueryFieldType.Text, Label = "Description" },

            new() { Path = "DavConfig.Name", Type = QueryFieldType.Text, Label = "Dav Config" },

            new() { Path = "Categories.Value", Type = QueryFieldType.MultiSelect, Label = "Categories" },
            new() { Path = "Attachments.Title", Type = QueryFieldType.Text, Label = "Attachments" },
            new() { Path = "Comments.Value", Type = QueryFieldType.Text, Label = "Comments" },
            new() { Path = "Attendees.Name", Type = QueryFieldType.Text, Label = "Attendee Name" },
            new() { Path = "Attendees.Email", Type = QueryFieldType.Text, Label = "Attendee Email" },
            new() { Path = "Attendees.Role", Type = QueryFieldType.Text, Label = "Attendee Role"},

            new() { Path = "Alarms.SelectedDate", Type = QueryFieldType.Date, Label = "Alarms" },
            new() { Path = "Alarms.Action", Type = QueryFieldType.Text, Label = "Alarms Action"},
        };
    }
    
    public static class NoteSchema
    {
        public static List<QueryFieldDescriptor> Fields => new()
        {
            new() { Path = "Status", Type = QueryFieldType.Enum, Label = "Status", EnumType = typeof(JournalStatus) },
            new() { Path = "Classification", Type = QueryFieldType.Enum, Label = "Classification" },
            new() { Path = "Url", Type = QueryFieldType.Text, Label = "Url" },
            new() { Path = "Contact", Type = QueryFieldType.Text, Label = "Contact" },
            
            new() { Path = "LastModified", Type = QueryFieldType.Date, Label = "Last Modified" },

            new() { Path = "Summary", Type = QueryFieldType.Text, Label = "Summary" },
            new() { Path = "Description", Type = QueryFieldType.Text, Label = "Description" },

            new() { Path = "DavConfig.Name", Type = QueryFieldType.Text, Label = "Dav Config" },

            new() { Path = "Categories.Value", Type = QueryFieldType.MultiSelect, Label = "Categories" },
            new() { Path = "Attachments.Title", Type = QueryFieldType.Text, Label = "Attachments" },
            new() { Path = "Comments.Value", Type = QueryFieldType.Text, Label = "Comments" },
            new() { Path = "Attendees.Name", Type = QueryFieldType.Text, Label = "Attendee Name" },
            new() { Path = "Attendees.Email", Type = QueryFieldType.Text, Label = "Attendee Email" },
            new() { Path = "Attendees.Role", Type = QueryFieldType.Text, Label = "Attendee Role"},

            new() { Path = "Alarms.SelectedDate", Type = QueryFieldType.Date, Label = "Alarms" },
            new() { Path = "Alarms.Action", Type = QueryFieldType.Text, Label = "Alarms Action"},
        };
    }
    
    public static class  TaskSchema
    {
        public static List<QueryFieldDescriptor> Fields => new()
        {
            new() { Path = "Status", Type = QueryFieldType.Enum, Label = "Status", EnumType = typeof(TodoStatus) },
            new() { Path = "Classification", Type = QueryFieldType.Enum, Label = "Classification" },
            new() { Path = "Priority", Type = QueryFieldType.Enum, Label = "Priority" },
            new() { Path = "Location", Type = QueryFieldType.Date, Label = "Location" },
            new() { Path = "Url", Type = QueryFieldType.Text, Label = "Url" },
            new() { Path = "Contact", Type = QueryFieldType.Text, Label = "Contact" },

            new () { Path = "StartDate", Type = QueryFieldType.Date, Label = "Start Date" },
            new () { Path = "DueDate", Type = QueryFieldType.Date, Label = "Due Date" },
            new() { Path = "LastModified", Type = QueryFieldType.Date, Label = "Last Modified" },

            new() { Path = "Summary", Type = QueryFieldType.Text, Label = "Summary" },
            new() { Path = "Description", Type = QueryFieldType.Text, Label = "Description" },

            new() { Path = "DavConfig.Name", Type = QueryFieldType.Text, Label = "Dav Config" },

            new() { Path = "Categories.Value", Type = QueryFieldType.MultiSelect, Label = "Categories" },
            new() { Path = "Attachments.Title", Type = QueryFieldType.Text, Label = "Attachments" },
            new() { Path = "Comments.Value", Type = QueryFieldType.Text, Label = "Comments" },
            new() { Path = "Attendees.Name", Type = QueryFieldType.Text, Label = "Attendee Name" },
            new() { Path = "Attendees.Email", Type = QueryFieldType.Text, Label = "Attendee Email" },
            new() { Path = "Attendees.Role", Type = QueryFieldType.Text, Label = "Attendee Role"},

            new() { Path = "Alarms.SelectedDate", Type = QueryFieldType.Date, Label = "Alarms" },
            new() { Path = "Alarms.Action", Type = QueryFieldType.Text, Label = "Alarms Action"},
        };
    }
}