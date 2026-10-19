using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UiMcpServer;

// UI Automation reports physical pixels only to a DPI-aware process; without this,
// screenshots and click points are wrong on any scaled display.
NativeMethods.SetProcessDPIAware();

// Two transports, same tools:
//   UiMcpServer.exe                   stdio, started as a child process (Claude Code, .mcp.json)
//   UiMcpServer.exe --http [url]      Streamable HTTP on POST <url>/mcp, stateless; default
//                                     http://localhost:5081. Bind to 0.0.0.0 only on a trusted
//                                     network: whoever reaches the port drives the desktop.
// Either way the process needs the interactive desktop session; UI Automation and SendInput
// do not work from a service or a container.
if (args.Contains("--http"))
{
    var url = args.SkipWhile(a => a != "--http").Skip(1).FirstOrDefault(a => a.StartsWith("http")) ?? "http://localhost:5081";

    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .WithTools<UiTools>();

    var app = builder.Build();
    app.MapMcp("/mcp");
    app.Run(url);
}
else
{
    var builder = Host.CreateApplicationBuilder(args);

    // Stdout carries the JSON-RPC stream, so all logging goes to stderr.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<UiTools>();

    await builder.Build().RunAsync();
}
