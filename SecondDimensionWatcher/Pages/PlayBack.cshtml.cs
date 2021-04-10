using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SecondDimensionWatcher.Controllers;

namespace SecondDimensionWatcher.Pages
{
    public class PlayBackModel : PageModel
    {
        private readonly TorrentController _torrentController;

        public PlayBackModel(TorrentController torrentController)
        {
            _torrentController = torrentController;
        }

        public string PlayBackUrl { get; set; }
        public string Mime { get; set; }

        public List<string> AssUrls { get; set; }
        public List<string> SrtUrls { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }

        public string ToUrl(string hash, string path)
        {
            return $"/api/Torrent/File/{hash}?relativePath={HttpUtility.UrlEncode(path)}";
        }

        public async Task OnGetAsync([FromQuery] string hash, [FromQuery] string path)
        {
            Hash = hash;
            PlayBackUrl = ToUrl(hash, path);
            Name = path.Split("/").Last();
            Mime = await _torrentController.GetMimeAsync(hash, path);
            var subtitles = new List<string>();
            //Can not use System.Linq.Async due to the use of EF core.
            await foreach (var sub in _torrentController.PossibleSubtitles(hash, path)) subtitles.Add(sub);
            AssUrls = subtitles.Where(s => s.EndsWith(".ass")).ToList();
            SrtUrls = subtitles.Where(s => s.EndsWith(".srt")).ToList();
        }
    }
}