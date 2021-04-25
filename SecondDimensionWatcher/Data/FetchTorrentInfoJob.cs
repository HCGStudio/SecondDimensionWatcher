using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Quartz;

namespace SecondDimensionWatcher.Data
{
    public class FetchTorrentInfoJob : IJob
    {
        private readonly AppDataContext _dataContext;
        private readonly HttpClient _http;
        private readonly IMemoryCache _memoryCache;

        public FetchTorrentInfoJob(AppDataContext dataContext,
            IMemoryCache memoryCache,
            HttpClient http)
        {
            _dataContext = dataContext;
            _memoryCache = memoryCache;
            _http = http;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
                var shouldUpdated = await _dataContext.AnimationInfo
                    .Where(a => a.IsTracked && !a.IsFinished)
                    .Select(a => a.Hash)
                    .ToArrayAsync();
                if (!shouldUpdated.Any())
                    continue;
                var hashes = string.Join('|', shouldUpdated);
                var result = await GetTorrentStatus(hashes, CancellationToken.None);
                var finished = new List<TorrentInfo>();
                foreach (var info in result)
                {
                    if (info.State == "uploading" || info.State.Contains("UP")) finished.Add(info);

                    _memoryCache.Set(info.Hash.ToUpper(), info);
                }

                foreach (var torrentInfo in finished)
                {
                    var info = await _dataContext.AnimationInfo
                        .Where(a => a.Hash == torrentInfo.Hash.ToUpper())
                        .FirstOrDefaultAsync();
                    if (info == null)
                        continue;
                    info.IsFinished = true;
                    info.StorePath = torrentInfo.SavePath;
                }

                await _dataContext.SaveChangesAsync();
            }
        }

        private async ValueTask<TorrentInfo[]> GetTorrentStatus(string hash, CancellationToken cancellationToken)
        {
            var response = await _http.GetStreamAsync("/api/v2/torrents/info?hashes=" + hash, cancellationToken);
            return await JsonSerializer.DeserializeAsync<TorrentInfo[]>(response, cancellationToken: cancellationToken);
        }
    }
}