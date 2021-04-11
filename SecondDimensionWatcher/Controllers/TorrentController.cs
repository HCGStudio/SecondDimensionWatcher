using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ByteSizeLib;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorrentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDataContext _dataContext;
        private readonly FileExtensionContentTypeProvider _extensionContentTypeProvider;
        private readonly HttpClient _http;
        private readonly ILogger<FeedController> _logger;


        public TorrentController(IConfiguration configuration, AppDataContext dataContext,
            ILogger<FeedController> logger, HttpClient http,
            FileExtensionContentTypeProvider extensionContentTypeProvider)
        {
            _configuration = configuration;
            _dataContext = dataContext;
            _logger = logger;
            _http = http;
            _extensionContentTypeProvider = extensionContentTypeProvider;
            http.BaseAddress = new(_configuration["DownloadSetting:BaseAddress"]);
            http.DefaultRequestHeaders.UserAgent.Add(
                new("SecondDimensionWatcher", "1.0"));
        }

        protected async Task<TorrentInfo[]> GetTorrentStatus(string hash, CancellationToken cancellationToken)
        {
            var response = await _http.GetStreamAsync("/api/v2/torrents/info?hashes=" + hash, cancellationToken);
            return await JsonSerializer.DeserializeAsync<TorrentInfo[]>(response, cancellationToken: cancellationToken);
        }

        [HttpGet("Query/{hash}")]
        public async Task<IActionResult> QueryStatusAsync([FromRoute] string hash, CancellationToken cancellationToken)
        {
            var status = await GetTorrentStatus(hash, cancellationToken);
            if (!HttpContext.WebSockets.IsWebSocketRequest)
                return Ok(status.Adapt<TorrentInfoDto[]>());

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            while (webSocket.State == WebSocketState.Open)
            {
                status = await GetTorrentStatus(hash, cancellationToken);
                await webSocket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(status.Adapt<TorrentInfoDto[]>()),
                    WebSocketMessageType.Text, true, cancellationToken);
                await Task.Delay(100, cancellationToken);
            }

            return Ok();
        }

        [HttpGet("Pause/{hash}")]
        public async Task<IActionResult> PauseAsync([FromRoute] string hash, CancellationToken cancellationToken)
        {
            await _http.GetAsync("/api/v2/torrents/pause?hashes=" + hash, cancellationToken);
            return Ok();
        }

        [HttpGet("Resume/{hash}")]
        public async Task<IActionResult> ResumeAsync([FromRoute] string hash, CancellationToken cancellationToken)
        {
            await _http.GetAsync("/api/v2/torrents/resume?hashes=" + hash, cancellationToken);
            return Ok();
        }

        [HttpDelete("Untrack/{id}")]
        public async Task<IActionResult> UntrackAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            var animationInfo = await _dataContext.AnimationInfo.FindAsync(id, cancellationToken);
            if (animationInfo == null)
                return NotFound();

            animationInfo.IsTracked = false;
            await _dataContext.SaveChangesAsync(cancellationToken);
            return Ok();
        }


        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            var animationInfo = await _dataContext.AnimationInfo.FindAsync(id, cancellationToken);
            if (animationInfo == null)
                return NotFound();

            await _http.GetAsync("/api/v2/torrents/resume?deleteFiles=true&hashes=" + animationInfo.Hash,
                cancellationToken);

            animationInfo.IsTracked = false;
            await _dataContext.SaveChangesAsync(cancellationToken);
            return Ok();
        }

        [HttpGet("FileName/{hash}")]
        public async IAsyncEnumerable<string> GetFileNames([FromRoute] string hash, [FromQuery] string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            if (System.IO.File.Exists(info.SavePath))
            {
                var fileInfo = new FileInfo(info.SavePath);
                yield return fileInfo.Name;
                yield break;
            }

            foreach (var fileInfo in new DirectoryInfo(Path.Combine(info.SavePath, relativePath)).EnumerateFiles())
                yield return fileInfo.Name;
        }

        [HttpGet("File/{hash}")]
        public async Task<IActionResult> GetFileAsync([FromRoute] string hash, [FromQuery] string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            var fileInfo = new FileInfo(Path.Combine(info.SavePath, relativePath));
            if (System.IO.File.Exists(info.SavePath)) fileInfo = new(info.SavePath);

            FileResult file;
            if (_extensionContentTypeProvider.TryGetContentType(fileInfo.Extension, out var contentType))
                file = File(System.IO.File.OpenRead(fileInfo.FullName), contentType, fileInfo.Name);
            else
                file = File(System.IO.File.OpenRead(fileInfo.FullName), "application/octet-stream", fileInfo.Name);

            file.EnableRangeProcessing = true;
            return file;
        }

        internal async Task<string> GetMimeAsync(string hash, string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            var fileInfo = new FileInfo(Path.Combine(info.SavePath, relativePath));
            if (System.IO.File.Exists(info.SavePath)) fileInfo = new(info.SavePath);

            return _extensionContentTypeProvider
                .TryGetContentType(fileInfo.Extension, out var contentType)
                ? contentType
                : "application/octet-stream";
        }

        internal async Task<string> GetRealPathAsync(string hash, string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            var fileInfo = new FileInfo(Path.Combine(info.SavePath, relativePath));
            if (System.IO.File.Exists(info.SavePath)) fileInfo = new(info.SavePath);
            return fileInfo.FullName;
        }

        private bool CanBeSubtitle(string ext)
        {
            return ext == ".ass" || ext == ".srt";
        }

        internal async IAsyncEnumerable<string> PossibleSubtitles([FromRoute] string hash,
            [FromQuery] string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            var fileInfo = new FileInfo(Path.Combine(info.SavePath, relativePath));
            if (System.IO.File.Exists(info.SavePath))
                yield break;
            if (fileInfo.Directory == null)
                yield break;
            foreach (var file in fileInfo.Directory.EnumerateFiles())
                if (Path.GetFileNameWithoutExtension(file.Name)
                    .StartsWith(Path.GetFileNameWithoutExtension(fileInfo.Name)) && CanBeSubtitle(file.Extension))
                    yield return Path.Combine(relativePath, "..", file.Name);
        }

        [HttpGet("DirectoryName/{hash}")]
        public async IAsyncEnumerable<string> GetDirectoryNames([FromRoute] string hash,
            [FromQuery] string relativePath)
        {
            var info = (await GetTorrentStatus(hash, default)).First();
            if (System.IO.File.Exists(info.SavePath)) yield break;

            foreach (var directoryInfo in new DirectoryInfo(Path.Combine(info.SavePath, relativePath))
                .EnumerateDirectories()) yield return directoryInfo.Name;
        }

        private List<DownloadContent> EnumerateFiles(DirectoryInfo info, string relativePath)
        {
            var list = new List<DownloadContent>();
            foreach (var dir in info.EnumerateDirectories())
                list.Add(new DownloadDirectoryContent
                {
                    Name = dir.Name,
                    RelativePath = relativePath,
                    SubContents = EnumerateFiles(dir, Path.Combine(relativePath, dir.Name))
                });
            list.AddRange(info.EnumerateFiles()
                .Select(fileInfo => new DownloadContent
                {
                    Name = fileInfo.Name,
                    RelativePath = relativePath
                }));
            return list;
        }

        [HttpGet("Download/{id}")]
        public async Task<IActionResult> DownloadAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            id = HttpUtility.UrlDecode(id);
            var animationInfo = await _dataContext.AnimationInfo.FindAsync(id);
            if (animationInfo == null)
                return NotFound();
            if (animationInfo.IsTracked)
            {
                _logger.LogInformation($"The torrent {animationInfo.Description} has already added.");
                return Ok();
            }

            var content = new MultipartFormDataContent
            {
                {new ByteArrayContent(animationInfo.TorrentData), "torrent", $"{animationInfo.Hash}.torrent"}
            };
            var dir = Path.GetFullPath(_configuration["DownloadDir"]);
            if (!string.IsNullOrWhiteSpace(dir))
                content.Add(new StringContent(Path.GetFullPath(dir)), "savepath");
            var response = await _http.PostAsync("/api/v2/torrents/add", content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                animationInfo.IsTracked = true;
                await _dataContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation($"The torrent {animationInfo.Description} successfully added.");
                return Ok();
            }

            return BadRequest();
        }

        public class TorrentInfoDto
        {
            [JsonConverter(typeof(ReadableTimeSpanConverter))]
            public int Eta { get; set; }

            public string State { get; set; }
            public double Progress { get; set; }

            [JsonConverter(typeof(ReadableSpeedConverter))]
            public int Speed { get; set; }
        }

        public class ReadableTimeSpanConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return int.Parse(reader.GetString() ?? throw new InvalidOperationException());
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(new TimeSpan(0, 0, value).ToString("g", CultureInfo.CurrentCulture));
            }
        }

        public class ReadableSpeedConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return int.Parse(reader.GetString() ?? throw new InvalidOperationException());
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(ByteSize.FromBytes(value).ToString("#.#") + "/s");
            }
        }

        public class TorrentInfo
        {
            [JsonPropertyName("eta")] public int Eta { get; set; }

            [JsonPropertyName("state")] public string State { get; set; }

            [JsonPropertyName("progress")] public double Progress { get; set; }

            [JsonPropertyName("content_path")] public string SavePath { get; set; }

            [JsonPropertyName("dlspeed")] public int Speed { get; set; }
        }

        public class DownloadContent
        {
            public string Name { get; set; }
            public string RelativePath { get; set; }
        }

        public class DownloadDirectoryContent : DownloadContent
        {
            public ICollection<DownloadContent> SubContents { get; set; }
        }
    }
}