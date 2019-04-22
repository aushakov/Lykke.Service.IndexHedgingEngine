﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.IndexHedgingEngine.Client.Models.Settings;
using Refit;

namespace Lykke.Service.IndexHedgingEngine.Client.Api
{
    /// <summary>
    /// Provides methods for working with cross asset pairs.
    /// </summary>
    [PublicAPI]
    public interface ICrossAssetPairsApi
    {
        /// <summary>
        /// Returns all cross asset pairs.
        /// </summary>
        /// <returns>A collection of cross asset pairs.</returns>
        [Get("/api/CrossAssetPairs")]
        Task<IReadOnlyCollection<CrossAssetPairSettingsModel>> GetAllAsync();

        /// <summary>
        /// Adds new cross asset pair settings.
        /// </summary>
        /// <param name="model">The model that describes cross asset pair settings.</param>
        /// <param name="userId">The identifier of the user who added cross asset pair settings.</param>
        [Post("/api/CrossAssetPairs")]
        Task<Guid> AddAsync([Body] CrossAssetPairSettingsModel model, string userId);

        /// <summary>
        /// Updates existing cross asset pair settings.
        /// </summary>
        /// <param name="model">The model that describes cross asset pair settings.</param>
        /// <param name="userId">The identifier of the user who updated cross asset pair settings.</param>
        [Put("/api/CrossAssetPairs")]
        Task UpdateAsync([Body] CrossAssetPairSettingsModel model, string userId);

        /// <summary>
        /// Deletes cross asset pair settings.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The identifier of the user who deleted cross asset pair settings.</param>
        [Delete("/api/CrossAssetPairs")]
        Task DeleteAsync(Guid id, string userId);
    }
}
