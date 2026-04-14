using System;
using System.Threading.Tasks;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Models;

namespace VSuiteLab.Services;

public class CalDavService
{
    private readonly DatabaseService _databaseService;

    public CalDavService()
    {
        _databaseService = new DatabaseService();
    }
    
    
    /// <summary>
    /// Adds a new mount to the database
    /// </summary>
    /// <param name="davConfig"></param>
    /// <returns> A <see cref="VSuiteLab.Models.Helpers.StatusResponse{String}"/> containing the result of the operation. </returns>
    public async Task<StatusResponse<string>> addMount(DavConfig davConfig)
    {
        var client = DavMiddlewareService.GetDavClient(davConfig);
        var hasCalDav = await DavMiddlewareService.HasCalDav(client, davConfig);
        if (hasCalDav.Success)
        {
            var calDavFeatures = await DavMiddlewareService.CheckDavFeatures(client, davConfig);
            if (calDavFeatures.Success)
            {
                davConfig = calDavFeatures.Value;
                if (!davConfig.SupportsVjournal && !davConfig.SupportsVtodo)
                    return StatusResponse<string>.Error("This server does not support VTODO or VJOURNAL.");
            }
            
            var davAlreadyExists = await _databaseService.ReadExistsWhereAsync<DavConfig>(d => d.httpUrl == davConfig.httpUrl);
            if (!davAlreadyExists.Success || davAlreadyExists.Value)
            {
                return StatusResponse<string>.Error("Mount already exists.");
            }
            
            var databaseResponse = await _databaseService.CreateAsync(davConfig);
            if (!databaseResponse.Success)
            {
                return new StatusResponse<string>
                {
                    Success = false,
                    Value = databaseResponse.Message,
                    Message = databaseResponse.Message
                };
            }
        }
        return hasCalDav;
    }
    
   
}