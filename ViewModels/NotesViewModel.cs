using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HeyRed.Mime;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using VSuiteLab.Converters;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Services;
using VSuiteLab.Utils;
using VSuiteLab.Utils.Query;
using VSuiteLab.Views;
using VSuiteLab.Views.Windows;

namespace VSuiteLab.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    private readonly DatabaseContext _db;
    private readonly QueryService _queryService = new();

    private SearchQueryBuilder QueryBuilder { get; }

    public IEnumerable<EnumOption<JournalStatus>> JounralStatuses =>
        new[]
        {
            new EnumOption<JournalStatus>(JournalStatus.Draft, "Draft"),
            new EnumOption<JournalStatus>(JournalStatus.Final, "Final"),
            new EnumOption<JournalStatus>(JournalStatus.Cancelled, "Cancelled")
        };

    public IEnumerable<EnumOption<string>> Classifications => new[]
    {
        new EnumOption<string>("PUBLIC", "Public"),
        new EnumOption<string>("PRIVATE", "Private"),
        new EnumOption<string>("CONFIDENTIAL", "Confidential"),
    };

    private ObservableCollection<CalDavNote> notes = new();

    public ObservableCollection<CalDavNote> Notes
    {
        get => notes;
        set
        {
            notes = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<DavConfig> DavInstances { get; } = new();

    [ObservableProperty] private DavConfig? selectedDavInstance;

    [ObservableProperty] private CalDavNote? selectedNote;

    [ObservableProperty] private bool _debugEnabled;

    [ObservableProperty] private ObservableCollection<GroupItemsCalDavNote> groupedNotes = new();

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
            .ApplyQuery(Notes.Where(j => !j.IsDeleted),
                query.Filters,
                query.Sorts)
            .ToList();

        var grouped = _queryService
            .ApplyGrouping(filtered, query.Groups)
            .ToList();

        GroupedNotes = new ObservableCollection<GroupItemsCalDavNote>(
            grouped.Select(g => new GroupItemsCalDavNote
            {
                Key = g.Key,
                Items = new ObservableCollection<CalDavNote>(g.Items.ToList())
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

    partial void OnSelectedNoteChanged(CalDavNote? value)
    {
        if (value == null)
        {
            SelectedDavInstance = null;
            return;
        }

        SelectedDavInstance = DavInstances.FirstOrDefault(d => d.Id == value.DavConfigId);
    }

    public NotesViewModel(SearchQueryBuilder queryBuilder)
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
        DebugEnabled = appSettings?.FirstOrDefault()?.DebugEnabled ?? false;

        var notes = await _db.Notes
            .Include(n => n.DavConfig)
            .Include(n => n.Alarms)
            .Include(n => n.Categories)
            .Include(n => n.Attendees)
            .Include(n => n.Attachments)
            .Include(n => n.Comments)
            .ToListAsync();

        Notes = new ObservableCollection<CalDavNote>(notes);
            
        var uniqueCategoires = notes
            .SelectMany(n => n.Categories)
            .GroupBy(c => c.Value)
            .Select(g => g.First())
            .ToList();
            
        AllCategories.Clear();
        foreach (var category in uniqueCategoires)
            AllCategories.Add(category);

        SelectedNote = new();
        Refresh();
    }

    private async Task RefreshForInstance(DavConfig config)
    {
        GroupedNotes.Clear();

        var toRemove = Notes.Where(n => n.DavConfigId == config.Id).ToList();
        foreach (var note in toRemove)
            Notes.Remove(note);

        // Reload only this instance's notes
        var notes = await _db.Notes
                .Where(n => n.DavConfigId == config.Id)
                .Include(n => n.DavConfig)
                .Include(n => n.Categories)
                .Include(n => n.Attendees)
                .Include(n => n.Attachments)
                .Include(n => n.Comments)
                .Include(n => n.Alarms)
                .AsSplitQuery()
                .ToListAsync();

        foreach (var note in notes)
            Notes.Add(note);

        Refresh();
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
                if (ics is CalDavNote calDavNote)
                {
                    SelectedNote = calDavNote;
                    SelectedDavInstance = config;

                    await SaveNewJournal();
                }
                else
                {
                    faultyFile.Add(new Tuple<string, string>(file.Name, "Invalid Note ICS file"));
                }
            }
            catch (Exception e)
            {
                faultyFile.Add(new Tuple<string, string>(file.Name, e.Message));
            }
        }

        if (faultyFile.Count > 0)
        {
            var faltyFilesString =
                string.Join(",", faultyFile.Select(t => string.Format("-{0}, {1}\n", t.Item1, t.Item2)));
            var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Note Import",
                $"Could not import {faultyFile.Count} out of {files.Count} notes:\n" +
                $"{faltyFilesString}", ButtonEnum.Ok, Icon.Error);
            await invalidJournal.ShowAsync();
        }
        else
        {
            var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Note Import",
                "All notes have been imported successfully", ButtonEnum.Ok, Icon.Info);
            await invalidJournal.ShowAsync();
        }
    }

    [RelayCommand]
    public async Task DownloadIcsCommand(CalDavNote? task)
    {
        if (task == null)
            return;

        var icsUtils = new IcsUtils();
        var icsContent = icsUtils.BuildIcs(task);

        await FilePickerUtils.SaveFileDialog(task.Uid + ".ics", Encoding.UTF8.GetBytes(icsContent), "ics");
    }

    [RelayCommand]
    public async Task SaveNewJournal()
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

            _db.Notes.Add(SelectedNote);
            await _db.SaveChangesAsync();

            Notes.Add(SelectedNote);

            SelectedNote = null;
            Dispatcher.UIThread.Post(() => SelectedNote = new CalDavNote());

            ApplyGrouping();
        }
    }

    [RelayCommand]
    public async Task SaveJournalCommand()
    {
        if (SelectedNote != null)
        {
            SelectedNote.IsDirty = true;
            SelectedNote.LastModified = DateTime.UtcNow;

            _db.Notes.Update(SelectedNote);
            await _db.SaveChangesAsync();
            
            SelectedNote = null;
            Dispatcher.UIThread.Post(() => SelectedNote = new CalDavNote());
        }
    }

    [RelayCommand]
    public async Task DeleteJournalCommand()
    {
        if (SelectedNote != null)
        {
            SelectedNote.IsDirty = true;
            SelectedNote.IsDeleted = true;
            
            await _db.SaveChangesAsync();

            Notes.Remove(SelectedNote);
            SelectedNote = null;
            Dispatcher.UIThread.Post(() => SelectedNote = new CalDavNote());

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
        
        if (Notes.Any(j => j.Categories.Any(c => c.Value.ToLower().Trim() != category.Value.ToLower().Trim())))
            AllCategories.Remove(category);
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
    public async Task DownloadAttachmentCommand(CalDavAttachment alarm)
    {
        var fileExtension = alarm.Title.Split('.').LastOrDefault();
        await FilePickerUtils.SaveFileDialog(alarm.Title, alarm.Uri, fileExtension);
    }
}