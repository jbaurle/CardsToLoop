using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace CardRunner;

// The connection to the UiMcpServer: a child process over stdio when the setting is a path,
// Streamable HTTP when it is an address. The tools come back as AIFunctions and go into
// the same list as the local ones; the model does not see the difference.
public sealed class UiServer : IAsyncDisposable
{
    private readonly McpClient _client;

    private UiServer(McpClient client)
    {
        _client = client;
    }

    public static async Task<UiServer> ConnectAsync(string serverSetting, string repoRoot)
    {
        IClientTransport transport;

        if (serverSetting.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            transport = new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(serverSetting) });
        }
        else
        {
            var exePath = Path.GetFullPath(serverSetting, repoRoot);

            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"UiMcpServer not found, build src/UiMcpServer first: {exePath}");
            }

            transport = new StdioClientTransport(new StdioClientTransportOptions { Name = "ui", Command = exePath, WorkingDirectory = repoRoot });
        }

        return new UiServer(await McpClient.CreateAsync(transport));
    }

    public async Task<IList<AIFunction>> ListToolsAsync()
    {
        var tools = await _client.ListToolsAsync();
        return tools.Cast<AIFunction>().ToList();
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}
