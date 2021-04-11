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
            provider.Mappings.Add(".mkv", "	video/webm");
            services.AddSingleton(provider);
            services.AddHttpClient<TorrentController>(client =>
            {
                client.BaseAddress = new(Configuration["DownloadSetting:BaseAddress"]);
                client.DefaultRequestHeaders.UserAgent.Add(
                    new("SecondDimensionWatcher", "1.0"));
            });

            services.AddQuartz(q =>
            {
                var jobKey = new JobKey("rss");
                q.UseMicrosoftDependencyInjectionScopedJobFactory();
                q.AddJob<RssUpdateJob>(o => { o.WithIdentity(jobKey); });
                q.AddTrigger(o => { o.ForJob(jobKey).WithCronSchedule("0 0/10 * 1/1 * ? *"); });
            });


            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            services.AddHttpClient<FeedController>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.Add(
                    new("SecondDimensionWatcher", "1.0"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDataContext dataContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            dataContext.Database.Migrate();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecondDimensionWatcher v1"));

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
                    context.Context.Response.Headers.Add("Expires", "-1");
                }
            });

            app.UseWebSockets();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}