using System.Threading.Tasks;
using Octokit;
using Updatum;

namespace VSuiteLab.Services;

public class UpdateService
{
    private readonly UpdatumManager _updater;

    public UpdateService()
    {
        _updater = new UpdatumManager("SrS2225a", "VSuite-Lab")
        {
            AssetRegexPattern = "^vsuitelab-.*(linux-x64|linux-arm64|win-x64|win-arm64|osx-x64|osx-arm64).*",
            
            InstallUpdateWindowsExeType = UpdatumWindowsExeType.Installer,
            
            InstallUpdateSingleFileExecutableNameStrategy =
                UpdatumSingleFileExecutableNameStrategy.EntryApplicationName,

            InstallUpdateSingleFileExecutableName = "VSuiteLab",

            InstallUpdateCodesignMacOSApp = true
        };
    }
    
    /// <summary>
    /// Checks GitHub releases for a compatible update
    /// </summary>
    public async Task<bool> CheckAsync()
    {
        return await _updater.CheckForUpdatesAsync();
    }

    /// <summary>
    /// Latest release (after CheckAsync)
    /// </summary>
    public Release? GetLatestRelease()
    {
        return _updater.LatestRelease;
    }

    /// <summary>
    /// Gets compatible asset for current OS/RID
    /// </summary>
    public ReleaseAsset? GetAsset(Release release)
    {
        return _updater.GetCompatibleReleaseAsset(release);
    }

    /// <summary>
    /// Download update
    /// </summary>
    public async Task<UpdatumDownloadedAsset?> DownloadAsync(Release release)
    {
        return await _updater.DownloadUpdateAsync(release);
    }

    /// <summary>
    /// Install downloaded update
    /// </summary>
    public async Task InstallAsync(UpdatumDownloadedAsset download)
    {
        await _updater.InstallUpdateAsync(download);
    }

    /// <summary>
    /// Optional: expose progress/state
    /// </summary>
    public UpdatumManager Updater => _updater;
}