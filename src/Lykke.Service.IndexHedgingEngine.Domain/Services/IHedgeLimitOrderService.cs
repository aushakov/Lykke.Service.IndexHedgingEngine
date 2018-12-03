using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.IndexHedgingEngine.Domain.Services
{
    public interface IHedgeLimitOrderService
    {
        IReadOnlyCollection<HedgeLimitOrder> GetAll();

        Task<HedgeLimitOrder> GetByIdAsync(string hedgeLimitOrderId);

        Task UpdateAsync(IReadOnlyCollection<HedgeLimitOrder> hedgeLimitOrders);
    }
}