using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Pages
{
    public partial class PlayBack
    {
        [Parameter] public string Hash { get; set; }

        [Parameter] public string Path { get; set; }

        [Inject] public AppDataContext DbContext { get; set; }

        [Inject] public FileExtensionContentTypeProvider FileExtensionContentTypeProvider { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public AnimationInfo AnimationInfo { get; set; }


        public FileInfo FileInfo { get; set; }
        public string Mime { get; set; }
        public bool IsReady { get; set; }
        public string FileUrl => $"/api/Torrent/File/{Hash}?relativePath={HttpUtility.UrlEncode(Path)}";

        protected override async Task OnParametersSetAsync()
        {
            AnimationInfo = await DbContext.AnimationInfo.Where(a => a.Hash == Hash).FirstOrDefaultAsync();
            if (File.Exists(AnimationInfo.StorePath))
            {
                FileInfo = new(AnimationInfo.StorePath);
                IsReady = true;
            }
            else
            {
                var dir = new DirectoryInfo(AnimationInfo.StorePath);
                FileInfo = new(System.IO.Path.Combine(AnimationInfo.StorePath, Path));
                IsReady = true;
                //Avoid .. attack
                if (!FileInfo.FullName.Contains(dir.FullName))
                {
                    FileInfo = null;
                    IsReady = false;
                }
            }

            Mime = FileExtensionContentTypeProvider
                .TryGetContentType(FileInfo?.Extension, out var mime)
                ? mime
                : "application/octet-stream";
            StateHasChanged();
            await JSRuntime.InvokeVoidAsync("play");
        }
    }
}