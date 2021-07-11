using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using SingleQuartzHost.Crontab;
using SingleQuartzHost.Crontab.Jobs;

namespace SingleQuartzHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region quartz

            services.AddHostedService<QuartzHostedService>();

            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            services.AddTransient<MyJob1>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob1),
                cronExpression: "0/10 * * * * ?")); // 10s执行一次

            services.AddTransient<MyJob2>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob2),
                cronExpression: "0/15 * * * * ?")); // 15s执行一次

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {           
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Single Server Quartz Sample in NET 5.0.");
                });
            });
        }
    }
}
