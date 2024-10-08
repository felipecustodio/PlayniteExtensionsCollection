﻿using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteUtilitiesCommon;
using PluginsCommon;
using SpecialKHelper.Core.Domain;
using SpecialKHelper.EasyAnticheat.Application;
using SpecialKHelper.EasyAnticheat.Persistence;
using SpecialKHelper.PluginSidebarItem.Application;
using SpecialKHelper.PluginSidebarItem.Presentation;
using SpecialKHelper.SpecialKHandler.Application;
using SpecialKHelper.SpecialKHandler.Domain.Exceptions;
using SpecialKHelper.SpecialKProfilesEditor.Application;
using SpecialKHelper.SpecialKProfilesEditor.Presentation.Views;
using SteamCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpecialKHelper
{
    public class SpecialKHelper : GenericPlugin
    {
        private static readonly ILogger _logger = LogManager.GetLogger();
        private readonly string _pluginInstallPath;
        private const string _globalModeDisableFeatureName = "[SK] Global Mode Disable";
        private const string _selectiveModeEnableFeatureName = "[SK] Selective Mode Enable";

        private readonly SidebarItemSwitcherViewModel _sidebarItemSwitcherViewModel;

        private readonly EasyAnticheatService _easyAnticheatHelper;
        private readonly SteamHelper _steamHelper;

        private SpecialKHelperSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("71349310-9ed8-4bf5-8bf2-e92cdb222748");

        public SpecialKHelper(IPlayniteAPI api) : base(api)
        {
            settings = new SpecialKHelperSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            _pluginInstallPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _sidebarItemSwitcherViewModel = new SidebarItemSwitcherViewModel(true, _pluginInstallPath);
            _easyAnticheatHelper = new EasyAnticheatService(new EasyAnticheatCache(GetPluginUserDataPath()));
            _steamHelper = new SteamHelper(GetPluginUserDataPath(), PlayniteApi);
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            if (settings.Settings.ShowSidebarItem)
            {
                yield return new SidebarItem
                {
                    Title = ResourceProvider.GetString("LOCSpecial_K_Helper_SidebarTooltip"),
                    Type = SiderbarItemType.Button,
                    Icon = new SidebarItemSwitcherView { DataContext = _sidebarItemSwitcherViewModel },
                    Activated = () => {
                        _sidebarItemSwitcherViewModel.SwitchAllowState();
                    }
                };
            }
        }

        private string GetSpecialKPath()
        {
            if (settings.Settings.CustomSpecialKPath.IsNullOrEmpty())
            {
                return null;
            }

            if (FileSystem.FileExists(settings.Settings.CustomSpecialKPath))
            {
                return Path.GetDirectoryName(settings.Settings.CustomSpecialKPath);
            }

            _logger.Warn($"Special K Registry Directory not found in {settings.Settings.CustomSpecialKPath}");
            PlayniteApi.Notifications.Add(new NotificationMessage(
                "sk_customExeNotFound",
                string.Format(ResourceProvider.GetString("LOCSpecial_K_Helper_NotifcationErrorMessageSkCustomExecutablePathNotFound"), settings.Settings.CustomSpecialKPath),
                NotificationType.Error,
                () => OpenSettingsView()
            ));

            return null;
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            var game = args.Game;
            var startServices = GetShouldStartService(game);

            if (_steamHelper.IsEnvinronmentVariableSet())
            {
                if (settings.Settings.SteamOverlayForBpm != SteamOverlay.BigPictureMode
                    || Steam.IsGameSteamGame(game)
                    || !SteamClient.GetIsSteamBpmRunning())
                {
                    _steamHelper.RemoveBigPictureModeEnvVariable();
                }
            }
            else if (settings.Settings.SteamOverlayForBpm == SteamOverlay.BigPictureMode && SteamClient.GetIsSteamBpmRunning())
            {
                _steamHelper.SetBigPictureModeEnvVariable();
            }

            var skifPath = GetSpecialKPath();
            if (!startServices)
            {
                StopAllSpecialKServices();
                return;
            }

            var startSuccess32 = false;
            var startSuccess64 = false;
            try
            {
                startSuccess32 = SpecialKServiceManager.Start32BitsService(skifPath);
                startSuccess64 = SpecialKServiceManager.Start64BitsService(skifPath);
            }
            catch (SpecialKFileNotFoundException e)
            {
                LogSkFileNotFound(e);
            }
            catch (SpecialKPathNotFoundException e)
            {
                LogSkPathNotFound(e);
            }

            if (!startSuccess32 || !startSuccess64)
            {
                _logger.Info("Execution stopped due to services not started");
                return;
            }

            SpecialKConfigurationManager.ValidateDefaultProfile(game, skifPath, settings, GetPluginUserDataPath(), PlayniteApi);
            SpecialKConfigurationManager.ValidateReshadeConfiguration(game, skifPath);
        }

        private void LogSkFileNotFound(SpecialKFileNotFoundException e)
        {
            _logger.Error(e, $"Special K file not found");
            PlayniteApi.Notifications.Add(new NotificationMessage(
                Guid.NewGuid().ToString(),
                string.Format(ResourceProvider.GetString("LOCSpecial_K_Helper_NotifcationErrorMessageSkFileNotFound"), e.Message),
                NotificationType.Error,
                () => ProcessStarter.StartUrl(@"https://github.com/darklinkpower/PlayniteExtensionsCollection/wiki/Special-K-Helper#file-not-found-notification-error")
            ));
        }

        private void LogSkPathNotFound(SpecialKPathNotFoundException e)
        {
            _logger.Error(e, "Special K Path registry key not found");
            PlayniteApi.Notifications.Add(new NotificationMessage(
                "sk_registryNotFound",
                ResourceProvider.GetString("LOCSpecial_K_Helper_NotifcationErrorMessageSkRegistryKeyNotFound"),
                NotificationType.Error,
                () => OpenSettingsView()
            ));
        }

        private bool GetShouldStartService(Game game)
        {
            if (!_sidebarItemSwitcherViewModel.AllowSkUse)
            {
                _logger.Info("Start of services is disabled by sidebar item");
                return false;
            }

            if (settings.Settings.StopIfEasyAntiCheat && _easyAnticheatHelper.IsGameEacEnabled(game))
            {
                _logger.Info($"Start of services disabled due to game {game.Name} using EasyAntiCheat");
                return false;
            }

            if (settings.Settings.OnlyExecutePcGames && !PlayniteUtilities.IsGamePcGame(game))
            {
                return false;
            }

            if (game.Features.HasItems())
            {
                if (settings.Settings.StopExecutionIfVac && game.Features.Any(x => x.Name == "Valve Anti-Cheat Enabled"))
                {
                    return false;
                }

                if (settings.Settings.SpecialKExecutionMode == SpecialKExecutionMode.Global)
                {
                    if (game.Features.Any(x => x.Name == _globalModeDisableFeatureName))
                    {
                        return false;
                    }
                }
                else if (settings.Settings.SpecialKExecutionMode == SpecialKExecutionMode.Selective)
                {
                    if (!game.Features.Any(x => x.Name == _selectiveModeEnableFeatureName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (_steamHelper.IsEnvinronmentVariableSet())
            {
                _steamHelper.RemoveBigPictureModeEnvVariable();
            }

            StopAllSpecialKServices();
        }

        private void StopAllSpecialKServices()
        {
            var skifPath = GetSpecialKPath();
            try
            {
                SpecialKServiceManager.Stop32BitsService(skifPath);
                SpecialKServiceManager.Stop64BitsService(skifPath);
            }
            catch (SpecialKFileNotFoundException e)
            {
                LogSkFileNotFound(e);
            }
            catch (SpecialKPathNotFoundException e)
            {
                LogSkPathNotFound(e);
            }
        }        

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        private void OpenEditorWindow(string searchTerm = null)
        {
            var skifPath = GetSpecialKPath();
            if (skifPath.IsNullOrEmpty())
            {
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    "sk_registryNotFound",
                    ResourceProvider.GetString("LOCSpecial_K_Helper_NotifcationErrorMessageSkRegistryKeyNotFound"),
                    NotificationType.Error
                ));
                return;
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false,
                ShowMaximizeButton = true
            });

            window.Height = 700;
            window.Width = 900;
            window.Title = ResourceProvider.GetString("LOCSpecial_K_Helper_WindowTitleSkProfileEditor");

            window.Content = new SpecialKProfileEditorView();
            window.DataContext = new SpecialKProfileEditorViewModel(PlayniteApi, skifPath, searchTerm);
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptionOpenEditor"),
                    MenuSection = "@Special K Helper",
                    Action = o => {
                        OpenEditorWindow();
                    }
                },
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var firstGame = args.Games.Last();
            var menuItems = new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = string.Format(ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptionOpenSteamControllerConfig"), firstGame.Name),
                    MenuSection = $"Special K Helper",
                    Icon = PlayniteUtilities.GetIcoFontGlyphResource('\uEA30'),
                    Action = o =>
                    {
                        _steamHelper.OpenGameSteamControllerConfig(firstGame);
                    }
                }
            };

            if (settings.Settings.SpecialKExecutionMode == SpecialKExecutionMode.Global)
            {
                menuItems.Add(new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptiongGlobalModeAddFeature"),
                    MenuSection = $"Special K Helper",
                    Icon = PlayniteUtilities.GetIcoFontGlyphResource('\uEFA9'),
                    Action = o =>
                    {
                        PlayniteUtilities.AddFeatureToGames(PlayniteApi, args.Games.Distinct(), _globalModeDisableFeatureName);
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSpecial_K_Helper_DoneMessage"));
                    }
                });
                menuItems.Add(new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptiongGlobalModeRemoveFeature"),
                    MenuSection = $"Special K Helper",
                    Icon = PlayniteUtilities.GetIcoFontGlyphResource('\uEED7'),
                    Action = o => {
                        PlayniteUtilities.RemoveFeatureFromGames(PlayniteApi, args.Games.Distinct(), _globalModeDisableFeatureName);
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSpecial_K_Helper_DoneMessage"));
                    }
                });
            }
            else if (settings.Settings.SpecialKExecutionMode == SpecialKExecutionMode.Selective)
            {
                menuItems.Add(new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptiongSelectiveModeAddFeature"),
                    MenuSection = $"Special K Helper",
                    Icon = PlayniteUtilities.GetIcoFontGlyphResource('\uEED7'),
                    Action = o =>
                    {
                        PlayniteUtilities.AddFeatureToGames(PlayniteApi, args.Games.Distinct(), _selectiveModeEnableFeatureName);
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSpecial_K_Helper_DoneMessage"));
                    }
                });
                menuItems.Add(new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCSpecial_K_Helper_MenuItemDescriptiongSelectiveModeRemoveFeature"),
                    MenuSection = $"Special K Helper",
                    Icon = PlayniteUtilities.GetIcoFontGlyphResource('\uEFA9'),
                    Action = o => {
                        PlayniteUtilities.RemoveFeatureFromGames(PlayniteApi, args.Games.Distinct(), _selectiveModeEnableFeatureName);
                        PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSpecial_K_Helper_DoneMessage"));
                    }
                });
            }

            return menuItems;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SpecialKHelperSettingsView();
        }
    }
}