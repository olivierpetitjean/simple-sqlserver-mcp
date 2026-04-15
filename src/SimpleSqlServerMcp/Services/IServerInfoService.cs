using SimpleSqlServerMcp.Models;

namespace SimpleSqlServerMcp.Services;

internal interface IServerInfoService
{
    Task<ServerInfoResult> GetServerInfoAsync(CancellationToken cancellationToken);
}
