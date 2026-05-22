using Microsoft.AspNetCore.Http;

namespace SWP391_AutoWashPro_BE.Service.MediaService;

public interface IService
{
    public Task<string> UploadImageAsync(IFormFile file);
}