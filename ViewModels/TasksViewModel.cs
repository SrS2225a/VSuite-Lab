using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using HeyRed.Mime;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Converters;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Utils;
using VSuiteLab.Utils.Query;
using VSuiteLab.Views;
using TodoStatus = VSuiteLab.Models.TodoStatus;

namespace VSuiteLab.ViewModels
{
    public partial class TasksViewModel : ViewModelBase
    {
        private readonly DatabaseContext _db;
        private readonly QueryService _queryService = new();

        private SearchQueryBuilder QueryBuilder { get; }

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
            new EnumOption<string>(string.Empty, "No Class"),
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
            if (SelectedNote?.StartDate == null)
                return;

            SelectedNote.StartDate = null;
            StartTimeOnly = null;
            StartDateOnly = null;
            OnPropertyChanged(nameof(DueDateOnly));
            OnPropertyChanged(nameof(DueTimeOnly));
        }

        public void ClearDueDate()
        {
            if (SelectedNote?.DueDate == null)
                return;

            SelectedNote.DueDate = null;
            DueTimeOnly = null;
            DueDateOnly = null;
            OnPropertyChanged(nameof(StartDateOnly));
            OnPropertyChanged(nameof(StartTimeOnly));
        }

        private ObservableCollection<CalDavTask> _tasks = new();

        public ObservableCollection<CalDavTask> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DavConfig> DavInstances { get; } = new();

        [ObservableProperty] private DavConfig? selectedDavInstance;

        [ObservableProperty] private CalDavTask? selectedNote;

        [ObservableProperty] private string _searchText = string.Empty;

        [ObservableProperty] private bool _debugEnabled;

        [ObservableProperty] private ObservableCollection<GroupItemsCalDavTask> groupedNotes = new();

        [ObservableProperty] private bool isPreviewMode = false;
        
        [ObservableProperty]
        private string? categoryInput;
    
        public ObservableCollection<CalDavCategory> AllCategories { get; } = new();

        private void Refresh()
        {
            ApplyGrouping();
        }

        private void ApplyGrouping()
        {
            var query = QueryMapper.ToQueryModel(
                QueryBuilder.Filters,
                QueryBuilder.Sorts,
                QueryBuilder.Groups);

            var filtered = _queryService
                .ApplyQuery(Tasks.Where(j => !j.IsDeleted),
                    query.Filters,
                    query.Sorts)
                .ToList();

            var grouped = _queryService
                .ApplyGrouping(filtered, query.Groups)
                .ToList();

            GroupedNotes = new ObservableCollection<GroupItemsCalDavTask>(
                grouped.Select(g => new GroupItemsCalDavTask
                {
                    Key = g.Key,
                    Items = new ObservableCollection<CalDavTask>(g.Items.ToList())
                })
            );
        }

        private void HookBuilderChanges()
        {
            HookCollection(QueryBuilder.Filters);
            HookCollection(QueryBuilder.Sorts);
            HookCollection(QueryBuilder.Groups);
        }

        private void HookCollection<T>(ObservableCollection<T> collection)
            where T : INotifyPropertyChanged
        {
            // Hook existing items
            foreach (var item in collection)
                item.PropertyChanged += OnBuilderItemChanged;

            collection.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (T item in e.NewItems)
                        item.PropertyChanged += OnBuilderItemChanged;
                }

                if (e.OldItems != null)
                {
                    foreach (T item in e.OldItems)
                        item.PropertyChanged -= OnBuilderItemChanged;
                }

                Refresh(); // collection itself changed
            };
        }

        private void OnBuilderItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            Refresh();
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

        public TasksViewModel(SearchQueryBuilder queryBuilder)
        {
            _db = new DatabaseContext();
            QueryBuilder = queryBuilder;

            WeakReferenceMessenger.Default.Register<SyncCompletedMessage>(this,
                (_, m) => { _ = Dispatcher.UIThread.InvokeAsync(() => RefreshForInstance(m.Value)); });

            WeakReferenceMessenger.Default.Register<DavConfigChangedMessage>(this,
                (_, m) =>
                {
                    Dispatcher.UIThread.Post(async void () =>
                    {
                        switch (m.ChangeType)
                        {
                            case DavConfigChangeType.Added:
                                if (!DavInstances.Any(x => x.Id == m.Value.Id))
                                    DavInstances.Add(m.Value);
                                break;

                            case DavConfigChangeType.Updated:
                                var existing = DavInstances.FirstOrDefault(x => x.Id == m.Value.Id);
                                if (existing != null)
                                {
                                    var index = DavInstances.IndexOf(existing);
                                    DavInstances[index] = m.Value;
                                }
                                break;

                            case DavConfigChangeType.Deleted:
                                var toRemove = DavInstances.FirstOrDefault(x => x.Id == m.Value.Id);
                                if (toRemove != null)
                                {
                                    DavInstances.Remove(toRemove);
                                    await RefreshForInstance(toRemove);
                                }
                                break;
                        }
                    });
                });

            // Initialize notes
            HookBuilderChanges();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadNotes();
        }

        private async Task LoadNotes()
        {
            await ReloadDavInstances();

            var appSettings = await _db.Settings.ToListAsync();
            DebugEnabled = appSettings.FirstOrDefault()?.DebugEnabled ?? false;

            var notes = await _db.Tasks
                .Include(n => n.DavConfig)
                .Include(n => n.Alarms)
                .Include(n => n.Categories)
                .Include(n => n.Attendees)
                .Include(n => n.Attachments)
                .Include(n => n.Comments)
                .ToListAsync();

            Tasks = new ObservableCollection<CalDavTask>(notes);
            
            var uniqueCategories = notes
                .SelectMany(n => n.Categories)
                .GroupBy(c => c.Value)
                .Select(g => g.First())
                .ToList();
            
            AllCategories.Clear();
            foreach (var category in uniqueCategories)
                AllCategories.Add(category);

            SelectedNote = new CalDavTask();

            ApplyGrouping();
        }

        private async Task RefreshForInstance(DavConfig config)
        {
            GroupedNotes.Clear();

            var toRemove = Tasks.Where(n => n.DavConfigId == config.Id).ToList();
            foreach (var note in toRemove)
                Tasks.Remove(note);

            var notes = await _db.Tasks
                .Where(n => n.DavConfigId == config.Id)
                .Include(n => n.DavConfig)
                .Include(n => n.Alarms)
                .Include(n => n.Categories)
                .Include(n => n.Attendees)
                .Include(n => n.Attachments)
                .Include(n => n.Comments)
                .AsSplitQuery()
                .ToListAsync();

            foreach (var note in notes)
                Tasks.Add(note);

            Refresh();
        }

        [RelayCommand]
        public async Task ImportIcsNote(DavConfig config)
        {
            var files = await FilePickerUtils.OpenFileDialog();
            if (files.Count == 0)
                return;

            List<Tuple<string, string>> faultyFile = new List<Tuple<string, string>>();

            var utils = new IcsUtils();

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                try
                {
                    var ics = utils.ParseIcs(Encoding.UTF8.GetString(ms.ToArray()));
                    if (ics is CalDavTask calDavTask)
                    {
                        SelectedNote = calDavTask;
                        SelectedDavInstance = config;

                        await SaveNewNote();
                    }
                    else
                    {
                        faultyFile.Add(new Tuple<string, string>(file.Name, "Invalid Task ICS file"));
                    }
                }
                catch (Exception e)
                {
                    faultyFile.Add(new Tuple<string, string>(file.Name, e.Message));
                }
            }

            if (faultyFile.Count > 0)
            {
                var faltyFilesString = string.Join(",",
                    faultyFile.Select(t => string.Format("-{0}, {1}\n", t.Item1, t.Item2)));
                var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Task Import",
                    $"Could not import {faultyFile.Count} out of {files.Count} tasks:\n" +
                    $"{faltyFilesString}", ButtonEnum.Ok, Icon.Error);
                await invalidJournal.ShowAsync();
            }
            else
            {
                var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Task Import",
                    "All tasks have been imported successfully", ButtonEnum.Ok, Icon.Info);
                await invalidJournal.ShowAsync();
            }
        }

        private async Task ReloadDavInstances()
        {
            DavInstances.Clear();

            var instances = await _db.DavConfigs
                .OrderBy(i => i.Name)
                .ToListAsync();

            foreach (var instance in instances)
                DavInstances.Add(instance);

            if (SelectedDavInstance != null)
            {
                SelectedDavInstance = DavInstances
                    .FirstOrDefault(d => d.Id == SelectedDavInstance.Id);
            }
        }


        [RelayCommand]
        public async Task DownloadIcsCommand(CalDavTask? task)
        {
            if (task == null)
                return;

            var icsUtils = new IcsUtils();
            var icsContent = icsUtils.BuildIcs(task);

            await FilePickerUtils.SaveFileDialog(task.Uid + ".ics", Encoding.UTF8.GetBytes(icsContent), "ics");
        }

        [RelayCommand]
        public async Task DownloadAttachmentCommand(CalDavAttachment alarm)
        {
            var fileExtension = alarm.Title.Split('.').LastOrDefault();
            await FilePickerUtils.SaveFileDialog(alarm.Title, alarm.Uri, fileExtension);
        }

        [RelayCommand]
        private async Task SaveNewNote()
        {
            if (SelectedNote != null && SelectedDavInstance != null)
            {
                var fullUri = new Uri(
                    new Uri(SelectedDavInstance?.httpUrl!),
                    $"{SelectedNote.Id}.ics");
                SelectedNote.Uri = fullUri.ToString();
                if (SelectedDavInstance != null) SelectedNote.DavConfigId = SelectedDavInstance.Id;
                SelectedNote.Uid = Guid.NewGuid().ToString();
                SelectedNote.IsDirty = true;
                
                _db.Tasks.Add(SelectedNote);
                await _db.SaveChangesAsync();

                Tasks.Add(SelectedNote);

                SelectedNote = new CalDavTask();
                Dispatcher.UIThread.Post(() => SelectedNote = new CalDavTask());

                ApplyGrouping();
            }
        }


        [RelayCommand]
        private async Task SaveNote()
        {
            if (SelectedNote != null && SelectedDavInstance != null)
            {
                SelectedNote.IsDirty = true;
                SelectedNote.LastModified = DateTime.UtcNow;

                _db.Tasks.Update(SelectedNote);
                await _db.SaveChangesAsync();
                SelectedNote = null;
                // Dispatcher.UIThread.Post(() => SelectedNote = new CalDavTask());
            }
        }


        [RelayCommand]
        private async Task DeleteNote()
        {
            if (SelectedNote != null)
            {
                SelectedNote.IsDeleted = true;
                SelectedNote.IsDirty = true;
                await _db.SaveChangesAsync();

                Tasks.Remove(SelectedNote);
                SelectedNote = null;
                //Dispatcher.UIThread.Post(() => SelectedNote = new CalDavTask());

                ApplyGrouping();
            }
        }

        [RelayCommand]
        public Task CancelNoteSelection()
        {
            SelectedNote = null;
            return Task.CompletedTask;
        }

        [RelayCommand]
        public void AddCategoryCommand()
        {
            if (SelectedNote == null || string.IsNullOrWhiteSpace(CategoryInput))
                return;

            var category = new CalDavCategory { Value = CategoryInput };
        
            if (AllCategories.All(c => c.Value.Trim().ToLower() != category.Value.ToLower().Trim()))
                AllCategories.Add(category);
        
            if (SelectedNote.Categories.All(c => c.Value.ToLower().Trim() != category.Value.ToLower().Trim()))
                SelectedNote.Categories.Add(category);

            // clear input
            CategoryInput = string.Empty;
        }

        [RelayCommand]
        public void RemoveCategoryCommand(CalDavCategory category)
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Categories.Remove(category);
        
            if (Tasks.Any(j => j.Categories.Any(c => c.Value.ToLower().Trim() != category.Value.ToLower().Trim())))
                AllCategories.Remove(category);
        }

        [RelayCommand]
        public void AddAttendeeCommand()
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Attendees.Add(new CalDavAttendee());
        }

        [RelayCommand]
        public void RemoveAttendeeCommand(CalDavAttendee attendee)
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Attendees.Remove(attendee);
        }

        [RelayCommand]
        public void AddAlarmCommand()
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Alarms.Add(new CalDavAlarm());
        }

        [RelayCommand]
        public void RemoveAlarmCommand(CalDavAlarm alarm)
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Alarms.Remove(alarm);
        }

        [RelayCommand]
        public void AddCommentCommand()
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Comments.Add(new CalDavComment());
        }

        [RelayCommand]
        public void RemoveCommentCommand(CalDavComment comment)
        {
            if (SelectedNote == null)
                return;

            SelectedNote.Comments.Remove(comment);
        }

        [RelayCommand]
        public async Task AddAttachmentCommand()
        {
            if (SelectedNote == null)
                return;

            var files = await FilePickerUtils.OpenFileDialog();

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
            if (SelectedNote == null)
                return;

            SelectedNote.Attachments.Remove(attachment);
        }
    }
}