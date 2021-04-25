using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher.Shared
{
    public partial class Auth
    {
        [Inject] public IMemoryCache MemoryCache { get; set; }

        [Inject] public IConfiguration Configuration { get; set; }

        [Parameter] public BlazorContext BlazorContext { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        public string PasswordToSet { get; set; }
        public string PasswordToAuth { get; set; }

        public async Task SetPassword()
        {
            if (string.IsNullOrEmpty(PasswordToSet))
                return;

            var settingModel = new AppSettingModel
            {
                AuthKey = SHA512.HashData(Encoding.UTF8.GetBytes(PasswordToSet))
            };
            await using var stream = File.OpenWrite("app.json");
            await JsonSerializer.SerializeAsync(stream, settingModel);
            await JSRuntime.InvokeVoidAsync("location.reload");
        }

        public async Task AuthPassword()
        {
            var passwordHashString = Configuration["AuthKey"];
            var passwordHash = Convert.FromBase64String(passwordHashString);
            if (passwordHash.SequenceEqual(SHA512.HashData(Encoding.UTF8.GetBytes(PasswordToAuth))))
            {
                MemoryCache.Set(BlazorContext.ClientIp, true);
                await JSRuntime.InvokeVoidAsync("location.reload");
            }
        }
    }
}