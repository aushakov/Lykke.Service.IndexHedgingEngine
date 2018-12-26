using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.IndexHedgingEngine.Domain.Services
{
    public interface IBalanceService
    {
        IReadOnlyCollection<Balance> GetByExchange(string exchange);

        Balance GetByAssetId(string exchange, string assetId);

        Task UpdateAsync();
    }
}
