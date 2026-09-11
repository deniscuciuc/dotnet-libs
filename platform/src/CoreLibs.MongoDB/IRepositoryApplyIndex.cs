using Microsoft.Extensions.Logging;

namespace CoreLibs.MongoDB;

public interface IRepositoryApplyIndex
{
    Task ApplyAsync(ILogger logger);
}
