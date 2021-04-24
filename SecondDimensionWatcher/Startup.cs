using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using SecondDimensionWatcher.Controllers;
using SecondDimensionWatcher.Data;

namespace SecondDimensionWatcher
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllers();

            services.AddDbContext<AppDataContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() {Title = "SecondDimensionWatcher", Version = "v1"});
            });
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings.Add(".mkv", "video/webm");
            services.AddSingleton(provider);
            var http = new HttpClient();
            http.BaseAddress = new(Configuration["DownloadSetting:BaseAddress"]);
            http.DefaultRequestHeaders.UserAgent.Add(
                new("SecondDimensionWatcher", "1.0"));
            services.AddSingleton(http);
            services.AddHttpClient<FeedController>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(
                    new("SecondDimensionWatcher", "1.0"));
            });
            services.AddHttpClient<TorrentController>(client =>
            {
                client.BaseAddress = new(Configuration["DownloadSetting:BaseAddress"]);
                client.DefaultRequestHeaders.UserAgent.Add(
                    new("SecondDimensionWatcher", "1.0"));
            });
            services.AddMemoryCache();

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionScopedJobFactory();
                q.AddJob<RssUpdateJob>(o => { o.WithIdentity("rss"); });
                q.AddJob<FetchTorrentInfoJob>(o => { o.WithIdentity("fetch"); });
                q.AddTrigger(o =>
                {
                    o.ForJob("rss")
                        .WithCronSchedule("0 0/10 * 1/1 * ? *");
                });
                q.AddTrigger(o => { o.ForJob("fetch").StartNow(); });
            });


            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDataContext dataContext)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            dataContext.Database.Migrate();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecondDimensionWatcher v1"));

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}