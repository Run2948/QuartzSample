#  QuartzSample - [.NET 5.0 中配置Quartz集群](https://www.cnblogs.com/shapman/p/14218712.html)

Quartz定时任务框架使用示例，其内包含以下两种使用场景的示例
- 单机版本
- Quartz集群

#### 准备工作：

- 数据库一个，mysql、sqlserver等其他数据库均可
- 在上一篇文章中贴出来的单机版本的代码，没有看过的请转到： https://www.cnblogs.com/shapman/p/14218440.html
  或者也可以直接下载上一篇及当前这一篇文章的示例源码：https://github.com/book12138/QuartzSample

#### Quartz.net官方关于配置集群的文档：https://www.quartz-scheduler.net/documentation/quartz-2.x/tutorial/job-stores.html#ramjobstore

#### 本篇文章内容参考自：https://www.cnblogs.com/JulianHuang/p/12720436.html

#### 1、数据库中添加几张表

根据自己的数据库类型，自行进入这个地址复制SQL执行：https://github.com/quartznet/quartznet/tree/master/database/tables
![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231210623954-421155656.png)
以下内容均以 Mysql 为例

#### 2、修改startup中依赖注入的代码

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231202133355-1774004797.png)
改为：

```csharp
services.AddSingleton<ISchedulerFactory>(u => {
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
       ["quartz.dataSource.myDS.connectionString"] = "server=localhost;uid=root;pwd=123;database=quartzsample", // 配置数据库连接字符串，自己处理好连接字符串，我这里就直接这么写了
       ["quartz.dataSource.myDS.provider"] = "mysql-custom", // 配置数据库提供程序（这里是自定义的，定义的代码在上面）
       ["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz",
       ["quartz.serializer.type"] = "binary",
       ["quartz.jobStore.clustered"] = "true",    //  指示Quartz.net的JobStore是应对 集群模式
       ["quartz.scheduler.instanceId"] = "AUTO"
    };
    return new StdSchedulerFactory(properties);
});
```

#### 注意改一下数据库连接字符串

#### 3、修改 QuartzHostedService 类

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231202654857-1066266062.png)
修改 StartAsync 这个方法的内容，在 foreach 循环体内部添加一行 if 判断语句
原来的代码为：
![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231202850859-2147437153.png)
需要改成：

```
 /// <summary>
 /// 批量启动定时任务
 /// </summary>
 /// <param name="cancellationToken"></param>
 /// <returns></returns>
 public async Task StartAsync(CancellationToken cancellationToken)
 {
     _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
     _scheduler.JobFactory = _jobFactory;

     // 循环遍历startup里注册的作业
     foreach (var jobSchedule in _jobSchedules)
     {
         // 判断数据库中有没有记录过，有的话，quartz会自动从数据库中提取信息创建 schedule
         if (!await _scheduler.CheckExists(new JobKey(GenerateIdentity(jobSchedule, IdentityType.Job))) &&
          !await _scheduler.CheckExists(new TriggerKey(GenerateIdentity(jobSchedule, IdentityType.Trigger))))
         {
             var job = CreateJob(jobSchedule);
             var trigger = CreateTrigger(jobSchedule);

             await _scheduler.ScheduleJob(job, trigger, cancellationToken);
         }
     }

     await _scheduler.Start();
}
```

#### 3、目前集群已配置完成，进入项目根目录，开三个 cmd 用来在本地模拟集群

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231210915151-1963066717.png)
复制下面这条语句，粘贴到 cmd，然后每个 cmd 里面都修改成不同的端口号

```
dotnet run --urls=http://*:10086
```

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231211233705-329627240.png)

##### 首先先只运行两个程序，然后观察规律

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231212050297-784551158.png)

##### 仔细观察两个端口，你可以发现，同样的代码，但是定时任务并不会同时被两个进程去执行，他只会执行一个，那现在我把右边的给停下来

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231212332022-461862567.png)

##### 这个时候你就能发现，右边那个停止之后,左边的不仅会执行 job2 ，而且还会执行 job1

##### 那如果是三个又会怎么样呢

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231212814796-1405740439.png)

##### 现在我再把两边的给关掉，只留中间的那个

![img](https://img2020.cnblogs.com/blog/1709656/202012/1709656-20201231213147185-681895463.png)

##### Quartz借助数据库，使用悲观锁的方式，达到即使部署了多台机器，同一个定时任务也不会连续被几台服务机一起执行的结果。当一台机挂了，这时其他机器接过上台机的接力棒继续跑，从而做到高可用。

本篇文章代码地址：https://github.com/shapmanLv/QuartzSample