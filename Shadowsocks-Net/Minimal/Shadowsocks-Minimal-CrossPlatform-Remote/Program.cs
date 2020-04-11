using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Buffers;
using System.IO;
using Argument.Check;
using Shadowsocks;
using Shadowsocks.Remote;
using Shadowsocks.Infrastructure;
using Shadowsocks.Infrastructure.Sockets;
using Serilog;

namespace Shadowsocks_Minimal_Crossplatform_Remote
{
    class Program
    {
        private static RemoteServer _RemoteServer;

        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var logger = SetLog();
            var appConfig = new ConfigurationBuilder().AddJsonFile("app-config.json", optional: true, reloadOnChange: true).Build();
            var remoteConfig = appConfig.GetSection("RemoteServerConfig").Get<RemoteServerConfig>();
            _RemoteServer = new RemoteServer(remoteConfig, logger);


            _RemoteServer.Start();

            while(true)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    _RemoteServer.Stop();
                    break;
                }
                logger.LogWarning("pls input exit to kill program");
            }
        }

        private static Microsoft.Extensions.Logging.ILogger SetLog()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs" + Path.DirectorySeparatorChar + "log-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
                .CreateLogger();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            var logger = loggerFactory.CreateLogger("SSRemote");
            return logger;
        }
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.Logger.Error("CurrentDomain_ProcessExit");
        }
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;            
        }
    }
}
