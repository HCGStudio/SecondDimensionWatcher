using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using Microsoft.AspNetCore.Components;
using SecondDimensionWatcher.Controllers;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Shared
{
    public partial class AnimationView : IDisposable
    {
        public enum TorrentStatus
        {
            UnTracked,
            Running,
            Paused,
            Finished,
            Error
        }

        [Inject] public TorrentController TorrentController { get; set; }

        [Inject] public FeedController FeedController { get; set; }

        [Parameter] public AnimationInfo AnimationInfo { get; set; }

        public TorrentInfo Info { get; set; }
        public string ProgressClass { get; set; } = string.Empty;
        public double ProgressValue { get; set; }

        public string RemainTimeString => new TimeSpan(0, 0, Info?.Eta ?? 0)
            .ToString("g", CultureInfo.CurrentCulture);

        public string SpeedString => ByteSize.FromBytes(Info?.Speed ?? 0).ToString("#.#") + "/s";

        public TorrentStatus Status { get; set; }

        public Task FetchTask { get; set; }
        public CancellationTokenSource TokenSource { get; } = new();

        public void Dispose()
        {
            TokenSource.Cancel();
        }

        public void SetSuitableClass()
        {
            ProgressClass = Status switch
            {
                TorrentStatus.UnTracked => "",
                TorrentStatus.Running => "progress-bar progress-bar-animated progress-bar-striped",
                TorrentStatus.Paused => "progress-bar progress-bar-striped bg-warning",
                TorrentStatus.Finished => "progress-bar progress-bar-striped bg-success",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public TorrentStatus GetStatusFromString(string state)
        {
            if (state == "uploading" || state.Contains("UP"))
                return TorrentStatus.Finished;
            if (state is "pausedDL" or "checkingResumeData")
                return TorrentStatus.Paused;
            if (state == "downloading" || state.Contains("DL"))
                return TorrentStatus.Running;
            return TorrentStatus.Error;
        }

        protected override void OnParametersSet()
        {
            if (AnimationInfo.IsTracked)
                FetchTask = InvokeAsync(async () =>
                {
                    var token = TokenSource.Token;

                    while (Status != TorrentStatus.Finished || !token.IsCancellationRequested)
                    {
                        Info = await TorrentController.GetTorrentInfoOnce(AnimationInfo.Hash, token);
                        Status = GetStatusFromString(Info.State);
                        SetSuitableClass();
                        ProgressValue = Info.Progress;
                        StateHasChanged();
                        await Task.Delay(100, token);
                    }
                });
        }

        public async ValueTask Pause()
        {
            await TorrentController.PauseAsync(AnimationInfo.Hash, CancellationToken.None);
        }

        public async ValueTask Resume()
        {
            await TorrentController.ResumeAsync(AnimationInfo.Hash, CancellationToken.None);
        }

        public async ValueTask Download()
        {
            await TorrentController.DownloadAsync(AnimationInfo.Id, CancellationToken.None);
        }
    }
}