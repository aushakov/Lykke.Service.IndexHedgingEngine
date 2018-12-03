using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.IndexHedgingEngine.Domain;
using Lykke.Service.IndexHedgingEngine.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.IndexHedgingEngine.DomainServices.Tests
{
    [TestClass]
    public class MarketMakerManagerTests
    {
        private readonly Mock<IMarketMakerService> _marketMakerServiceMock =
            new Mock<IMarketMakerService>();
        
        private readonly Mock<IHedgeService> _hedgeServiceMock =
            new Mock<IHedgeService>();
        
        private readonly Mock<IIndexService> _indexServiceMock =
            new Mock<IIndexService>();
        
        private readonly Mock<IInternalTradeService> _internalTradeServiceMock =
            new Mock<IInternalTradeService>();
        
        private readonly Mock<IIndexSettingsService> _indexSettingsServiceMock =
            new Mock<IIndexSettingsService>();
        
        private readonly Mock<ITokenService> _tokenServiceMock =
            new Mock<ITokenService>();
        
        private MarketMakerManager _marketMakerManager;

        private List<IndexSettings> _indexSettings = new List<IndexSettings>();        
        private List<InternalTrade> _internalTrades = new List<InternalTrade>();        
        
        [TestInitialize]
        public void TestInitialize()
        {
            _indexSettingsServiceMock.Setup(o => o.GetAllAsync())
                .Returns(() => Task.FromResult<IReadOnlyCollection<IndexSettings>>(_indexSettings));

            _internalTradeServiceMock.Setup(o => o.RegisterAsync(It.IsAny<InternalTrade>()))
                .Returns((InternalTrade trade) =>
                {
                    _internalTrades.Add(trade);
                    return Task.CompletedTask;
                });

            _tokenServiceMock.Setup(o => o.UpdateVolumeAsync(
                    It.IsAny<string>(), It.IsAny<TradeType>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
                .Returns(() => Task.CompletedTask);
            
            _marketMakerManager = new MarketMakerManager(
                _marketMakerServiceMock.Object,
                _hedgeServiceMock.Object,
                _indexServiceMock.Object,
                _internalTradeServiceMock.Object,
                _indexSettingsServiceMock.Object,
                _tokenServiceMock.Object);
        }

        /// <remarks>https://lykkex.atlassian.net/browse/LIQ-1028</remarks>
        [TestMethod]
        public async Task Do_Not_Recalculate_Hedge_Orders_After_Trades()
        {
            // arrange

            _indexSettings = new List<IndexSettings> { new IndexSettings
            {
                Name = "LCI",
                AssetId = "TLYCI",
                AssetPairId = "TLYCIUSD",
                BuyVolume = 1,
                SellVolume = 1
            }};

            _hedgeServiceMock
                .Setup(o => o.ExecuteAsync())
                .Returns(() => Task.CompletedTask);

            // act

            await _marketMakerManager.HandleInternalTradesAsync(new[]
            {
                new InternalTrade
                {
                    AssetPairId = "TLYCIUSD",
                }
            });

            // assert

            Assert.IsTrue(_internalTrades.Count > 0);
            
            _hedgeServiceMock.Verify(o => o.ExecuteAsync(), Times.Never);
        }
    }
}
