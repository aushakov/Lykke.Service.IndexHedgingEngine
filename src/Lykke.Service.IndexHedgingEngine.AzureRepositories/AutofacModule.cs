using Autofac;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.RabbitMq.Azure.Deduplicator;
using Lykke.RabbitMqBroker.Deduplication;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Balances;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Hedging;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Indices;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.HedgeLimitOrders;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Positions;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Settings;
using Lykke.Service.IndexHedgingEngine.AzureRepositories.Trades;
using Lykke.Service.IndexHedgingEngine.Domain.Repositories;
using Lykke.SettingsReader;

namespace Lykke.Service.IndexHedgingEngine.AzureRepositories
{
    [UsedImplicitly]
    public class AutofacModule : Module
    {
        private readonly IReloadingManager<string> _connectionString;
        private readonly IReloadingManager<string> _lykkeTradesDeduplicatorConnectionString;
        private readonly IReloadingManager<string> _lykkeHedgeTradesDeduplicatorConnectionString;

        public AutofacModule(
            IReloadingManager<string> connectionString,
            IReloadingManager<string> lykkeTradesDeduplicatorConnectionString,
            IReloadingManager<string> lykkeHedgeTradesDeduplicatorConnectionString)
        {
            _connectionString = connectionString;
            _lykkeTradesDeduplicatorConnectionString = lykkeTradesDeduplicatorConnectionString;
            _lykkeHedgeTradesDeduplicatorConnectionString = lykkeHedgeTradesDeduplicatorConnectionString;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(container => new BalanceOperationRepository(
                    AzureTableStorage<BalanceOperationEntity>.Create(_connectionString,
                        "BalanceOperations", container.Resolve<ILogFactory>())))
                .As<IBalanceOperationRepository>()
                .SingleInstance();

            builder.Register(container => new FundingRepository(
                    AzureTableStorage<FundingEntity>.Create(_connectionString,
                        "Funding", container.Resolve<ILogFactory>())))
                .As<IFundingRepository>()
                .SingleInstance();

            builder.Register(container => new TokenRepository(
                    AzureTableStorage<TokenEntity>.Create(_connectionString,
                        "Tokens", container.Resolve<ILogFactory>())))
                .As<ITokenRepository>()
                .SingleInstance();

            builder.Register(container => new AssetHedgeSettingsRepository(
                    AzureTableStorage<AssetHedgeSettingsEntity>.Create(_connectionString,
                        "AssetHedgeSettings", container.Resolve<ILogFactory>())))
                .As<IAssetHedgeSettingsRepository>()
                .SingleInstance();

            builder.Register(container => new IndexPriceRepository(
                    AzureTableStorage<IndexPriceEntity>.Create(_connectionString,
                        "IndexPrices", container.Resolve<ILogFactory>())))
                .As<IIndexPriceRepository>()
                .SingleInstance();

            builder.Register(container => new IndexSettingsRepository(
                    AzureTableStorage<IndexSettingsEntity>.Create(_connectionString,
                        "IndexSettings", container.Resolve<ILogFactory>())))
                .As<IIndexSettingsRepository>()
                .SingleInstance();

            builder.Register(container => new HedgeLimitOrderRepository(
                    AzureTableStorage<HedgeLimitOrderEntity>.Create(_connectionString,
                        "HedgeLimitOrders", container.Resolve<ILogFactory>())))
                .As<IHedgeLimitOrderRepository>()
                .SingleInstance();

            builder.Register(container => new PositionRepository(
                    AzureTableStorage<PositionEntity>.Create(_connectionString,
                        "Positions", container.Resolve<ILogFactory>())))
                .As<IPositionRepository>()
                .SingleInstance();

            builder.Register(container => new AssetLinkRepository(
                    AzureTableStorage<AssetLinkEntity>.Create(_connectionString,
                        "AssetLinks", container.Resolve<ILogFactory>())))
                .As<IAssetLinkRepository>()
                .SingleInstance();

            builder.Register(container => new HedgeSettingsRepository(
                    AzureTableStorage<HedgeSettingsEntity>.Create(_connectionString,
                        "Settings", container.Resolve<ILogFactory>())))
                .As<IHedgeSettingsRepository>()
                .SingleInstance();

            builder.Register(container => new MarketMakerStateRepository(
                    AzureTableStorage<MarketMakerStateEntity>.Create(_connectionString,
                        "Settings", container.Resolve<ILogFactory>())))
                .As<IMarketMakerStateRepository>()
                .SingleInstance();

            builder.Register(container => new TimersSettingsRepository(
                    AzureTableStorage<TimersSettingsEntity>.Create(_connectionString,
                        "Settings", container.Resolve<ILogFactory>())))
                .As<ITimersSettingsRepository>()
                .SingleInstance();

            builder.Register(container => new InternalTradeRepository(
                    AzureTableStorage<InternalTradeEntity>.Create(_connectionString,
                        "InternalTrades", container.Resolve<ILogFactory>()),
                    AzureTableStorage<AzureIndex>.Create(_connectionString,
                        "InternalTradesIndices", container.Resolve<ILogFactory>())))
                .As<IInternalTradeRepository>()
                .SingleInstance();

            builder.Register(container => new LykkeTradeRepository(
                    AzureTableStorage<InternalTradeEntity>.Create(_connectionString,
                        "LykkeTrades", container.Resolve<ILogFactory>()),
                    AzureTableStorage<AzureIndex>.Create(_connectionString,
                        "LykkeTradesIndices", container.Resolve<ILogFactory>())))
                .As<ILykkeTradeRepository>()
                .SingleInstance();

            builder.Register(container => new VirtualTradeRepository(
                    AzureTableStorage<VirtualTradeEntity>.Create(_connectionString,
                        "VirtualTrades", container.Resolve<ILogFactory>())))
                .As<IVirtualTradeRepository>()
                .SingleInstance();

            builder.Register(container => new AzureStorageDeduplicator(
                    AzureTableStorage<DuplicateEntity>.Create(_lykkeTradesDeduplicatorConnectionString,
                        "LykkeTradesDeduplicator", container.Resolve<ILogFactory>())))
                .Named<IDeduplicator>("LykkeTradesDeduplicator")
                .SingleInstance();

            builder.Register(container => new AzureStorageDeduplicator(
                    AzureTableStorage<DuplicateEntity>.Create(_lykkeHedgeTradesDeduplicatorConnectionString,
                        "LykkeHedgeTradesDeduplicator", container.Resolve<ILogFactory>())))
                .Named<IDeduplicator>("LykkeHedgeTradesDeduplicator")
                .SingleInstance();
        }
    }
}