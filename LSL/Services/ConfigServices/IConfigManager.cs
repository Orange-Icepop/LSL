using System.Threading.Tasks;
using LSL.Common;

namespace LSL.Services.ConfigServices;

public interface IConfigManager<TConfig>
{
    TConfig Config { get; }
    public Task<Result<TConfig>> LoadAsync();
    public Task<Result> SetAndWriteAsync(TConfig config);
}