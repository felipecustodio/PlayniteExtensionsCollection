﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DatabaseCommon;
using Playnite.SDK.Data;
using ReviewViewer.Domain;
using ReviewViewer.Infrastructure;

namespace ReviewViewer.Application
{
    public class SteamReviewsCoordinator
    {
        private readonly ISteamReviewsProvider _reviewProvider;
        private readonly LiteDbRepository<ReviewsResponseRecord> _repository;
        private TimeSpan _cacheDuration;

        public SteamReviewsCoordinator(
            ISteamReviewsProvider reviewProvider,
            LiteDbRepository<ReviewsResponseRecord> repository,
            TimeSpan cacheDuration)
        {
            _reviewProvider = reviewProvider;
            _repository = repository;
            _cacheDuration = cacheDuration;
        }

        public void ChangeCacheDuration(TimeSpan cacheDuration)
        {
            _cacheDuration = cacheDuration;
        }

        /// <summary>
        /// Get reviews for appId with given options.
        /// Will return cached data if fresh, otherwise fetches fresh data.
        /// </summary>
        public async Task<ReviewsResponseDto> GetReviewsAsync(
            int appId,
            QueryOptions options,
            bool forceRefresh,
            CancellationToken cancellationToken,
            string cursor = "*")
        {
            var cacheKey = CacheKeyBuilder.BuildCacheKey(appId, options, cursor);
            var hashedCacheKey = cacheKey.ToHashedKey();
            var existingCache = _repository.FirstOrDefault(x => x.CacheKey == hashedCacheKey);
            if (!forceRefresh && existingCache != null && (DateTime.UtcNow - existingCache.CreatedAt) < _cacheDuration)
            {
                return existingCache.Response;
            }

            var freshData = await _reviewProvider.GetReviewsAsync(appId, options, cancellationToken, cursor);
            if (freshData != null)
            {
                if (existingCache != null)
                {
                    _repository.Delete(existingCache.Id);
                }

                var newRecord = new ReviewsResponseRecord
                {
                    CacheKey = hashedCacheKey,
                    Response = freshData
                };

                _repository.Insert(newRecord);
                return newRecord.Response;
            }
            else if (existingCache != null)
            {
                // Fallback to stale cache
                return existingCache.Response;
            }

            return null;
        }
    }
}
