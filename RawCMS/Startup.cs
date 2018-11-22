﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using RawCMS.Library.Core;
using Swashbuckle.AspNetCore.Swagger;
using System.Linq;
using System.Runtime.Loader;

namespace RawCMS
{
    public class Startup
    {
        //private readonly CorePlugin cp = new CorePlugin();

        //TODO: this forces module reload. Fix it to avoid this manual step.
        //private readonly AuthPlugin dd = new AuthPlugin();

        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;
        private AppEngine appEngine;
        

        public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            

            this.loggerFactory = loggerFactory;
            loggerFactory.AddConsole(LogLevel.Trace);
            logger = loggerFactory.CreateLogger(typeof(Startup));

            // loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddNLog();
            env.ConfigureNLog(".\\conf\\nlog.config");

            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }

            appEngine.Plugins.OrderBy(x => x.Priority).ToList().ForEach(x =>
            {
                x.Configure(app, appEngine);
            });

            app.UseMvc();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{collection?}/{id?}");
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            app.UseStaticFiles();

            app.UseWelcomePage();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            appEngine = new AppEngine(loggerFactory, Configuration);

            appEngine.Plugins.OrderBy(x => x.Priority).ToList().ForEach(x =>
            {
                x.Setup(Configuration);
            });

            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Web API", Version = "v1" });
                //x.IncludeXmlComments(AppContext.BaseDirectory + "YourProject.Api.xml");
                c.IgnoreObsoleteProperties();
                c.IgnoreObsoleteActions();
                c.DescribeAllEnumsAsStrings();
            });

            //Invoke appEngine

            appEngine.Plugins.OrderBy(x => x.Priority).ToList().ForEach(x =>
            {
                x.ConfigureServices(services);
            });

            appEngine.Init();
        }
    }
}