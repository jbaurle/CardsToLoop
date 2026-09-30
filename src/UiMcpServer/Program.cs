using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UiMcpServer;

// UI Automation reports physical pixels only to a DPI-aware process; without this,
// screenshots and click points are wrong on any scaled display.
NativeMethods.SetProcessDPIAware();

var builder = Host.CreateApplicationBuilder(args);

// Stdout carries the JSON-RPC stream, so all logging goes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<UiTools>();

await builder.Build().RunAsync();
