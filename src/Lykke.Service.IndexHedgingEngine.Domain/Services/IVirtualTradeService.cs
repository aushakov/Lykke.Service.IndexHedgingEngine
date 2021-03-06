using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.IndexHedgingEngine.Domain.Services
{
    public interface IVirtualTradeService
    {
        Task<IReadOnlyCollection<VirtualTrade>> GetAsync(DateTime startDate, DateTime endDate, string assetPairId,
            int limit);

        Task AddAsync(VirtualTrade virtualTrade);
    }
}
