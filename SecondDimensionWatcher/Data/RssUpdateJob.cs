using System.Threading.Tasks;
using Quartz;
using SecondDimensionWatcher.Services;

namespace SecondDimensionWatcher.Data
{
    public class RssUpdateJob : IJob
    {
        private readonly FeedService _feedService;

        public RssUpdateJob(FeedService feedService)
        {
            _feedService = feedService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _feedService.RefreshAsync(default);
        }
    }
}