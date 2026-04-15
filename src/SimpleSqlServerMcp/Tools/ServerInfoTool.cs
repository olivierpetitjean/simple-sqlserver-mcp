using System.ComponentModel;
using ModelContextProtocol.Server;
using SimpleSqlServerMcp.Models;
using SimpleSqlServerMcp.Services;

namespace SimpleSqlServerMcp.Tools;

[McpServerToolType]
internal sealed class ServerInfoTool(IServerInfoService serverInfoService)
{
    private readonly IServerInfoService _serverInfoService = serverInfoService;

    [McpServerTool(Name = "server_info"), Description("Returns SQL Server connection context and MCP safety settings.")]
    public Task<ServerInfoResult> GetServerInfo(CancellationToken cancellationToken)
    {
        return _serverInfoService.GetServerInfoAsync(cancellationToken);
    }
}
