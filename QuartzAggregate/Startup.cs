using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Spi;
using QuartzAggregate.Crontab;
using QuartzAggregate.Crontab.Jobs;
using System.Collections.Specialized;
using System.Data;

namespace QuartzAggregate
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
            services.AddSingleton<ISchedulerFactory>(u =>
            {
                // 根据自己的数据库类型，自行进入这个地址复制SQL执行：https://github.com/quartznet/quartznet/tree/master/database/tables
                DbProvider.RegisterDbMetadata("mysql-custom", new DbMetadata()
                {
                    AssemblyName = typeof(MySqlConnection).Assembly.GetName().Name,
                    ConnectionType = typeof(MySqlConnection),
                    CommandType = typeof(MySqlCommand),
                    ParameterType = typeof(MySqlParameter),
                    ParameterDbType = typeof(DbType),
                    ParameterDbTypePropertyName = "DbType",
                    ParameterNamePrefix = "@",
                    ExceptionType = typeof(MySqlException),
                    BindByName = true
                });
                var properties = new NameValueCollection
                {
                    ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz", // 配置Quartz以使用JobStoreTx
                    ["quartz.jobStore.useProperties"] = "true", // 配置AdoJobStore以将字符串用作JobDataMap值
                    ["quartz.jobStore.dataSource"] = "myDS", // 配置数据源名称
                    ["quartz.jobStore.tablePrefix"] = "QRTZ_", // quartz所使用的表，在当前数据库中的表前缀
                    ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.StdAdoDelegate, Quartz",  // 配置AdoJobStore使用的DriverDelegate
                    ["quartz.dataSource.myDS.connectionString"] = "Server=local.host;User Id=root;Password=123456;Database=QuartzNet;Charset=utf8;", // 配置数据库连接字符串
                    ["quartz.dataSource.myDS.provider"] = "mysql-custom", // 配置数据库提供程序（这里是自定义的，定义的代码在上面）
                    ["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz",
                    ["quartz.serializer.type"] = "binary",
                    ["quartz.jobStore.clustered"] = "true",    //  指示Quartz.net的JobStore是应对 集群模式
                    ["quartz.scheduler.instanceId"] = "AUTO"
                };
                return new StdSchedulerFactory(properties);
            });

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
                    await context.Response.WriteAsync("Group Server Quartz Sample in NET 5.0.");
                });
            });
        }
    }
}
