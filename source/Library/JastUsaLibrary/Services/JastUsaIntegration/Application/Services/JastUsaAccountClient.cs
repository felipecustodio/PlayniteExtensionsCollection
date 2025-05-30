﻿using JastUsaLibrary.JastUsaIntegration.Application.Interfaces;
using JastUsaLibrary.JastUsaIntegration.Infrastructure.External;
using JastUsaLibrary.Services.JastUsaIntegration.Domain.Entities;
using JastUsaLibrary.Services.JastUsaIntegration.Domain.Enums;
using JastUsaLibrary.Services.JastUsaIntegration.Domain.ValueObjects;
using JastUsaLibrary.Services.JastUsaIntegration.Infrastructure.DTOs;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JastUsaLibrary.JastUsaIntegration.Application.Services
{
    public class JastUsaAccountClient : IAccountClient
    {
        private readonly ILogger _logger = LogManager.GetLogger();
        private readonly IPlayniteAPI _playniteApi;
        private readonly JastUsaApiClient _apiClient;
        private readonly IAuthenticationPersistence _authenticationPersistence;

        public JastUsaAccountClient(
            IPlayniteAPI playniteApi,
            JastUsaApiClient apiClient,
            IAuthenticationPersistence authenticationPersistence)
        {
            _playniteApi = playniteApi;
            _apiClient = apiClient;
            _authenticationPersistence = authenticationPersistence;
        }

        public bool GetIsUserLoggedIn()
        {
            return _authenticationPersistence.LoadAuthentication() != null;
        }

        public bool Login(string email, string password, bool rememberMe)
        {
            _authenticationPersistence.DeleteAuthentication();
            var authRequest = new AuthenticationCredentials(email, password, rememberMe);
            var token = _apiClient.GetAuthenticationToken(authRequest.Email, authRequest.Password, authRequest.RememberMe);
            if (token != null)
            {
                return _authenticationPersistence.SaveAuthentication(authRequest);
            }

            _playniteApi.Dialogs.ShowErrorMessage("Login failed. Please check your credentials.", "JAST USA Library");
            return false;
        }

        public AuthenticationToken GetAuthenticationToken(CancellationToken cancellationToken = default)
        {
            var authRequest = _authenticationPersistence.LoadAuthentication();
            if (authRequest is null)
            {
                return null;
            }

            var authResponse = _apiClient.GetAuthenticationToken(authRequest.Email, authRequest.Password, authRequest.RememberMe, cancellationToken);
            return new AuthenticationToken(authResponse.Token, authResponse.Customer, authResponse.RefreshToken);
        }

        public async Task<List<JastGameData>> GetGamesAsync(CancellationToken cancellationToken = default)
        {
            var token = GetAuthenticationToken(cancellationToken);
            var products = await _apiClient.GetProductsAsync(token, cancellationToken);

            var gamesData = new List<JastGameData>();
            foreach (var product in products)
            {
                var gameData = new JastGameData(
                    product.ProductName,
                    product.ProductCode,
                    product.GameId,
                    product.Game.ApiRoute,
                    product.Game.Translations.EnUs?.Id,
                    product.Game.Translations.Ja?.Id,
                    product.Game.Translations.ZhHans?.Id,
                    product.Game.Translations.ZhHant?.Id);

                gamesData.Add(gameData);
            }

            return gamesData;
        }

        public async Task<JastGameDownloads> GetGameTranslationsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var token = GetAuthenticationToken(cancellationToken);
            var response = await _apiClient.GetGameTranslationsAsync(token, id, cancellationToken);
            if (response is null)
            {
                return null;
            }

            // We remove all the assets that are not for Windows because Playnite only supports windows after all
            List<JastGameDownloadData> GetDownloads(List<GameLink> links, JastDownloadType jastDownloadType)
            {
                links.RemoveAll(item => !item.Platforms.Contains(JastPlatforms.Windows));
                return links.Select(x => ConvertToDownloadData(x, jastDownloadType)).ToList();
            }

            JastGameDownloadData ConvertToDownloadData(GameLink gameLink, JastDownloadType jastDownloadType)
            {
                var platforms = new List<JastPlatform>();
                if (gameLink.Platforms.Contains(JastPlatforms.Windows))
                {
                    platforms.Add(JastPlatform.Windows);
                }

                if (gameLink.Platforms.Contains(JastPlatforms.Linux))
                {
                    platforms.Add(JastPlatform.Linux);
                }

                if (gameLink.Platforms.Contains(JastPlatforms.Mac))
                {
                    platforms.Add(JastPlatform.Mac);
                }

                return new JastGameDownloadData(gameLink.GameId, gameLink.GameLinkId, gameLink.Label, platforms, gameLink.Version, jastDownloadType);
            }

            return new JastGameDownloads(
                id,
                GetDownloads(response.GamePathLinks, JastDownloadType.Game),
                GetDownloads(response.GamePatchLinks, JastDownloadType.Patch),
                GetDownloads(response.GameExtraLinks, JastDownloadType.Extra));
        }

        public async Task<Uri> GetAssetDownloadLinkAsync(JastGameDownloadData downloadData, CancellationToken cancellationToken = default)
        {
            var token = GetAuthenticationToken(cancellationToken);
            return await _apiClient.GenerateDownloadLinkAsync(token, downloadData.GameId, downloadData.GameLinkId, cancellationToken);
        }
    }
}