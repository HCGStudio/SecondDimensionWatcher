using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;
using SecondDimensionWatcher.Services;

namespace SecondDimensionWatcher.Pages
{
    public partial class Index
    {
        public enum Selector
        {
            Remote,
            Local,
            Downloading,
            Finished
        }

        public Index()
        {
            ModalCallBack = info =>
            {
                ModalContent = $"是否删除{info.Id}及其所有文件？";
                ModalTitle = "删除确认";
                ToBeDelete = info;
                StateHasChanged();
            };
        }

        public IEnumerable<AnimationInfo> Info { get; set; } = Array.Empty<AnimationInfo>();

        [Inject] public FeedService FeedService { get; set; }

        [Inject] public AppDataContext DbContext { get; set; }

        [Inject] public HttpClient Http { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }

        public string ModalTitle { get; set; } = string.Empty;
        public string ModalContent { get; set; } = string.Empty;
        public Action<AnimationInfo> ModalCallBack { get; }
        public AnimationInfo ToBeDelete { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPage { get; set; }

        public Selector Selected { get; set; }

        public async ValueTask Delete()
        {
            var infoInDb = await DbContext.AnimationInfo.FindAsync(ToBeDelete.Id);
            await Http.GetAsync("/api/v2/torrents/delete?deleteFiles=true&hashes=" + infoInDb.Hash);
            infoInDb.IsTracked = false;
            Info = Array.Empty<AnimationInfo>();
            await DbContext.SaveChangesAsync();
            await SwitchPage(CurrentPage);
        }

        protected override async Task OnInitializedAsync()
        {
            Info = await DbContext.AnimationInfo
                .Where(a => !a.IsTracked)
                .OrderByDescending(a => a.PublishTime)
                .Take(10)
                .ToArrayAsync();
            TotalPage = (int) Math.Ceiling(
                await DbContext.AnimationInfo.Where(a => !a.IsTracked).CountAsync() / 10d);
        }

        public async ValueTask SwitchPage(int dest)
        {
            CurrentPage = dest;
            Info = Selected switch
            {
                Selector.Remote => await DbContext.AnimationInfo
                    .Where(a => !a.IsTracked)
                    .OrderByDescending(a => a.PublishTime)
                    .Skip((dest - 1) * 10)
                    .Take(10)
                    .ToArrayAsync(),
                Selector.Local => await DbContext.AnimationInfo
                    .Where(a => a.IsTracked)
                    .OrderByDescending(a => a.TrackTime)
                    .ThenByDescending(a => a.PublishTime)
                    .Skip((dest - 1) * 10)
                    .Take(10)
                    .ToArrayAsync(),
                Selector.Downloading => await DbContext.AnimationInfo
                    .Where(a => a.IsTracked && !a.IsFinished)
                    .OrderByDescending(a => a.TrackTime)
                    .ThenByDescending(a => a.PublishTime)
                    .Skip((dest - 1) * 10)
                    .Take(10)
                    .ToArrayAsync(),
                Selector.Finished => await DbContext.AnimationInfo
                    .Where(a => a.IsFinished)
                    .OrderByDescending(a => a.TrackTime)
                    .ThenByDescending(a => a.PublishTime)
                    .Skip((dest - 1) * 10)
                    .Take(10)
                    .ToArrayAsync(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public async ValueTask SetSelectorAndUpdateContent(Selector newValue)
        {
            if (newValue == Selected)
                return;
            Selected = newValue;
            //Force unload old
            Info = Array.Empty<AnimationInfo>();
            TotalPage = Selected switch
            {
                Selector.Remote => (int) Math.Ceiling(
                    await DbContext.AnimationInfo.Where(a => !a.IsTracked).CountAsync() / 10d),
                Selector.Local => (int) Math.Ceiling(
                    await DbContext.AnimationInfo.Where(a => a.IsTracked).CountAsync() / 10d),
                Selector.Downloading => (int) Math.Ceiling(
                    await DbContext.AnimationInfo
                        .Where(a => a.IsTracked && !a.IsFinished)
                        .CountAsync() / 10d),
                Selector.Finished => (int) Math.Ceiling(
                    await DbContext.AnimationInfo
                        .Where(a => a.IsFinished)
                        .CountAsync() / 10d),
                _ => throw new ArgumentOutOfRangeException()
            };
            await SwitchPage(1);
        }

        public async ValueTask RefreshAsync()
        {
            await FeedService.RefreshAsync(CancellationToken.None);
            await JSRuntime.InvokeVoidAsync("location.reload");
        }
    }
}