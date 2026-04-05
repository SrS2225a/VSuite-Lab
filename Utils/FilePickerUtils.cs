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
    /// <summary>
    /// Save file dialog
    /// </summary>
    /// <param name="fileName">The name of the file to save</param>
    /// <param name="data">The raw stream data to save</param>
    /// <param name="extension">The file extension to use</param>
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
    
    /// <summary>
    /// Open file dialog
    /// </summary>
    /// <returns>A list of selected files</returns>   

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