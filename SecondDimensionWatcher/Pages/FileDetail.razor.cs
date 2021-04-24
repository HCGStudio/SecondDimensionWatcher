using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Pages
{
    public partial class FileDetail
    {
        [Parameter] public string Hash { get; set; }

        [Inject] public AppDataContext DbContext { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public AnimationInfo AnimationInfo { get; set; }
        public bool IsFolder { get; set; }
        public string Base { get; set; }
        public Stack<string> CurrentPath { get; } = new();
        public bool IsReady { get; set; }
        public string CurrentFullPath => Path.Combine(Base, Path.Combine(CurrentPath.ToArray()));
        public DirectoryInfo CurrentInfo => new(CurrentFullPath);

        protected override async Task OnParametersSetAsync()
        {
            AnimationInfo = await DbContext.AnimationInfo.Where(a => a.Hash == Hash).FirstOrDefaultAsync();
            Base = Path.GetFullPath(AnimationInfo.StorePath);
            IsFolder = Directory.Exists(Base);
            IsReady = true;
        }

        public void PushPath(string path)
        {
            if (path != "..")
                CurrentPath.Push(path);
            else
                CurrentPath.Pop();
        }

    }
}