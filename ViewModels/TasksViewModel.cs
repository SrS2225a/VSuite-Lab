using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HeyRed.Mime;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Converters;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Utils;
using TodoStatus = VSuiteLab.Models.TodoStatus;

namespace VSuiteLab.ViewModels
{
    public partial class TasksViewModel : ViewModelBase
    {
        private readonly SyncService _syncService;
        private readonly DatabaseService _databaseService;
        private readonly QueryService _queryService = new();
        
        public IEnumerable<EnumOption<TodoStatus>> TodoStatuses =>
            new[]
            {
                new EnumOption<TodoStatus>(TodoStatus.NeedsAction, "Needs Action"),
                new EnumOption<TodoStatus>(TodoStatus.InProgress, "In Progress"),
                new EnumOption<TodoStatus>(TodoStatus.Completed, "Completed"),
                new EnumOption<TodoStatus>(TodoStatus.Cancelled, "Canceled")
            };

        public IEnumerable<EnumOption<int>> Priorities =>
            new[]
            {
                new EnumOption<int>(0, "No Priority"),
                new EnumOption<int>(1, "1 - Highest"),
                new EnumOption<int>(2, "2 - Higher"),
                new EnumOption<int>(3, "3 - High"),
                new EnumOption<int>(4, "4 - Medium High"),
                new EnumOption<int>(5, "5 - Medium"),
                new EnumOption<int>(6, "6 - Medium Low"),
                new EnumOption<int>(7, "7 - Low"),
                new EnumOption<int>(8, "8 - Lower"),
                new EnumOption<int>(9, "9 - Lowest")
            };

        public IEnumerable<EnumOption<string>> Classifications => new[]
        {
            new EnumOption<string>("PUBLIC", "Public"),
            new EnumOption<string>("PRIVATE", "Private"),
            new EnumOption<string>("CONFIDENTIAL", "Confidential"),
        };
        
        public DateTimeOffset? StartDateOnly
        {
            get => TimeConverter.GetDateOnly(SelectedNote?.StartDate);

            set
            {
                if (SelectedNote == null)
                    return;

                SelectedNote.StartDate = TimeConverter.SetDateOnly(SelectedNote.StartDate, value);
                
                OnPropertyChanged(nameof(StartTimeOnly));
            }
        }

        public TimeSpan? StartTimeOnly
        {
            get => TimeConverter.GetTimeOnly(SelectedNote?.StartDate);

            set
            {
                if (SelectedNote == null)
                    return;

                SelectedNote.StartDate = TimeConverter.SetTimeOnly(SelectedNote.StartDate, value);
                
                OnPropertyChanged(nameof(StartDateOnly));
            }
        }
        
        public DateTimeOffset? DueDateOnly
        {
            get => TimeConverter.GetDateOnly(SelectedNote?.DueDate);

            set
            {
                if (SelectedNote == null)
                    return;

                SelectedNote.DueDate = TimeConverter.SetDateOnly(SelectedNote.DueDate, value);
                
                OnPropertyChanged(nameof(DueTimeOnly));
            }
        }

        public TimeSpan? DueTimeOnly
        {
            get => TimeConverter.GetTimeOnly(SelectedNote?.DueDate);

            set
            {
                if (SelectedNote == null)
                    return;
                
                SelectedNote.DueDate = TimeConverter.SetTimeOnly(SelectedNote.DueDate, value);
                
                OnPropertyChanged(nameof(DueDateOnly));
            }
        }

        public void ClearStartDate()
        {
            if(SelectedNote.StartDate == null)
                return;

            SelectedNote.StartDate = null;
            StartTimeOnly = null;
            StartDateOnly = null;
            OnPropertyChanged();
        }
        
        public void ClearDueDate()
        {
            if(SelectedNote.DueDate == null)
                return;

            SelectedNote.DueDate = null;
            DueTimeOnly = null;
            DueDateOnly = null;
            OnPropertyChanged();
        }
        
        public IRelayCommand<CalDavTask> ShowDebugCommand => new RelayCommand<CalDavTask>(async task =>
        {
            if (task == null) return;

            string debugInfo = $"Id: {task.Id}\n" +
                               $"UriUrl: {task.uriUrl}\n" +
                               $"Etag: {task.Etag}\n" +
                               $"Uid: {task.Uid}\n" +
                               $"Sequence: {task.Sequence}";

            // Get the current main window
            var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow != null)
            {
                var dialog = new Window
                {
                    Title = "Debug Info",
                    Width = 300,
                    Height = 200,
                    Content = new TextBlock
                    {
                        Text = debugInfo,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(10)
                    }
                };

                await dialog.ShowDialog(mainWindow);
            }
        });
        
        public ObservableCollection<CalDavTask> Notes { get; } = new();
        public ObservableCollection<DavConfig> DavInstances { get; } = new();
        
        [ObservableProperty] private DavConfig? selectedDavInstance;
        
        [ObservableProperty] 
        private CalDavTask? selectedNote;
        
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty] private bool _debugEnabled = false; 
        
        [ObservableProperty]
        private ObservableCollection<GroupItemsCalDavTask> groupedNotes = new();
        
        private void ApplyGrouping()
        {
            var parsed = QueryUtils.ParseQuery(SearchText);
            var filtered = _queryService.ApplyQuery(Notes, parsed.Filters, parsed.Sorts);
            var grouped = _queryService.ApplyGrouping(filtered, parsed.Groups);
                
            var newGroups = new ObservableCollection<GroupItemsCalDavTask>(
                grouped.Select(g => new GroupItemsCalDavTask
                {
                    Key = g.Key,
                    Items = new ObservableCollection<CalDavTask>(g.Items)
                })
            );

            GroupedNotes = newGroups;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyGrouping();
        }

        partial void OnSelectedNoteChanged(CalDavTask? value)
        {
            if (value == null)
            {
                SelectedDavInstance = null;
                return;
            }
            
            SelectedDavInstance = DavInstances.FirstOrDefault(d => d.Id == value.DavConfigId);
            
            OnPropertyChanged(nameof(StartDateOnly));
            OnPropertyChanged(nameof(StartTimeOnly));
            OnPropertyChanged(nameof(DueDateOnly));
            OnPropertyChanged(nameof(DueTimeOnly));
        }

        public TasksViewModel()
        {
            _syncService = new SyncService();
            _databaseService = new DatabaseService();

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadNotes();
        }

        private async Task LoadNotes()
        {
            var instances = await _databaseService.ReadAllAsync<DavConfig>();
            foreach(var instance in instances.Value.OrderBy(i => i.Name))
            {
                DavInstances.Add(instance);
            }

            var appSettings = await _databaseService.ReadAllAsync<Settings>();
            DebugEnabled = appSettings.Value?.FirstOrDefault().DebugEnabled ?? false;
            Console.WriteLine("debug enabled:");
            Console.WriteLine(DebugEnabled);
            
            var notes = await _databaseService.ReadAllAsync<CalDavTask>(query =>
                query
                    .Include(n => n.Alarms)
                    .Include(n => n.Categories)
                    .Include(n => n.Attendees)
                    .Include(n => n.Attachments)
                    .Include(n => n.Comments)
                    .AsSplitQuery()
            );
            foreach (var note in notes.Value)
            {
                Notes.Add(note);
            }
            
            SelectedNote = new();
            // await _syncService.SyncAllAsync();
            ApplyGrouping();
        }

        [RelayCommand]
        public async Task DownloadICSCommand(CalDavTask task)
        {
            if(task == null)
                return;
            
            var sfd = new SaveFileDialog
            {
                DefaultExtension = "ics",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "iCalendar", Extensions = { "ics" } }
                },
                InitialFileName = task.Uid + ".ics"
            };
            
            var ICSUtils = new ICSUtils();
            var IcsContent = ICSUtils.BuildVTodoICS(task);
            
            var lifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var path = await sfd.ShowAsync(lifetime?.MainWindow);

            if (!string.IsNullOrEmpty(path))
            {
                await File.WriteAllTextAsync(path, IcsContent);
            }
        }

        [RelayCommand]
        private async Task SaveNewNote()
        {
            if (SelectedNote != null && selectedDavInstance != null)
            {
                var fullUri = new Uri(
                    new Uri(selectedDavInstance.httpUrl),
                    $"{SelectedNote.Id}.ics");
                SelectedNote.uriUrl = fullUri.ToString();
                SelectedNote.DavConfigId = selectedDavInstance.Id;
                SelectedNote.Uid = Guid.NewGuid().ToString();
                SelectedNote.IsDirty = true;
                await _databaseService.CreateAsync(SelectedNote);
                
                Notes.Add(SelectedNote);
                SelectedNote = new();
            }
        }


        [RelayCommand]
        private async Task SaveNote()
        {
            if (SelectedNote != null && selectedDavInstance != null)
            {
                SelectedNote.IsDirty = true;
                SelectedNote.LastModified = DateTime.Now.ToUniversalTime();

                await _databaseService.SaveChangesAsync();
                
                // Reset selected note
                SelectedNote = new();
            }
        }

        [RelayCommand]
        private async Task DeleteNote()
        {
            if (SelectedNote != null)
            {
                SelectedNote.IsDeleted = true;
                SelectedNote.IsDirty = true;
                await _databaseService.UpdateAsync(SelectedNote);

                Notes.Remove(SelectedNote);
                SelectedNote = new();
            }
        }
        
        [RelayCommand]
        public void AddCategoryCommand()
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Categories.Add(new CalDavCategory { Value = string.Empty });
        }

        [RelayCommand]
        public void RemoveCategoryCommand(CalDavCategory category)
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Categories.Remove(category);
        }

        [RelayCommand]
        public void AddAttendeeCommand()
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Attendees.Add(new CalDavAttendee());
        }

        [RelayCommand]
        public void RemoveAttendeeCommand(CalDavAttendee attendee)
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Attendees.Remove(attendee);
        }
        
        [RelayCommand]
        public void AddAlarmCommand()
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Alarms.Add(new CalDavAlarm());
        }

        [RelayCommand]
        public void RemoveAlarmCommand(CalDavAlarm alarm)
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Alarms.Remove(alarm);
        }

        [RelayCommand]
        public void AddCommentCommand()
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Comments.Add(new CalDavComment());
        }

        [RelayCommand]
        public void RemoveCommentCommand(CalDavComment comment)
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Comments.Remove(comment);
        }

        [RelayCommand]
        public async Task AddAttachmentCommand()
        {
            if(SelectedNote == null)
                return;
            
            var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select file to attach",
                AllowMultiple = true
            });

            foreach (var file in files)
            { 
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                
                SelectedNote.Attachments.Add(new CalDavAttachment
                {
                    Title = file.Name,
                    ContentType = MimeTypesMap.GetMimeType(file.Name),
                    Uri = ms.ToArray()
                });
            }
        }

        [RelayCommand]
        public void RemoveAttachmentCommand(CalDavAttachment attachment)
        {
            if(SelectedNote == null)
                return;
            
            SelectedNote.Attachments.Remove(attachment);
        }
    }
}