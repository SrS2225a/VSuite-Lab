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

namespace VSuiteLab.ViewModels;

public partial class JournalsViewModel : ViewModelBase
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

    private ObservableCollection<CalDavJournal> _journals = new();

    public ObservableCollection<CalDavJournal> Journals
    {
        get => _journals;
        set
        {
            _journals = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<DavConfig> DavInstances { get; } = new();

    [ObservableProperty] private DavConfig? selectedDavInstance;

    [ObservableProperty] private CalDavJournal? selectedJournal;

    [ObservableProperty] private bool _debugEnabled;

    [ObservableProperty] private ObservableCollection<GroupItemsCalDavJournal> groupedJournals = new();

    [ObservableProperty] private bool isPreviewMode;
    
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
            .ApplyQuery(Journals.Where(j => !j.IsDeleted),
                query.Filters,
                query.Sorts)
            .ToList();

        var grouped = _queryService
            .ApplyGrouping(filtered, query.Groups)
            .ToList();

        GroupedJournals = new ObservableCollection<GroupItemsCalDavJournal>(
            grouped.Select(g => new GroupItemsCalDavJournal
            {
                Key = g.Key,
                Items = new ObservableCollection<CalDavJournal>(g.Items.ToList())
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

    partial void OnSelectedJournalChanged(CalDavJournal? value)
    {
        if (value == null)
        {
            SelectedDavInstance = null;
            return;
        }

        SelectedDavInstance = DavInstances.FirstOrDefault(d => d.Id == value.DavConfigId);
        
        OnPropertyChanged(nameof(PublishedDateOnly));
        OnPropertyChanged(nameof(PublishedTimeOnly));
    }

    public JournalsViewModel(SearchQueryBuilder queryBuilder)
    {
        _db = new DatabaseContext();
        QueryBuilder = queryBuilder;

        WeakReferenceMessenger.Default.Register<SyncCompletedMessage>(this,
            (_, m) => { _ = Dispatcher.UIThread.InvokeAsync(() => RefreshForInstance(m.Value)); });

        WeakReferenceMessenger.Default.Register<DavConfigChangedMessage>(this,
            async (_, m) => { await Dispatcher.UIThread.InvokeAsync(async () => { await ReloadDavInstances(); }); });

        // Initialize notes
        HookBuilderChanges();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadJournals();
    }

    private async Task LoadJournals()
    {
        await ReloadDavInstances();

        var appSettings = await _db.Settings.ToListAsync();
        DebugEnabled = appSettings?.FirstOrDefault()?.DebugEnabled ?? false;

        var notes = await _db.Journals
                    .Include(n => n.DavConfig)
                    .Include(n => n.Categories)
                    .Include(n => n.Attendees)
                    .Include(n => n.Attachments)
                    .Include(n => n.Comments)
                    .Include(n => n.Alarms)
                    .ToListAsync();
        

        Journals = new ObservableCollection<CalDavJournal>(notes.OrderByDescending(n => n.PublishedDate));
            
        var uniqueCategoires = notes
            .SelectMany(n => n.Categories)
            .GroupBy(c => c.Value)
            .Select(g => g.First())
            .ToList();
            
        AllCategories.Clear();
        foreach (var category in uniqueCategoires)
            AllCategories.Add(category);

        SelectedJournal = new();
        Refresh();
    }

    private async Task RefreshForInstance(DavConfig config)
    {
        GroupedJournals.Clear();

        var toRemove = Journals.Where(n => n.DavConfigId == config.Id).ToList();
        foreach (var note in toRemove)
            Journals.Remove(note);

        // Reload only this instance's notes
        var journals = await _db.Journals
                .Where(n => n.DavConfigId == config.Id)
                .Include(n => n.DavConfig)
                .Include(n => n.Categories)
                .Include(n => n.Attendees)
                .Include(n => n.Attachments)
                .Include(n => n.Comments)
                .Include(n => n.Alarms)
                .ToListAsync();
        
        foreach (var journal in journals)
            Journals.Add(journal);

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
        
        // keep selection valid
        if (SelectedDavInstance != null)
        {
            SelectedDavInstance = DavInstances
                .FirstOrDefault(d => d.Id == SelectedDavInstance.Id);
        }
    }

    [RelayCommand]
    public async Task DownloadIcsCommand(CalDavJournal? task)
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
        if (SelectedJournal != null && SelectedDavInstance != null)
        {
            var fullUri = new Uri(
                new Uri(SelectedDavInstance?.httpUrl!),
                $"{SelectedJournal.Id}.ics");
            SelectedJournal.Uri = fullUri.ToString();
            if (SelectedDavInstance != null) SelectedJournal.DavConfigId = SelectedDavInstance.Id;
            SelectedJournal.PublishedDate ??= DateTimeOffset.Now;//
            SelectedJournal.Uid = Guid.NewGuid().ToString();
            SelectedJournal.IsDirty = true;

            _db.Journals.Add(SelectedJournal);
            await _db.SaveChangesAsync();

            Journals.Add(SelectedJournal);

            SelectedJournal = null;
            Dispatcher.UIThread.Post(() => SelectedJournal = new CalDavJournal());

            ApplyGrouping();
        }
    }

    [RelayCommand]
    public async Task SaveJournalCommand()
    {
        if (SelectedJournal != null)
        {
            SelectedJournal.IsDirty = true;
            SelectedJournal.LastModified = DateTime.UtcNow;
            
            _db.Journals.Update(SelectedJournal);
            await _db.SaveChangesAsync();
            
            SelectedJournal = null;
            Dispatcher.UIThread.Post(() => SelectedJournal = new CalDavJournal());
        }
    }

    [RelayCommand]
    public async Task DeleteJournalCommand()
    {
        if (SelectedJournal != null)
        {
            SelectedJournal.IsDirty = true;
            SelectedJournal.IsDeleted = true;

            _db.Journals.Update(SelectedJournal);
            await _db.SaveChangesAsync();

            Journals.Remove(SelectedJournal);
            SelectedJournal = null;
            Dispatcher.UIThread.Post(() => SelectedJournal = new CalDavJournal());

            ApplyGrouping();
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
                if (ics is CalDavJournal calDavJournal)
                {
                    SelectedJournal = calDavJournal;
                    SelectedDavInstance = config;

                    await SaveNewJournal();
                }
                else
                {
                    faultyFile.Add(new Tuple<string, string>(file.Name, "Invalid Journal ICS file"));
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
            var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Journal Import",
                $"Could not import {faultyFile.Count} out of {files.Count} journals:\n" +
                $"{faltyFilesString}", ButtonEnum.Ok, Icon.Error);
            await invalidJournal.ShowAsync();
        }
        else
        {
            var invalidJournal = MessageBoxManager.GetMessageBoxStandard("Journal Import",
                "All journals have been imported successfully", ButtonEnum.Ok, Icon.Info);
            await invalidJournal.ShowAsync();
        }
    }

    [RelayCommand]
    public Task CancelNoteSelection()
    {
        SelectedJournal = null;
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void AddCategoryCommand()
    {
        if (SelectedJournal == null || string.IsNullOrWhiteSpace(CategoryInput))
            return;

        SelectedJournal.Categories.Add(new CalDavCategory { Value = CategoryInput });
        
        // if (AllCategories.All(c => c.Value.Trim().ToLower() != category.Value.ToLower().Trim()))
        //     AllCategories.Add(category);
        //
        // if (SelectedJournal.Categories.All(c => c.Value.ToLower().Trim() != category.Value.ToLower().Trim()))
        //     SelectedJournal.Categories.Add(category);

        // clear input
        CategoryInput = string.Empty;
    }

    [RelayCommand]
    public void RemoveCategoryCommand(CalDavCategory category)
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Categories.Remove(category);

        // optional: remove from global list ONLY if truly unused
        if (!Journals.Any(j => j.Categories.Any(c => c.Id == category.Id)))
        {
            AllCategories.Remove(category);
        }
    }

    [RelayCommand]
    public void AddCommentCommand()
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Comments.Add(new CalDavComment());
    }

    [RelayCommand]
    public void RemoveCommentCommand(CalDavComment comment)
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Comments.Remove(comment);
    }

    [RelayCommand]
    public void AddAttendeeCommand()
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Attendees.Add(new CalDavAttendee());
    }

    [RelayCommand]
    public void RemoveAttendeeCommand(CalDavAttendee attendee)
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Attendees.Remove(attendee);
    }

    [RelayCommand]
    public async Task AddAttachmentCommand()
    {
        if (SelectedJournal == null)
            return;

        var files = await FilePickerUtils.OpenFileDialog();

        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            SelectedJournal.Attachments.Add(new CalDavAttachment
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
        if (SelectedJournal == null)
            return;

        SelectedJournal.Attachments.Remove(attachment);
    }

    [RelayCommand]
    public void AddAlarmCommand()
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Alarms.Add(new CalDavAlarm());
    }

    [RelayCommand]
    public void RemoveAlarmCommand(CalDavAlarm alarm)
    {
        if (SelectedJournal == null)
            return;

        SelectedJournal.Alarms.Remove(alarm);
    }

    [RelayCommand]
    public async Task DownloadAttachmentCommand(CalDavAttachment alarm)
    {
        var fileExtension = alarm.Title.Split('.').LastOrDefault();
        await FilePickerUtils.SaveFileDialog(alarm.Title, alarm.Uri, fileExtension);
    }

    public DateTimeOffset? PublishedDateOnly
    {
        get => TimeConverter.GetDateOnly(SelectedJournal?.PublishedDate);

        set
        {
            if (SelectedJournal == null)
                return;

            SelectedJournal.PublishedDate = TimeConverter.SetDateOnly(SelectedJournal.PublishedDate, value);

            OnPropertyChanged();
        }
    }
    
    [RelayCommand]
    public void ClearPublishedDate()
    {
        if (SelectedJournal?.PublishedDate == null)
            return;

        SelectedJournal.PublishedDate = null;
        PublishedDateOnly = null;
        PublishedTimeOnly = null;
    }


    public TimeSpan? PublishedTimeOnly
    {
        get => TimeConverter.GetTimeOnly(SelectedJournal?.PublishedDate);

        set
        {
            if (SelectedJournal == null)
                return;

            SelectedJournal.PublishedDate = TimeConverter.SetTimeOnly(SelectedJournal.PublishedDate, value);

            OnPropertyChanged();
        }
    }
}