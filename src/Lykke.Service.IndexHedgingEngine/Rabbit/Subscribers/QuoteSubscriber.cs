using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.IndexHedgingEngine.Domain;
using Lykke.Service.IndexHedgingEngine.Domain.Services;
using Lykke.Service.IndexHedgingEngine.Settings.ServiceSettings.Rabbit.Subscribers;

namespace Lykke.Service.IndexHedgingEngine.Rabbit.Subscribers
{
    public class QuoteSubscriber : IDisposable
    {
        private readonly SubscriberSettings _settings;
        private readonly IQuoteService _quoteService;
        private readonly IReadOnlyDictionary<string, string> _assetPairMapping;
        private readonly ILogFactory _logFactory;

        private RabbitMqSubscriber<TickPrice> _subscriber;

        public QuoteSubscriber(
            SubscriberSettings settings,
            IQuoteService quoteService,
            IReadOnlyDictionary<string, string> assetPairMapping,
            ILogFactory logFactory)
        {
            _settings = settings;
            _quoteService = quoteService;
            _assetPairMapping = assetPairMapping;
            _logFactory = logFactory;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_settings.ConnectionString, _settings.Exchange, _settings.Queue);

            settings.DeadLetterExchangeName = null;

            _subscriber = new RabbitMqSubscriber<TickPrice>(_logFactory, settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings, TimeSpan.FromSeconds(10)))
                .SetMessageDeserializer(new JsonMessageDeserializer<TickPrice>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        public void Stop()
        {
            _subscriber?.Stop();
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }

        private Task ProcessMessageAsync(TickPrice tickPrice)
        {
            // TODO: Remove this workaround
            string assetPair = _assetPairMapping
                .FirstOrDefault(o => o.Key.Equals(tickPrice.Asset, StringComparison.InvariantCultureIgnoreCase)).Value;
            
            return _quoteService.UpdateAsync(new Quote(assetPair ?? tickPrice.Asset, tickPrice.Timestamp, tickPrice.Ask,
                tickPrice.Bid, tickPrice.Source));
        }
    }
}
