using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using System;

#if NETCOREAPP2_2
using Microsoft.AspNetCore;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace GraphQL.Samples.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting host");
#if NETCOREAPP2_2
                CreateWebHostBuilder(args).Build().Run();
#else
                CreateHostBuilder(args).Build().Run();
#endif
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

#if NETCOREAPP2_2
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder<Startup>(args)
                .UseSerilog();
        }
#else
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseSerilog()
                        .UseStartup<Startup>();
                });
        }
#endif
    }
}
