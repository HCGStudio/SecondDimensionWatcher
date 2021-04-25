using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorrentController : ControllerBase
    {
        private readonly AppDataContext _dataContext;
        private readonly FileExtensionContentTypeProvider _extensionContentTypeProvider;
        private readonly IMemoryCache _memoryCache;

        public TorrentController(
            AppDataContext dataContext,
            FileExtensionContentTypeProvider extensionContentTypeProvider,
            IMemoryCache memoryCache)
        {
            _dataContext = dataContext;
            _extensionContentTypeProvider = extensionContentTypeProvider;
            _memoryCache = memoryCache;
        }


        [HttpGet("File/{hash}")]
        public async Task<IActionResult> GetFileAsync([FromRoute] string hash, [FromQuery] string relativePath)
        {
            if (_memoryCache.Get<bool?>(HttpContext.Connection.RemoteIpAddress?.ToString()) is null or false)
                return NotFound();
            var info = await _dataContext
                .AnimationInfo
                .Where(a => a.Hash == hash)
                .FirstOrDefaultAsync();
            if (info == null)
                return NotFound();
            var fileInfo = new FileInfo(Path.Combine(info.StorePath, relativePath));
            if (System.IO.File.Exists(info.StorePath)) fileInfo = new(info.StorePath);

            FileResult file;

            if (_extensionContentTypeProvider.TryGetContentType(fileInfo.Extension, out var contentType))
                file = File(
                    System.IO.File.OpenRead(fileInfo.FullName),
                    contentType,
                    fileInfo.Name);
            else
                file = File(
                    System.IO.File.OpenRead(fileInfo.FullName),
                    "application/octet-stream",
                    fileInfo.Name);

            file.EnableRangeProcessing = true;
            return file;
        }
    }
}