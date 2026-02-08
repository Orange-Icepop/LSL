using System.Threading.Tasks;
using LSL.Common;
using LSL.Common.Results;

namespace LSL.Services.ConfigServices;

public interface IConfigManager<TConfig>
{
    TConfig Config { get; }
    /// <summary>
    /// Load the config of TConfig asynchronously.
    /// </summary>
    /// <returns>The config. Returns a warning when correctable errors occured and was fixed. Returns an error if it's uncorrectable, such as an IOException.</returns>
    public Task<Result<TConfig>> LoadAsync();
    /// <summary>
    /// Validate, fix and write the modified TConfig asynchronously.
    /// </summary>
    /// <param name="config">The modified TConfig.</param>
    /// <returns>The fixed config. Returns a warning if illegal values are set. Returns an error when uncorrectable issues occured, such as an IOException.</returns>
    public Task<Result<TConfig>> SetAndWriteAsync(TConfig config);
}