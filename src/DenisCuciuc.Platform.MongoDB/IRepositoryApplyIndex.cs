using Microsoft.Extensions.Logging;

namespace DenisCuciuc.Platform.MongoDB;

public interface IRepositoryApplyIndex
{
    Task ApplyAsync(ILogger logger);
}
