using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleSqlServerMcp.Host;

namespace SimpleSqlServerMcp;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSqlServerMcp(builder.Configuration);

        using IHost host = builder.Build();
        await host.RunAsync().ConfigureAwait(false);
    }
}
