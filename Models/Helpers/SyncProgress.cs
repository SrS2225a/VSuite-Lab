using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models.Helpers
{
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

        public void Update(string msg, bool error = false)
        {
            Message = $"Syncing {ServerName} {CurrentIndex} of {MaxIndex}\n" + msg;
            IsError = error;
        }

        public void Complete(bool success)
        {
            Success = success;
            IsCompleted = true;
            Timestamp = DateTime.Now;
        }
    }
}