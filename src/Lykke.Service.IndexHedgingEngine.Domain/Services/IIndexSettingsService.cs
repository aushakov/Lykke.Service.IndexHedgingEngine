using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.IndexHedgingEngine.Domain.Services
{
    public interface IIndexSettingsService
    {
        Task<IReadOnlyCollection<IndexSettings>> GetAllAsync();

        Task<IndexSettings> GetByIndexAsync(string indexName);

        Task AddAsync(IndexSettings indexSettings);

        Task UpdateAsync(IndexSettings indexSettings);

        Task DeleteAsync(string indexName);
    }
}
