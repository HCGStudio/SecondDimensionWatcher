using System.Threading.Tasks;
using Quartz;
using SecondDimensionWatcher.Controllers;

namespace SecondDimensionWatcher.Data
{
    public class RssUpdateJob : IJob
    {
        private readonly FeedController _feedController;

        public RssUpdateJob(FeedController feedController)
        {
            _feedController = feedController;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _feedController.RefreshAsync(default);
        }
    }
}