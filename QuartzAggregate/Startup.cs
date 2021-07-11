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
                // �����Լ������ݿ����ͣ����н��������ַ����SQLִ�У�https://github.com/quartznet/quartznet/tree/master/database/tables
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
                    ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz", // ����Quartz��ʹ��JobStoreTx
                    ["quartz.jobStore.useProperties"] = "true", // ����AdoJobStore�Խ��ַ�������JobDataMapֵ
                    ["quartz.jobStore.dataSource"] = "myDS", // ��������Դ����
                    ["quartz.jobStore.tablePrefix"] = "QRTZ_", // quartz��ʹ�õı��ڵ�ǰ���ݿ��еı�ǰ׺
                    ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.StdAdoDelegate, Quartz",  // ����AdoJobStoreʹ�õ�DriverDelegate
                    ["quartz.dataSource.myDS.connectionString"] = "Server=local.host;User Id=root;Password=123456;Database=QuartzNet;Charset=utf8;", // �������ݿ������ַ���
                    ["quartz.dataSource.myDS.provider"] = "mysql-custom", // �������ݿ��ṩ�����������Զ���ģ�����Ĵ��������棩
                    ["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz",
                    ["quartz.serializer.type"] = "binary",
                    ["quartz.jobStore.clustered"] = "true",    //  ָʾQuartz.net��JobStore��Ӧ�� ��Ⱥģʽ
                    ["quartz.scheduler.instanceId"] = "AUTO"
                };
                return new StdSchedulerFactory(properties);
            });

            services.AddTransient<MyJob1>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob1),
                cronExpression: "0/10 * * * * ?")); // 10sִ��һ��

            services.AddTransient<MyJob2>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob2),
                cronExpression: "0/15 * * * * ?")); // 15sִ��һ��

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
