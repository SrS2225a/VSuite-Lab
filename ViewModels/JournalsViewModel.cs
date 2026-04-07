using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using VSuiteLab.Converters;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Utils;
using VSuiteLab.Views;

namespace VSuiteLab.ViewModels;

public partial class JournalsViewModel : ViewModelBase,  IViewModelSearchableContext
{
    private readonly DatabaseService _databaseService;
    private readonly QueryService _queryService = new();
    
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
        
    [ObservableProperty] 
    private CalDavJournal? selectedJournal;
        
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty] private bool _debugEnabled = false; 
        
    [ObservableProperty]
    private ObservableCollection<GroupItemsCalDavJournal> groupedJournals = new();
        
    [ObservableProperty]
    private bool isPreviewMode = true;
    
    private void ApplyGrouping()
    {
        var parsed = QueryUtils.ParseQuery(SearchText);

        var filtered = _queryService
            .ApplyQuery(Journals.Where(j => !j.IsDeleted), parsed.Filters, parsed.Sorts)
            .ToList();

        var grouped = _queryService
            .ApplyGrouping(filtered, parsed.Groups)
            .ToList();

        GroupedJournals = new ObservableCollection<GroupItemsCalDavJournal>(
            grouped.Select(g => new GroupItemsCalDavJournal
            {
                Key = g.Key,
                Items = new ObservableCollection<CalDavJournal>(g.Items.ToList())
            })
        );
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyGrouping();
    }

    partial void OnSelectedJournalChanged(CalDavJournal? value)
    {
        if (value == null)
        {
            SelectedDavInstance = null;
            return;
        }

        SelectedDavInstance = DavInstances.FirstOrDefault(d => d.Id == value.DavConfigId);
    }
    
    public JournalsViewModel()
    {
        _databaseService = new DatabaseService();
            

        // Keep old message handling
        WeakReferenceMessenger.Default.Register<SyncCompletedMessage>(this, (r, m) =>
        {
            Dispatcher.UIThread.Post(async () =>
            {
                await RefreshForInstance(m.Value);
            });
        });

        // Initialize notes
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        await LoadJournals();
    }
    
    private async Task LoadJournals()
    {
        var instances = await _databaseService.ReadAllAsync<DavConfig>();
        foreach(var instance in instances.Value.OrderBy(i => i.Name))
        {
            DavInstances.Add(instance);
        }

        var appSettings = await _databaseService.ReadAllAsync<Settings>();
        DebugEnabled = appSettings.Value?.FirstOrDefault().DebugEnabled ?? false;
            
        var notes = await _databaseService.ReadAllAsync<CalDavJournal>(query =>
                query
                    .Include(n => n.DavConfig)
                    .Include(n => n.Categories)
                    .Include(n => n.Attendees)
                    .Include(n => n.Attachments)
                    .Include(n => n.Comments)
                    .Include(n => n.Alarms), true
        );

        if (notes.Value != null) Journals = new ObservableCollection<CalDavJournal>(notes.Value.OrderByDescending(n => n.PublishedDate));

        SelectedJournal = new();
        ApplyGrouping();
    }
    
    private async Task RefreshForInstance(DavConfig config)
    {
        GroupedJournals.Clear();
            
        var toRemove = Journals.Where(n => n.DavConfigId == config.Id).ToList();
        foreach (var note in toRemove)
            Journals.Remove(note);

        // Reload only this instance's notes
        var notes = await _databaseService.ReadAllAsync<CalDavJournal>(query =>
            query
                .Where(n => n.DavConfigId == config.Id)
                .Include(n => n.DavConfig)
                .Include(n => n.Categories)
                .Include(n => n.Attendees)
                .Include(n => n.Attachments)
                .Include(n => n.Comments)
                .Include(n => n.Alarms)
        );

        foreach (var note in notes.Value.OrderByDescending(n => n.PublishedDate))
            Journals.Add(note);
            
        ApplyGrouping();
    }
    
    [RelayCommand]
    public async Task DownloadICSCommand(CalDavJournal task)
    {
        if(task == null)
            return;
            
        var ICSUtils = new ICSUtils();
        var IcsContent = ICSUtils.BuildICS(task);
            
        await FilePickerUtils.SaveFileDialog(task.Uid + ".ics", Encoding.UTF8.GetBytes(IcsContent), "ics");
    }

    [RelayCommand]
    public async Task SaveNewJournal()
    {
        if (SelectedJournal != null && selectedDavInstance != null)
        {
            var fullUri = new Uri(
                new Uri(selectedDavInstance.httpUrl),
                $"{SelectedJournal.Id}.ics");
            SelectedJournal.Uri = fullUri.ToString();
            SelectedJournal.DavConfigId = selectedDavInstance.Id;
            SelectedJournal.Uid = Guid.NewGuid().ToString();
            SelectedJournal.IsDirty = true;

            await _databaseService.CreateAsync(SelectedJournal);
            
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

            await _databaseService.UpdateAsync(SelectedJournal);
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

            await _databaseService.UpdateAsync(SelectedJournal);

            Journals.Remove(SelectedJournal);
            SelectedJournal = null;
            Dispatcher.UIThread.Post(() => SelectedJournal = new CalDavJournal());
            
            ApplyGrouping();
        }
    }
    
    [RelayCommand]
    public void AddCategoryCommand()
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Categories.Add(new CalDavCategory { Value = string.Empty });
    }

    [RelayCommand]
    public void RemoveCategoryCommand(CalDavCategory category)
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Categories.Remove(category);
    }
    
    [RelayCommand]
    public void AddCommentCommand()
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Comments.Add(new CalDavComment());
    }

    [RelayCommand]
    public void RemoveCommentCommand(CalDavComment comment)
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Comments.Remove(comment);
    }
    
    [RelayCommand]
    public void AddAttendeeCommand()
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Attendees.Add(new CalDavAttendee());
    }
    
    [RelayCommand]
    public void RemoveAttendeeCommand(CalDavAttendee attendee)
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Attendees.Remove(attendee);
    }
    
    [RelayCommand]
    public async Task AddAttachmentCommand()
    {
        if(SelectedJournal == null)
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
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Attachments.Remove(attachment);
    }
    
    [RelayCommand]
    public void AddAlarmCommand()
    {
        if(SelectedJournal == null)
            return;
            
        SelectedJournal.Alarms.Add(new CalDavAlarm());
    }

    [RelayCommand]
    public void RemoveAlarmCommand(CalDavAlarm alarm)
    {
        if(SelectedJournal == null)
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

            SelectedJournal.PublishedDate = TimeConverter.SetDateOnly( SelectedJournal.PublishedDate, value);
                
            OnPropertyChanged(nameof(PublishedDateOnly));
        }
    }

    public TimeSpan? PublishedTimeOnly
    {
        get => TimeConverter.GetTimeOnly(SelectedJournal?.PublishedDate);

        set
        {
            if (SelectedJournal == null)
                return;

            SelectedJournal.PublishedDate = TimeConverter.SetTimeOnly(SelectedJournal.PublishedDate, value);
                
            OnPropertyChanged(nameof(PublishedTimeOnly));
        }
    }
}