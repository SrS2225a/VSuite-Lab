using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public partial class SyncProgress : ObservableObject
{
    [ObservableProperty] private string message = "";
    [ObservableProperty] private bool isError;
    [ObservableProperty] private bool isCompleted;
    [ObservableProperty] private int currentIndex;
    [ObservableProperty] private int maxIndex;
    [ObservableProperty] private string serverName = "";
    [ObservableProperty] private bool success;

    public DavConfig Config { get; set; } = null!;
    public string Url { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public void Update(string msg, int current, int max, bool error = false)
    {
        Message = msg;
        CurrentIndex = current;
        MaxIndex = max;
        IsError = error;
    }

    public void Complete(bool success)
    {
        Success = success;
        IsCompleted = true;
        Timestamp = DateTime.Now;
    }
}