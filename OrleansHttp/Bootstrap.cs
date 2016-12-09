using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Owin;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace OrleansHttp
{
    public class Bootstrap : IBootstrapProvider
    {
        IDisposable host;
        Logger logger;
      
        public string Name { get; private set; }


        public Task Close()
        {
            if (null != host) host.Dispose();
            return TaskDone.Done;
        }


        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.logger = providerRuntime.GetLogger(name);
            this.Name = name;

            var taskScheduler = TaskScheduler.Current;
            var port = config.Properties.ContainsKey("Port") ? int.Parse(config.Properties["Port"]) : 8080;

            this.host = WebApp.Start($"http://localhost:{port}",  appBuilder => 
            {
                var httpConfig = new HttpConfiguration();

                // grain/ITestGrain/0/Test/
                httpConfig.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "grain/{grainTypeName}/{grainId}/{grainMethodName}",
                    defaults: new { controller = "Grain", action = "Post" }
                );

                var container = new UnityContainer();
                container.RegisterInstance(providerRuntime);
                container.RegisterInstance(taskScheduler);
                httpConfig.DependencyResolver = new UnityResolver(container);

                appBuilder.UseWebApi(httpConfig);
            });
            
            this.logger.Verbose($"HTTP API listening on {port}");

            return TaskDone.Done;
        }
    }

  
}
