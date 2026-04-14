using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

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
        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!;
    
        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = fileName,
            DefaultExtension = extension,
            FileTypeChoices = extension != null
                ? new List<FilePickerFileType>
                {
                    new(fileName)
                    {
                        Patterns = [$"*.{extension}"]
                    }
                }
                : null
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(data);
        }
    }
    
    /// <summary>
    /// Open file dialog
    /// </summary>
    /// <returns>A list of selected files</returns>   

    public static async Task<IReadOnlyList<IStorageFile>> OpenFileDialog()
    {
        var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current?.ApplicationLifetime!).MainWindow;
        var files = await window?.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select file to attach",
            AllowMultiple = true
        })!;

        return files;
    }
}