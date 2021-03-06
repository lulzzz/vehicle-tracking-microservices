﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildingAspects.Behaviors;
using DomainModels.System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace vehicleStatus
{
    /// <summary>
    /// Developed   By: Mohamed Abdo
    ///             On: 2017-12-23
    /// Vehicle service to handle receiving post request from vehicles (IoT) devices.
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Running ping vehicle service with Kestrel.");

            ILogger mainLogger = new LoggerFactory()
                                        .AddConsole()
                                        .AddDebug()
                                        .CreateLogger<Program>();
            try
            {
                new Function(mainLogger, Identifiers.RetryCount).Decorate(() =>
                 {
                     BuildWebHost(args)
                     .UseStartup<Startup>()
                     .Build()
                     .Run();
                     return Task.CompletedTask;
                 }).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                mainLogger.LogCritical($"Failed to start ping vehicle service, {ex.Message}.", ex);
                return -1;
            }
            finally
            {
                mainLogger = null;
            }
        }

        public static IWebHostBuilder BuildWebHost(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
}
