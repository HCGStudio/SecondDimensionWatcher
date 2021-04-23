using System.Collections.Generic;

namespace SecondDimensionWatcher.Data
{
    public class DownloadDirectoryContent : DownloadContent
    {
        public ICollection<DownloadContent> SubContents { get; set; }
    }
}