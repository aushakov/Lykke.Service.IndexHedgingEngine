using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Service.IndexHedgingEngine.Domain;
using Lykke.Service.IndexHedgingEngine.Domain.Constants;
using Lykke.Service.IndexHedgingEngine.Domain.Services;
using Lykke.Service.IndexHedgingEngine.DomainServices.Extensions;
using Lykke.Service.IndexHedgingEngine.DomainServices.Utils;

namespace Lykke.Service.IndexHedgingEngine.DomainServices
{
    [UsedImplicitly]
    public class MarketMakerService : IMarketMakerService
    {
        private const string Exchange = ExchangeNames.Lykke;

        private readonly IIndexSettingsService _indexSettingsService;
        private readonly IIndexPriceService _indexPriceService;
        private readonly IBalanceService _balanceService;
        private readonly ISettingsService _settingsService;
        private readonly ILykkeExchangeService _lykkeExchangeService;
        private readonly ILimitOrderService _limitOrderService;
        private readonly IInstrumentService _instrumentService;
        private readonly TraceWriter _traceWriter;
        private readonly ILog _log;

        public MarketMakerService(
            IIndexSettingsService indexSettingsService,
            IIndexPriceService indexPriceService,
            IBalanceService balanceService,
            ISettingsService settingsService,
            ILykkeExchangeService lykkeExchangeService,
            ILimitOrderService limitOrderService,
            IInstrumentService instrumentService,
            TraceWriter traceWriter,
            ILogFactory logFactory)
        {
            _indexSettingsService = indexSettingsService;
            _indexPriceService = indexPriceService;
            _balanceService = balanceService;
            _settingsService = settingsService;
            _lykkeExchangeService = lykkeExchangeService;
            _limitOrderService = limitOrderService;
            _instrumentService = instrumentService;
            _traceWriter = traceWriter;
            _log = logFactory.CreateLog(this);
        }

        public async Task UpdateLimitOrdersAsync(string indexName)
        {
            IndexPrice indexPrice = await _indexPriceService.GetByIndexAsync(indexName);

            if (indexPrice == null)
                throw new InvalidOperationException("Index price not found");

            IndexSettings indexSettings = await _indexSettingsService.GetByIndexAsync(indexName);

            if (indexSettings == null)
                throw new InvalidOperationException("Index settings not found");

            AssetPairSettings assetPairSettings =
                await _instrumentService.GetAssetPairAsync(indexSettings.AssetPairId, Exchange);

            if (assetPairSettings == null)
                throw new InvalidOperationException("Asset pair settings not found");

            AssetSettings baseAssetSettings =
                await _instrumentService.GetAssetAsync(assetPairSettings.BaseAsset, ExchangeNames.Lykke);

            if (baseAssetSettings == null)
                throw new InvalidOperationException("Base asset settings not found");

            AssetSettings quoteAssetSettings =
                await _instrumentService.GetAssetAsync(assetPairSettings.QuoteAsset, ExchangeNames.Lykke);

            if (quoteAssetSettings == null)
                throw new InvalidOperationException("Quote asset settings not found");

            decimal sellPrice = (indexPrice.Price + indexSettings.SellMarkup)
                .TruncateDecimalPlaces(assetPairSettings.PriceAccuracy, true);

            decimal buyPrice = indexPrice.Price.TruncateDecimalPlaces(assetPairSettings.PriceAccuracy);

            IReadOnlyCollection<LimitOrder> limitOrders =
                CreateLimitOrders(indexSettings, assetPairSettings, sellPrice, buyPrice);

            ValidateBalance(limitOrders, baseAssetSettings, quoteAssetSettings);

            ValidateMinVolume(limitOrders, assetPairSettings.MinVolume);

            LimitOrder[] allowedLimitOrders = limitOrders
                .Where(o => o.Error == LimitOrderError.None)
                .ToArray();

            _log.InfoWithDetails("Limit orders created", limitOrders);

            _limitOrderService.Update(indexSettings.AssetPairId, limitOrders);

            await _lykkeExchangeService.ApplyAsync(indexSettings.AssetPairId, allowedLimitOrders);

            _traceWriter.LimitOrders(indexSettings.AssetPairId, limitOrders);
        }

        public async Task CancelLimitOrdersAsync(string indexName)
        {
            IndexSettings indexSettings = await _indexSettingsService.GetByIndexAsync(indexName);

            if (indexSettings == null)
                throw new InvalidOperationException("Index settings not found");

            await _lykkeExchangeService.CancelAsync(indexSettings.AssetPairId);

            _log.InfoWithDetails("Limit orders canceled", new {IndexName = indexName, indexSettings.AssetPairId});
        }

        private IReadOnlyCollection<LimitOrder> CreateLimitOrders(IndexSettings indexSettings,
            AssetPairSettings assetPairSettings, decimal sellPrice, decimal buyPrice)
        {
            var limitOrders = new List<LimitOrder>();

            string walletId = _settingsService.GetWalletId();

            decimal sellVolume = Math.Round(indexSettings.SellVolume / indexSettings.SellLimitOrdersCount,
                assetPairSettings.VolumeAccuracy);

            if (sellVolume >= assetPairSettings.MinVolume)
            {
                for (int i = 0; i < indexSettings.SellLimitOrdersCount; i++)
                    limitOrders.Add(LimitOrder.CreateSell(walletId, sellPrice, sellVolume));
            }
            else
            {
                limitOrders.Add(LimitOrder.CreateSell(walletId, sellPrice,
                    Math.Round(indexSettings.SellVolume, assetPairSettings.VolumeAccuracy)));
            }

            decimal buyVolume = Math.Round(indexSettings.BuyVolume / indexSettings.BuyLimitOrdersCount,
                assetPairSettings.VolumeAccuracy);

            if (buyVolume >= assetPairSettings.MinVolume)
            {
                for (int i = 0; i < indexSettings.BuyLimitOrdersCount; i++)
                    limitOrders.Add(LimitOrder.CreateBuy(walletId, buyPrice, buyVolume));
            }
            else
            {
                limitOrders.Add(LimitOrder.CreateBuy(walletId, buyPrice,
                    Math.Round(indexSettings.BuyVolume, assetPairSettings.VolumeAccuracy)));
            }

            return limitOrders;
        }

        private void ValidateBalance(IReadOnlyCollection<LimitOrder> limitOrders, AssetSettings baseAssetSettings,
            AssetSettings quoteAssetSettings)
        {
            List<LimitOrder> sellLimitOrders = limitOrders
                .Where(o => o.Error == LimitOrderError.None)
                .Where(o => o.Type == LimitOrderType.Sell)
                .OrderBy(o => o.Price)
                .ToList();

            List<LimitOrder> buyLimitOrders = limitOrders
                .Where(o => o.Error == LimitOrderError.None)
                .Where(o => o.Type == LimitOrderType.Buy)
                .OrderByDescending(o => o.Price)
                .ToList();

            if (sellLimitOrders.Any())
            {
                decimal balance = _balanceService.GetByAssetId(ExchangeNames.Lykke, baseAssetSettings.AssetId).Amount;

                foreach (LimitOrder limitOrder in sellLimitOrders)
                {
                    decimal amount = limitOrder.Volume.TruncateDecimalPlaces(baseAssetSettings.Accuracy, true);

                    if (balance - amount < 0)
                    {
                        decimal volume = balance.TruncateDecimalPlaces(baseAssetSettings.Accuracy);

                        limitOrder.UpdateVolume(volume);
                    }

                    balance = Math.Max(balance - amount, 0);
                }
            }

            if (buyLimitOrders.Any())
            {
                decimal balance = _balanceService.GetByAssetId(ExchangeNames.Lykke, quoteAssetSettings.AssetId).Amount;

                foreach (LimitOrder limitOrder in buyLimitOrders)
                {
                    decimal amount = (limitOrder.Price * limitOrder.Volume)
                        .TruncateDecimalPlaces(quoteAssetSettings.Accuracy, true);

                    if (balance - amount < 0)
                    {
                        decimal volume = (balance / limitOrder.Price).TruncateDecimalPlaces(baseAssetSettings.Accuracy);

                        limitOrder.UpdateVolume(volume);
                    }

                    balance = Math.Max(balance - amount, 0);
                }
            }
        }

        private static void ValidateMinVolume(IEnumerable<LimitOrder> limitOrders, decimal minVolume)
        {
            foreach (LimitOrder limitOrder in limitOrders.Where(o => o.Error == LimitOrderError.None))
            {
                if (limitOrder.Volume < minVolume || limitOrder.Volume <= 0)
                    limitOrder.Error = LimitOrderError.TooSmallVolume;
            }
        }
    }
}
