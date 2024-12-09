﻿using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using FlowHttp;
using FlowHttp.Constants;
using SteamWishlistDiscountNotifier.Domain.ValueObjects;
using SteamCommon;
using SteamWishlistDiscountNotifier.Domain.Enums;
using SteamWishlistDiscountNotifier.Application.Steam.Wishlist;
using SteamWishlistDiscountNotifier.Presentation.Filters;

namespace SteamWishlistDiscountNotifier.Presentation
{
    internal class SteamWishlistViewerViewModel : ObservableObject, IDisposable
    {
        #region Fields
        private readonly IPlayniteAPI _playniteApi;
        private string _steamSessionId;
        private string _steamLoginSecure;

        private const string _steamStoreSubUrlMask = @"https://store.steampowered.com/app/{0}/";
        private const string _steamUriOpenUrlMask = @"steam://openurl/{0}";
        #endregion

        #region Properties
        private SteamAccountInfo _accountInfo;
        public SteamAccountInfo AccountInfo
        {
            get => _accountInfo;
            set => SetValue(ref _accountInfo, value);
        }

        public Dictionary<WishlistViewSorting, string> WishlistSortingTypes { get; }
        public Dictionary<ListSortDirection, string> WishlistSortingOrders { get; }
        public Dictionary<Ownership, string> OwnershipTypes { get; }

        private WishlistViewSorting _selectedSortingType = WishlistViewSorting.Rank;
        public WishlistViewSorting SelectedSortingType
        {
            get => _selectedSortingType;
            set
            {
                SetValue(ref _selectedSortingType, value);
                UpdateWishlistSorting();
            }
        }

        private ListSortDirection _selectedSortingDirection = ListSortDirection.Ascending;
        public ListSortDirection SelectedSortingDirection
        {
            get => _selectedSortingDirection;
            set
            {
                SetValue(ref _selectedSortingDirection, value);
                UpdateWishlistSorting();
            }
        }

        private Ownership _selectedOwnershipType = Ownership.Any;
        public Ownership SelectedOwnershipType
        {
            get => _selectedOwnershipType;
            set
            {
                SetValue(ref _selectedOwnershipType, value);
                RefreshWishlistView();
            }
        }

        private List<SteamWishlistViewItem> _wishlistItemsCollection;
        public List<SteamWishlistViewItem> WishlistItemsCollection
        {
            get => _wishlistItemsCollection;
            set => SetValue(ref _wishlistItemsCollection, value);
        }

        private ICollectionView _wishlistCollectionView;
        public ICollectionView WishlistCollectionView => _wishlistCollectionView;

        private string _searchString = string.Empty;
        public string SearchString
        {
            get => _searchString;
            set
            {
                SetValue(ref _searchString, value);
                RefreshWishlistView();
            }
        }

        private SteamWishlistViewItem _selectedWishlistItem;
        public SteamWishlistViewItem SelectedWishlistItem
        {
            get => _selectedWishlistItem;
            set => SetValue(ref _selectedWishlistItem, value);
        }

        private FilterGroup _tagFilters;
        public FilterGroup TagFilters
        {
            get => _tagFilters;
            set => SetValue(ref _tagFilters, value);
        }

        public Uri DefaultBannerUri { get; }

        // Filters
        private bool _filterOnlyDiscounted = true;
        public bool FilterOnlyDiscounted
        {
            get => _filterOnlyDiscounted;
            set
            {
                SetValue(ref _filterOnlyDiscounted, value);
                RefreshWishlistView();
            }
        }

        private int _filterMinimumDiscount = 0;
        public int FilterMinimumDiscount
        {
            get => _filterMinimumDiscount;
            set
            {
                SetValue(ref _filterMinimumDiscount, value);
                RefreshWishlistView();
            }
        }

        private double _filterMinimumPrice = 0;
        public double FilterMinimumPrice
        {
            get => _filterMinimumPrice;
            set
            {
                SetValue(ref _filterMinimumPrice, value);
                RefreshWishlistView();
            }
        }

        private double _filterMaximumPrice = 999999;
        public double FilterMaximumPrice
        {
            get => _filterMaximumPrice;
            set
            {
                SetValue(ref _filterMaximumPrice, value);
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeGame = true;
        public bool FilterItemTypeGame
        {
            get => _filterItemTypeGame;
            set
            {
                _filterItemTypeGame = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeDlc = true;
        public bool FilterItemTypeDlc
        {
            get => _filterItemTypeDlc;
            set
            {
                _filterItemTypeDlc = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeMusic = true;
        public bool FilterItemTypeMusic
        {
            get => _filterItemTypeMusic;
            set
            {
                _filterItemTypeMusic = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeApplication = true;
        public bool FilterItemTypeApplication
        {
            get => _filterItemTypeApplication;
            set
            {
                _filterItemTypeApplication = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeVideo = true;
        public bool FilterItemTypeVideo
        {
            get => _filterItemTypeVideo;
            set
            {
                _filterItemTypeVideo = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterItemTypeHardware = true;
        public bool FilterItemTypeHardware
        {
            get => _filterItemTypeHardware;
            set
            {
                _filterItemTypeHardware = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterIncludeReleased = true;
        public bool FilterIncludeReleased
        {
            get => _filterIncludeReleased;
            set
            {
                _filterIncludeReleased = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        private bool _filterIncludeNotReleased = true;
        public bool FilterIncludeNotReleased
        {
            get => _filterIncludeNotReleased;
            set
            {
                _filterIncludeNotReleased = value;
                OnPropertyChanged();
                RefreshWishlistView();
            }
        }

        public string FormattedBalance { get; }

        #endregion

        #region Commands
        public RelayCommand<SteamWishlistViewItem> RemoveItemFromWishlistCommand { get; }
        public RelayCommand<SteamWishlistViewItem> OpenWishlistItemOnSteamCommand { get; }
        public RelayCommand<SteamWishlistViewItem> OpenWishlistItemOnWebCommand { get; }
        #endregion

        #region Constructor

        public SteamWishlistViewerViewModel(
            IPlayniteAPI playniteApi,
            SteamWalletDetails walletDetails,
            List<CWishlistGetWishlistSortedFilteredResponseWishlistItem> wishlistResponseItems,
            Dictionary<uint, string> bannersPathsMapper,
            string pluginInstallPath)
        {
            _playniteApi = playniteApi;
            FormattedBalance = walletDetails.FormattedBalance;
            WishlistItemsCollection = CreateViewItems(wishlistResponseItems, bannersPathsMapper);
            DefaultBannerUri = new Uri(Path.Combine(pluginInstallPath, "Resources", "DefaultBanner.png"), UriKind.Absolute);

            RemoveItemFromWishlistCommand = new RelayCommand<SteamWishlistViewItem>((a) => RemoveWishlistItem(a));
            OpenWishlistItemOnSteamCommand = new RelayCommand<SteamWishlistViewItem>((a) => OpenWishlistItemInSteamClient(a));
            OpenWishlistItemOnWebCommand = new RelayCommand<SteamWishlistViewItem>((a) => OpenWishlistItemInBrowser(a));

            TagFilters = CreateTagsFilterGroup(WishlistItemsCollection);
            TagFilters.SettingsChanged += OnTagFiltersSettingsChanged;

            _wishlistCollectionView = InitializeWishlistCollectionView(WishlistItemsCollection);
            _wishlistCollectionView = CollectionViewSource.GetDefaultView(WishlistItemsCollection);
            _wishlistCollectionView.SortDescriptions.Add(new SortDescription(GetSortingPropertyName(), _selectedSortingDirection));

            UpdateSteamCookies();
            WishlistSortingTypes = new Dictionary<WishlistViewSorting, string>
            {
                [WishlistViewSorting.Name] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypeName"),
                [WishlistViewSorting.Rank] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypeRank"),
                [WishlistViewSorting.Discount] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypeDiscount"),
                [WishlistViewSorting.Price] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypePrice"),
                [WishlistViewSorting.ReleaseDate] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypeReleaseDate"),
                [WishlistViewSorting.Added] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingTypeAdded")
            };

            WishlistSortingOrders = new Dictionary<ListSortDirection, string>
            {
                [ListSortDirection.Ascending] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingDirectionAscending"),
                [ListSortDirection.Descending] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_WishlistViewSortingDirectionDescending"),
            };

            OwnershipTypes = new Dictionary<Ownership, string>
            {
                [Ownership.Any] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_GameOwnershipAny"),
                [Ownership.Owned] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_GameOwnershipOwned"),
                [Ownership.NotOwned] = ResourceProvider.GetString("LOCSteam_Wishlist_Notif_GameOwnershipNotOwned")
            };
        }

        private List<SteamWishlistViewItem> CreateViewItems(List<CWishlistGetWishlistSortedFilteredResponseWishlistItem> wishlistResponseItems, Dictionary<uint, string> bannersPathsMapper)
        {
            var otherSourcesOwnership = GetNonSteamOwnedItems();
            var items = new List<SteamWishlistViewItem>();
            foreach (var x in wishlistResponseItems)
            {
                if (x.StoreItem.BestPurchaseOption != null)
                {
                    var item = new SteamWishlistViewItem
                    (
                        x.StoreItem.Name,
                        x.Appid,
                        x.Priority,
                        DateTimeOffset.FromUnixTimeSeconds(x.DateAdded).DateTime.ToString("yyyy/M/d"),
                        x.StoreItem.Release?.SteamReleaseDate != null ? DateTimeOffset.FromUnixTimeSeconds(x.StoreItem.Release.SteamReleaseDate).DateTime.ToString("yyyy/M/d") : string.Empty,
                        x.StoreItem.IsEarlyAccess,
                        x.StoreItem.BestPurchaseOption.DiscountPct,
                        x.StoreItem.BestPurchaseOption.FormattedFinalPrice,
                        x.StoreItem.BestPurchaseOption.FormattedOriginalPrice,
                        x.StoreItem.BestPurchaseOption.FinalPriceInCents / 100,
                        x.StoreItem.BestPurchaseOption.OriginalPriceInCents / 100,
                        string.Join(", ", otherSourcesOwnership.ContainsKey(x.StoreItem.Name.Satinize()) ? otherSourcesOwnership[x.StoreItem.Name.Satinize()] : new List<string>()),
                        bannersPathsMapper.ContainsKey(x.Appid) ? bannersPathsMapper[x.Appid] : string.Empty,
                        (SteamStoreItemAppType)Enum.ToObject(typeof(SteamStoreItemAppType), x.StoreItem.Type),
                        x.StoreItem.Reviews?.SummaryFiltered.ReviewScoreLabel ?? string.Empty
                    );

                    items.Add(item);
                }
                else
                {
                    var item = new SteamWishlistViewItem
                    (
                        x.StoreItem.Name,
                        x.Appid,
                        x.Priority,
                        DateTimeOffset.FromUnixTimeSeconds(x.DateAdded).DateTime.ToString("yyyy/M/d"),
                        x.StoreItem.Release?.SteamReleaseDate != null ? DateTimeOffset.FromUnixTimeSeconds(x.StoreItem.Release.SteamReleaseDate).DateTime.ToString("yyyy/M/d") : string.Empty,
                        x.StoreItem.IsEarlyAccess,
                        0,
                        string.Empty,
                        string.Empty,
                        0,
                        0,
                        string.Join(", ", otherSourcesOwnership.ContainsKey(x.StoreItem.Name.Satinize()) ? otherSourcesOwnership[x.StoreItem.Name.Satinize()] : new List<string>()),
                        bannersPathsMapper.ContainsKey(x.Appid) ? bannersPathsMapper[x.Appid] : string.Empty,
                        (SteamStoreItemAppType)Enum.ToObject(typeof(SteamStoreItemAppType), x.StoreItem.ItemType),
                        x.StoreItem.Reviews?.SummaryFiltered.ReviewScoreLabel ?? string.Empty
                    );

                    items.Add(item);
                }
            }

            return items;
        }

        private Dictionary<string, List<string>> GetNonSteamOwnedItems()
        {
            var defaultSource = "Playnite";
            return _playniteApi.Database.Games
                .AsParallel()
                .Where(game => !Steam.IsGameSteamGame(game))
                .GroupBy(game => game.Name.Satinize())
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(game => game.Source?.Name.IsNullOrEmpty() == false ? game.Source.Name : defaultSource).ToList()
                );
        }

        #endregion

        #region Private Methods

        private FilterGroup CreateTagsFilterGroup(IEnumerable<SteamWishlistViewItem> wishlistItems)
        {
            var tags = new HashSet<string>();
            var tagsFiltersSource = new ObservableCollection<FilterItem>();
            //foreach (var item in wishlistItems)
            //{
            //    if (!item.Data.WishlistItem.Tags.HasItems())
            //    {
            //        continue;
            //    }

            //    foreach (var tag in item.Data.WishlistItem.Tags)
            //    {
            //        if (tags.Contains(tag))
            //        {
            //            continue;
            //        }

            //        tagsFiltersSource.Add(new FilterItem(false, tag));
            //        tags.Add(tag);
            //    }
            //}

            return new FilterGroup(tagsFiltersSource);
        }

        // Event Handlers
        private void OnTagFiltersSettingsChanged(object sender, EventArgs e)
        {
            _wishlistCollectionView.Refresh();
        }

        // Wishlist Management
        private void RefreshWishlistView()
        {
            _wishlistCollectionView?.Refresh();
        }

        private void UpdateWishlistSorting()
        {
            using (_wishlistCollectionView.DeferRefresh())
            {
                _wishlistCollectionView.SortDescriptions.Clear();
                _wishlistCollectionView.SortDescriptions.Add(new SortDescription(GetSortingPropertyName(), _selectedSortingDirection));
            }
        }

        private void RemoveWishlistItem(SteamWishlistViewItem cacheItem)
        {
            var success = false;
            _playniteApi.Dialogs.ActivateGlobalProgress(async (a) =>
            {
                // TODO Move to Steam Wishlist service
                var request = HttpRequestFactory.GetHttpRequest()
                    .WithUrl("https://store.steampowered.com/api/removefromwishlist")
                    .WithCookies(new Dictionary<string, string> { { "sessionid", _steamSessionId }, { "steamLoginSecure", _steamLoginSecure } })
                    .WithPostHttpMethod()
                    .WithContent($"sessionid={_steamSessionId}&appid={cacheItem.Appid}", HttpContentTypes.FormUrlEncoded, Encoding.UTF8);

                var result = await request.DownloadStringAsync(a.CancelToken);
                if (!result.IsSuccess || !Serialization.TryFromJson<WishlistAddRemoveRequestResponseDto>(result.Content, out var response) || !response.Success)
                {
                    _playniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSteam_Wishlist_Notif_RemoveFromWishlistFail"), "Steam Wishlist Discount Notifier");
                    return;
                }

                success = true;
            }, new GlobalProgressOptions(ResourceProvider.GetString("LOCSteam_Wishlist_Notif_RemovingFromWishlist")) { Cancelable = true });

            if (success)
            {
                WishlistItemsCollection.Remove(cacheItem);
                _wishlistCollectionView.Refresh();
            }
        }

        private void UpdateSteamCookies()
        {
            using (var webView = _playniteApi.WebViews.CreateOffscreenView())
            {
                webView.NavigateAndWait(@"https://store.steampowered.com/cart");

                _steamSessionId = GetCookieValue(webView, "store.steampowered.com", "sessionid");
                _steamLoginSecure = GetCookieValue(webView, "store.steampowered.com", "steamLoginSecure");
            }
        }

        private string GetCookieValue(IWebView webView, string domain, string cookieName)
        {
            var cookie = webView
                .GetCookies()
                .FirstOrDefault(x => x.Domain == domain && x.Name == cookieName);
            return cookie?.Value;
        }

        private void OpenWishlistItemInSteamClient(SteamWishlistViewItem wishlistItem)
        {
            var subIdSteamUrl = string.Format(_steamStoreSubUrlMask, wishlistItem.Appid);
            ProcessStarter.StartUrl(string.Format(_steamUriOpenUrlMask, subIdSteamUrl));
        }

        private void OpenWishlistItemInBrowser(SteamWishlistViewItem wishlistItem)
        {
            var subIdSteamUrl = string.Format(_steamStoreSubUrlMask, wishlistItem.Appid);
            ProcessStarter.StartUrl(subIdSteamUrl);
        }

        private ICollectionView InitializeWishlistCollectionView(IEnumerable<SteamWishlistViewItem> wishlistItems)
        {
            var collectionView = CollectionViewSource.GetDefaultView(wishlistItems);
            collectionView.Filter = FilterWishlistItems;
            return collectionView;
        }

        private bool FilterWishlistItems(object item)
        {
            var wishlistItem = item as SteamWishlistViewItem;
            if (_filterOnlyDiscounted)
            {
                if (wishlistItem.DiscountPct == 0 || wishlistItem.DiscountPct < FilterMinimumDiscount)
                {
                    return false;
                }
            }

            if (wishlistItem.FinalPrice != 0)
            {
                if (!_filterIncludeReleased)
                {
                    return false;
                }
            }
            else if (!_filterIncludeNotReleased)
            {
                return false;
            }

            if (FilterMinimumPrice != 0)
            {
                if (wishlistItem.FinalPrice <= FilterMinimumPrice)
                {
                    return false;
                }
            }

            if (FilterMaximumPrice != 999999)
            {
                if (wishlistItem.FinalPrice > FilterMaximumPrice)
                {
                    return false;
                }
            }

            if (_selectedOwnershipType == Ownership.Owned && wishlistItem.FormattedOwnedSources.IsNullOrEmpty())
            {
                return false;
            }
            else if (_selectedOwnershipType == Ownership.NotOwned && !wishlistItem.FormattedOwnedSources.IsNullOrEmpty())
            {
                return false;
            }

            if (!DoesTextMatchSearchString(wishlistItem.Name))
            {
                return false;
            }

            if (!IsWishlistItemTypeFilterEnabled(wishlistItem.SteamStoreItemType))
            {
                return false;
            }

            //if (TagFilters.EnabledFiltersNames.Any(x => !wishlistItem.WishlistItem.Tags.Contains(x)))
            //{
            //    return false;
            //}

            return true;
        }

        private bool DoesTextMatchSearchString(string toMatch)
        {
            if (_searchString.IsNullOrWhiteSpace())
            {
                return true;
            }

            if (!_searchString.IsNullOrWhiteSpace() && toMatch.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (_searchString.IsNullOrWhiteSpace() && toMatch.IsNullOrWhiteSpace())
            {
                return true;
            }

            if (_searchString.GetJaroWinklerSimilarityIgnoreCase(toMatch) >= 0.95)
            {
                return true;
            }

            if (!_searchString.MatchesAllWords(toMatch))
            {
                return false;
            }

            return true;
        }

        private bool IsWishlistItemTypeFilterEnabled(SteamStoreItemAppType itemType)
        {
            switch (itemType)
            {
                case SteamStoreItemAppType.Game:
                    return FilterItemTypeGame;
                case SteamStoreItemAppType.DLC:
                    return FilterItemTypeDlc;
                case SteamStoreItemAppType.Music:
                    return FilterItemTypeMusic;
                case SteamStoreItemAppType.Software:
                    return FilterItemTypeApplication;
                case SteamStoreItemAppType.Video:
                case SteamStoreItemAppType.Series:
                    return FilterItemTypeVideo;
                case SteamStoreItemAppType.Hardware:
                    return FilterItemTypeHardware;
                case SteamStoreItemAppType.Mod:
                case SteamStoreItemAppType.Demo:
                    return FilterItemTypeGame;
                default:
                    return true;
            }
        }

        private string GetSortingPropertyName()
        {
            switch (_selectedSortingType)
            {
                case WishlistViewSorting.Name:
                    return nameof(SteamWishlistViewItem.Name);
                case WishlistViewSorting.Rank:
                    return nameof(SteamWishlistViewItem.Priority);
                case WishlistViewSorting.Discount:
                    return nameof(SteamWishlistViewItem.DiscountPct);
                case WishlistViewSorting.Price:
                    return nameof(SteamWishlistViewItem.FinalPrice);
                case WishlistViewSorting.ReleaseDate:
                    return nameof(SteamWishlistViewItem.FormattedReleaseDate);
                case WishlistViewSorting.Added:
                    return nameof(SteamWishlistViewItem.FormattedDateAdded);
                default:
                    return nameof(SteamWishlistViewItem.Name);
            }
        }

        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (TagFilters != null)
            {
                TagFilters.SettingsChanged -= OnTagFiltersSettingsChanged;
                TagFilters.Dispose();
            }
        }
        #endregion
    }

}