﻿using SteamWishlistDiscountNotifier.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWishlistDiscountNotifier.Presentation
{
    public class SteamWishlistViewItem
    {
        public string Name { get; }
        public uint Appid { get; }
        public uint Priority { get; }
        public List<string> Tags { get; }
        public string FormattedDateAdded { get; }
        public string FormattedReleaseDate { get; }
        public bool IsEarlyAccess { get; }
        public int DiscountPct { get; }
        public string FormattedFinalPrice { get; }
        public string FormattedOriginalPrice { get; }
        public long FinalPrice { get; }
        public long OriginalPrice { get; }
        public string FormattedOwnedSources { get; }
        public string BannerImagePath { get; }
        public SteamStoreItemAppType SteamStoreItemType { get; }
        public string ReviewScoreLabel { get; }
        public DateTime? DiscountEndDate { get; }
        public string FormattedDiscountEndDate { get; }

        public bool IsDiscounted => DiscountPct > 0;
        public SteamWishlistViewItem(
            string name,
            uint appid,
            uint priority,
            List<string> tags,
            string formattedDateAdded,
            string formattedReleaseDate,
            bool isEarlyAccess,
            int discountPct,
            string formattedFinalPrice,
            string formattedOriginalPrice,
            long finalPrice,
            long originalPrice,
            string formattedOwnedSources,
            string bannerImagePath,
            SteamStoreItemAppType steamStoreItemType,
            string reviewScoreLabel,
            DateTime? discountEndDate,
            string formattedDiscountEndDate)
        {
            Name = name;
            Appid = appid;
            Priority = priority;
            Tags = tags;
            FormattedDateAdded = formattedDateAdded;
            FormattedReleaseDate = formattedReleaseDate;
            IsEarlyAccess = isEarlyAccess;
            DiscountPct = discountPct;
            FormattedFinalPrice = formattedFinalPrice;
            FormattedOriginalPrice = formattedOriginalPrice;
            FinalPrice = finalPrice;
            OriginalPrice = originalPrice;
            FormattedOwnedSources = formattedOwnedSources;
            BannerImagePath = bannerImagePath;
            SteamStoreItemType = steamStoreItemType;
            ReviewScoreLabel = reviewScoreLabel;
            DiscountEndDate = discountEndDate;
            FormattedDiscountEndDate = formattedDiscountEndDate;
        }
    }
}