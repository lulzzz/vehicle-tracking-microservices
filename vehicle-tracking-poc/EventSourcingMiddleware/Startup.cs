﻿using BackgroundMiddleware.Concrete;
using DomainModels.DataStructure;
using DomainModels.System;
using DomainModels.Types.Messages;
using DomainModels.Vehicle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EventSourcingMiddleware
{
    internal class Startup
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
                {nameof(SystemLocalConfiguration.MessageSubscriberRoute),  Configuration.GetValue<string>(Identifiers.MessageSubscriberRoute)},
                {nameof(SystemLocalConfiguration.MessagesMiddlewareUsername),  Configuration.GetValue<string>(Identifiers.MessagesMiddlewareUsername)},
                {nameof(SystemLocalConfiguration.MessagesMiddlewarePassword),  Configuration.GetValue<string>(Identifiers.MessagesMiddlewarePassword)},

            });
        }

        private LocalConfiguration SystemLocalConfiguration;
        public IHostingEnvironment Environemnt { get; }
        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }

        // Inject background service, for receiving message
        public void ConfigureServices(IServiceCollection services)
        {
            var loggerFactorySrv = services.BuildServiceProvider().GetService<ILoggerFactory>();

            ///
            /// Injecting message receiver background service
            ///
            services.AddSingleton<IHostedService, RabbitMQSubscriber<(MessageHeader, DomainModels.Types.DomainModel<PingModel>, MessageFooter)>>(srv =>
            {
                return RabbitMQSubscriber<(MessageHeader header, DomainModels.Types.DomainModel<PingModel> body, MessageFooter footer)>.Create(loggerFactorySrv,
                   new RabbitMQConfiguration
                   {
                       hostName = SystemLocalConfiguration.MessagesMiddleware,
                       exchange = SystemLocalConfiguration.MiddlewareExchange,
                       userName = SystemLocalConfiguration.MessagesMiddlewareUsername,
                       password = SystemLocalConfiguration.MessagesMiddlewarePassword,
                       routes = new string[] { SystemLocalConfiguration.MessageSubscriberRoute }
                   }
                   , (pingMessageCallback) =>
                   {
                       try
                       {
                           var message = pingMessageCallback();
                           Logger.LogInformation($"[x] Event sourcing service receiving a message from exchange: {SystemLocalConfiguration.MiddlewareExchange}, route :{SystemLocalConfiguration.MessageSubscriberRoute}, message: {JsonConvert.SerializeObject(message)}");
                       }
                       catch (Exception ex)
                       {
                           Logger.LogCritical(ex, "de-serialize Object exceptions.");
                       }
                   });
           });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();

            app.Run(async (context) =>
            {
                context.Response.ContentType = "text/html";
                await context.Response
                    .WriteAsync("<p>Event sourcing middleware service, Hosted by Kestrel </p>");

                if (serverAddressesFeature != null)
                {
                    await context.Response
                        .WriteAsync("<p>Listening on the following addresses: " +
                            string.Join(", ", serverAddressesFeature.Addresses) +
                            "</p>");
                }

                await context.Response.WriteAsync($"<p>Request URL: {context.Request.GetDisplayUrl()} </p>");
            });
        }

    }

}
