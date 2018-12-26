using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.IndexHedgingEngine.Client.Models.AssetHedgeSettings
{
    /// <summary>
    /// Represents an asset hedge settings.
    /// </summary>
    [PublicAPI]
    public class AssetHedgeSettingsModel
    {
        /// <summary>
        /// The identifier of the asset.
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// The name of hedge exchange.
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// The identifier of the external asset pair.
        /// </summary>
        public string AssetPairId { get; set; }

        /// <summary>
        /// Returns <c>false</c> if asset hedge settings created automatically and indicates that the user should update default settings.
        /// </summary>
        public bool Approved { get; set; }

        /// <summary>
        /// The asset hedging mode.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetHedgeMode Mode { get; set; }
    }
}
