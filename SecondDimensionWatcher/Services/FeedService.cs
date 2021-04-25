using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using CodeHollow.FeedReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Services
{
    public class FeedService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDataContext _dataContext;
        private readonly HttpClient _http;
        private readonly ILogger<FeedService> _logger;

        public FeedService(ILogger<FeedService> logger, AppDataContext dataContext, IConfiguration configuration,
            HttpClient client)
        {
            _logger = logger;
            _dataContext = dataContext;
            _configuration = configuration;
            _http = client;
        }

        private static AsyncLock Mutex { get; } = new();

        public async ValueTask RefreshAsync(CancellationToken cancellationToken)
        {
            using var asyncLock = await Mutex.LockAsync(cancellationToken);
            var feedUrls = _configuration.GetSection("FetchUrls").Get<string[]>();
            foreach (var feedUrl in feedUrls)
            {
                var feed = await FeedReader.ReadAsync(feedUrl, cancellationToken);
                _logger.LogInformation($"Fetch {feed.Items.Count} items from remote.");
                var list = (from item in feed.Items
                    let element = item.SpecificItem.Element
                    let torrentDate = element.Element("{https://mikanani.me/0.1/}torrent")
                        ?.Element("{https://mikanani.me/0.1/}pubDate")
                        ?.Value
                    let url = element.Element("enclosure")?.Attribute("url")?.Value
                    select new AnimationInfo
                    {
                        Id = item.Id,
                        Description = item.Description,
                        PublishTime = DateTimeOffset.Parse(torrentDate),
                        TorrentUrl = url
                    }).ToList();

                foreach (var content in list)
                    if (await _dataContext.AnimationInfo.FindAsync(content.Id) == null)
                    {
                        content.TorrentData = await _http.GetByteArrayAsync(content.TorrentUrl, cancellationToken);
                        var parser = new BencodeParser();
                        content.Hash = BitConverter
                            .ToString(SHA1.HashData(
                                parser.Parse<BDictionary>(content.TorrentData)["info"]
                                    .EncodeAsBytes()))
                            .Replace("-", "");
                        await _dataContext.AddAsync(content, cancellationToken);
                    }
            }

            await _dataContext.SaveChangesAsync(cancellationToken);
        }
    }
}