using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public partial class BannerMessage : ObservableObject
{
    public int CurrentIndex { get; set; }
    public int MaxIndex { get; set; }
    public string ServerName { get; set; }

    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private bool isError = false;
    

    public async Task UpdateMessage(
        string action,
        int stepIndex,
        int stepMax)
    {
        Message =
            $"Syncing {ServerName} {CurrentIndex} of {MaxIndex}\n" +
            $"{action} ({stepIndex} of {stepMax})";
        IsError = false;
        
        OnPropertyChanged(nameof(Message));
        await Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
    }

    public async Task UpdateMessageWithError(
        string action,
        int stepIndex,
        int stepMax)
    {
        Message = $"{action} ({stepIndex} of {stepMax}) for {ServerName}";
        IsError = true;
        
        OnPropertyChanged(nameof(Message));
        await Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
    }

    public async Task ClearMessage()
    {
        Message = string.Empty;
        
        OnPropertyChanged(nameof(Message));
        await Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
    }
}