using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Pages
{
    public partial class Search
    {
        public Search()
        {
            ModalCallBack = info =>
            {
                ModalContent = $"是否删除{info.Id}及其所有文件？";
                ModalTitle = "删除确认";
                ToBeDelete = info;
                StateHasChanged();
            };
        }

        [Parameter] public string Term { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public AppDataContext DbContext { get; set; }
        [Inject] public HttpClient Http { get; set; }

        public IEnumerable<AnimationInfo> AnimationInfos { get; set; } = Array.Empty<AnimationInfo>();

        public string ModalTitle { get; set; } = string.Empty;
        public string ModalContent { get; set; } = string.Empty;
        public Action<AnimationInfo> ModalCallBack { get; }
        public AnimationInfo ToBeDelete { get; set; }
        public int TotalPage { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }


        protected override async Task OnParametersSetAsync()
        {
            TotalCount = await DbContext
                .AnimationInfo
                .Where(a => a.Id.Contains(Term))
                .CountAsync();
            TotalPage = (int) Math.Ceiling(TotalCount / 10d);
            await SwitchPage(1);
        }

        public async ValueTask SwitchPage(int newPage)
        {
            CurrentPage = newPage;
            AnimationInfos = Array.Empty<AnimationInfo>();
            AnimationInfos = await DbContext
                .AnimationInfo
                .Where(a => a.Id.Contains(Term))
                .OrderByDescending(a => a.IsFinished)
                .ThenByDescending(a => a.IsTracked)
                .ThenByDescending(a => a.TrackTime)
                .ThenByDescending(a => a.PublishTime)
                .Skip((newPage - 1) * 10)
                .Take(10)
                .ToArrayAsync();
            await JSRuntime.InvokeVoidAsync("window.scrollTo", 0, 0);
        }

        public async ValueTask Delete()
        {
            var infoInDb = await DbContext.AnimationInfo.FindAsync(ToBeDelete.Id);
            await Http.GetAsync("/api/v2/torrents/delete?deleteFiles=true&hashes=" + infoInDb.Hash);
            infoInDb.IsTracked = false;
            AnimationInfos = Array.Empty<AnimationInfo>();
            await DbContext.SaveChangesAsync();
            await JSRuntime.InvokeVoidAsync("location.reload");
        }
    }
}