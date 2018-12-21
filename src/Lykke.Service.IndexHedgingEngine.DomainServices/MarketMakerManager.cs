using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.IndexHedgingEngine.Domain;
using Lykke.Service.IndexHedgingEngine.Domain.Handlers;
using Lykke.Service.IndexHedgingEngine.Domain.Services;
using Lykke.Service.IndexHedgingEngine.DomainServices.Extensions;

namespace Lykke.Service.IndexHedgingEngine.DomainServices
{
    public class MarketMakerManager : IIndexHandler, IInternalTradeHandler
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IMarketMakerService _marketMakerService;
        private readonly IHedgeService _hedgeService;
        private readonly IInternalTradeService _internalTradeService;
        private readonly IIndexSettingsService _indexSettingsService;
        private readonly ITokenService _tokenService;
        private readonly IMarketMakerStateService _marketMakerStateService;
        private readonly ILog _log;

        public MarketMakerManager(
            IMarketMakerService marketMakerService,
            IHedgeService hedgeService,
            IInternalTradeService internalTradeService,
            IIndexSettingsService indexSettingsService,
            ITokenService tokenService,
            IMarketMakerStateService marketMakerStateService,
            ILogFactory logFactory)
        {
            _marketMakerService = marketMakerService;
            _hedgeService = hedgeService;
            _internalTradeService = internalTradeService;
            _indexSettingsService = indexSettingsService;
            _tokenService = tokenService;
            _marketMakerStateService = marketMakerStateService;
            _log = logFactory.CreateLog(this);
        }

        public async Task HandleIndexAsync(Index index)
        {
            MarketMakerState marketMakerState = await _marketMakerStateService.GetAsync();

            if (marketMakerState.Status != MarketMakerStatus.Active)
                return;
            
            if(!ValidateIndexWeightsValue(index))
                return;
            
            await _semaphore.WaitAsync();

            try
            {
                await _marketMakerService.UpdateOrderBookAsync(index);

                await _hedgeService.ExecuteAsync();
            }
            catch (Exception exception)
            {
                _log.ErrorWithDetails(exception, "An error occurred while processing index", index);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task HandleInternalTradesAsync(IReadOnlyCollection<InternalTrade> internalTrades)
        {
            await _semaphore.WaitAsync();

            try
            {
                IReadOnlyCollection<IndexSettings> indicesSettings = await _indexSettingsService.GetAllAsync();

                foreach (InternalTrade internalTrade in internalTrades)
                {
                    IndexSettings indexSettings = indicesSettings
                        .SingleOrDefault(o => o.AssetPairId == internalTrade.AssetPairId);

                    if (indexSettings != null)
                    {
                        await _internalTradeService.RegisterAsync(internalTrade);

                        await _tokenService.UpdateVolumeAsync(indexSettings.AssetId, internalTrade);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool ValidateIndexWeightsValue(Index index)
        {
            decimal totalWeight = index.Weights.Sum(o => o.Weight);

            if (.9m < totalWeight || totalWeight > 1.1m)
            {
                _log.WarningWithDetails("Wrong weight in the index", index);
                return false;
            }
            
            if(0 <= index.Value)
            {
                _log.WarningWithDetails("Wrong index value", index);
                return false;
            }

            return true;
        }
    }
}
