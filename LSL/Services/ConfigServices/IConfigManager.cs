using System.Threading.Tasks;
using LSL.Common;

namespace LSL.Services.ConfigServices;

public interface IConfigManager<TConfig>
{
    public TConfig CloneConfig();
    public Task<Result<TConfig>> LoadAsync();
    public Task<Result> SetAndWriteAsync(TConfig config);
}