﻿using BackgroundMiddleware.Abstract;
using BackgroundMiddleware.Concrete;
using BuildingAspects.Services;
using DomainModels.DataStructure;
using DomainModels.System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using WebComponents.Interceptors;

namespace vehicleStatus
{
    public class Startup
    {
        public Startup(ILoggerFactory logger, IHostingEnvironment environemnt, IConfiguration configuration)
        {
            Logger = logger.CreateLogger<Startup>();
            Environemnt = environemnt;
            Configuration = configuration;
            //local system configuration
            SystemLocalConfiguration = LocalConfiguration.Create(new Dictionary<string, string>() {
                {nameof(SystemLocalConfiguration.CacheServer), Configuration.GetValue<string>(Identifiers.CacheServer)},
                {nameof(SystemLocalConfiguration.CacheDBVehicles),  Configuration.GetValue<string>(Identifiers.CacheDBVehicles)},
                {nameof(SystemLocalConfiguration.MessagesMiddleware),  Configuration.GetValue<string>(Identifiers.MessagesMiddleware)},
                {nameof(SystemLocalConfiguration.MiddlewareExchange),  Configuration.GetValue<string>(Identifiers.MiddlewareExchange)},
                {nameof(SystemLocalConfiguration.MessagePublisherRoute),  Configuration.GetValue<string>(Identifiers.MessagePublisherRoute)},
                {nameof(SystemLocalConfiguration.MessagesMiddlewareUsername),  Configuration.GetValue<string>(Identifiers.MessagesMiddlewareUsername)},
                {nameof(SystemLocalConfiguration.MessagesMiddlewarePassword),  Configuration.GetValue<string>(Identifiers.MessagesMiddlewarePassword)},
            });
        }

        private LocalConfiguration SystemLocalConfiguration;
        public IHostingEnvironment Environemnt { get; }
        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }
        private string AssemblyName => $"{Environemnt.ApplicationName} V{this.GetType().Assembly.GetName().Version}";

        // Inject background service, for receiving message
        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactorySrv = services.BuildServiceProvider().GetService<ILoggerFactory>();

            ILogger _logger = loggerFactorySrv
                .AddConsole()
                .AddDebug()
                .CreateLogger<Startup>();

            var _operationalUnit = new OperationalUnit(
                environment: Environemnt.EnvironmentName,
                assembly: AssemblyName);

            services.AddSingleton<IOperationalUnit>(srv => _operationalUnit);

            services.AddSingleton<LocalConfiguration, LocalConfiguration>(srv => SystemLocalConfiguration);
            services.AddSingleton<IMessagePublisher, RabbitMQPublisher>(srv =>
            {
                return RabbitMQPublisher.Create(loggerFactorySrv, new RabbitMQConfiguration
                {
                    hostName = SystemLocalConfiguration.MessagesMiddleware,
                    exchange = SystemLocalConfiguration.MiddlewareExchange,
                    userName = SystemLocalConfiguration.MessagesMiddlewareUsername,
                    password = SystemLocalConfiguration.MessagesMiddlewarePassword,
                    routes = new string[] { SystemLocalConfiguration.MessagePublisherRoute }
                });
            });

            ///
            /// Injecting message receiver background service
            ///

            services.AddDistributedRedisCache(redisOptions =>
            {
                redisOptions.Configuration = SystemLocalConfiguration.CacheServer;
                redisOptions.Configuration = SystemLocalConfiguration.CacheDBVehicles;
            });

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = _operationalUnit.Assembly, Version = "v1" });
            });

            services.AddMvc(options =>
            {
                //TODO: add practical policy instead of empty policy for authentication / authorization .
                options.Filters.Add(new CustomAuthorizer(_logger, _operationalUnit));
                options.Filters.Add(new CustomeExceptoinHandler(_logger, _operationalUnit, Environemnt));
                options.Filters.Add(new CustomResponseResult(_logger, _operationalUnit));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IDistributedCache cache, IHostingEnvironment environemnt)
        {
            app.UseStatusCodePages();
            if (environemnt.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            // Enable static files (if exists)
            app.UseStaticFiles();
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", AssemblyName);
            });
            app.UseMvc();
        }
    }
}
