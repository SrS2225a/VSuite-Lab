using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using HeyRed.Mime;

namespace VSuiteLab.Utils;

public class FilePickerUtils
{
    public static async Task SaveFileDialog(string fileName, byte[] data, string? extension = null)
    {
        var mimeType = MimeTypesMap.GetMimeType(fileName);
        var sfd = new SaveFileDialog
        {
            DefaultExtension = extension,
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = mimeType, Extensions = { extension } }
            },
            InitialFileName = fileName
        };
        var lifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var path = await sfd.ShowAsync(lifetime?.MainWindow);
        
        if (!string.IsNullOrEmpty(path))
        {
            await File.WriteAllBytesAsync(path, data);
        }
    }

    public static async Task<IReadOnlyList<IStorageFile>> OpenFileDialog()
    {
        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select file to attach",
            AllowMultiple = true
        });

        return files;
    }
}