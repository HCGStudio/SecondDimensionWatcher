using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SecondDimensionWatcher.Controllers;
using SecondDimensionWatcher.Data;

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

        public IEnumerable<AnimationInfo> Info { get; set; } = Array.Empty<AnimationInfo>();

        [Inject] public FeedController FeedController { get; set; }

        [Inject] public AppDataContext DbContext { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPage { get; set; }

        public Selector Selected { get; set; }

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
                    .OrderByDescending(a => a.PublishTime)
                    .Skip((dest - 1) * 10)
                    .Take(10)
                    .ToArrayAsync(),
                Selector.Downloading => Info,
                Selector.Finished => Info,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public async ValueTask SetSelectorAndUpdateContent(Selector newValue)
        {
            if (newValue == Selected)
                return;
            Selected = newValue;
            TotalPage = Selected switch
            {
                Selector.Remote => (int) Math.Ceiling(
                    await DbContext.AnimationInfo.Where(a => !a.IsTracked).CountAsync() / 10d),
                Selector.Local => (int) Math.Ceiling(
                    await DbContext.AnimationInfo.Where(a => a.IsTracked).CountAsync() / 10d),
                Selector.Downloading => 0,
                Selector.Finished => 0,
                _ => throw new ArgumentOutOfRangeException()
            };
            await SwitchPage(1);
        }

        public async ValueTask RefreshAsync()
        {
            ;
        }
    }
}