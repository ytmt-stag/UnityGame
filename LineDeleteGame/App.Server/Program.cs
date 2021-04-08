using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core; // Kestrel is a cross-platform web server for ASP.NET Core
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;

namespace App.Server
{
    class Program
    {
        /// <summary>
        /// エントリポイント
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // カスタマイズしたHostBuilder生成
            IHostBuilder builder = createCustomHostBuilder(args);

            // BuilderからHostをBuildして実行開始
            IHost host = builder.Build();
            host.Run();
        }

        /// <summary>
        /// カスタマイズしたHostBuilder生成
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static IHostBuilder createCustomHostBuilder(string[] args)
        {
            // まずは既定の設定でBuilder生成 / https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1#default-builder-settings
            IHostBuilder defaultBuilder =
                Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration(
                        (hostingContext, config) =>
                        {
                            var env = hostingContext.HostingEnvironment;
                            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            // json(plane) -> json(各環境) -> 環境変数 -> コマンドライン引数 (右に行くほど優先順位上)
                            config.SetBasePath(Directory.GetCurrentDirectory())
                                  .AddJsonFile($"appsettings.json", optional: false)
                                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                                  .AddEnvironmentVariables()
                                  .AddCommandLine(args)
                                  .Build();
                        }
                );

            // WebServerとして稼働させるための設定
            IHostBuilder webDefaultBuilder = defaultBuilder.ConfigureWebHostDefaults(
                webBuilder =>
                {
                    IWebHostBuilder bld = webBuilder.UseKestrel(
                        options =>
                        {   // WORKAROUND: Accept HTTP/2 only to allow insecure HTTP/2 connections during development.
                            options.ConfigureEndpointDefaults(
                                endpointOptions =>
                                {
                                    endpointOptions.Protocols = HttpProtocols.Http2;
                                }
                            );
                            //options.Listen(System.Net.IPAddress.Parse("xxx.xxx.xxx.xxx"), 12345);
                        });
                    // 設定をStartupクラスに委譲
                    bld.UseStartup<Startup>();
                });
            return webDefaultBuilder;
        }
    }
}