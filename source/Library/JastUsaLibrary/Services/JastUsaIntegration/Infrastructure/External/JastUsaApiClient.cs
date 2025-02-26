﻿using FlowHttp;
using FlowHttp.Constants;
using JastUsaLibrary.Services.JastUsaIntegration.Infrastructure.DTOs;
using JastUsaLibrary.JastUsaIntegration.Domain.Exceptions;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JastUsaLibrary.Services.JastUsaIntegration.Domain.ValueObjects;

namespace JastUsaLibrary.JastUsaIntegration.Infrastructure.External
{
    public class JastUsaApiClient
    {
        private readonly ILogger _logger;

        public JastUsaApiClient(ILogger logger)
        {
            _logger = logger;
        }

        public AuthenticationTokenResponse GetAuthenticationToken(
            string email,
            string password,
            bool rememberMe,
            CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["Accept-Encoding"] = "utf-8"
            };

            var authenticationRequest = new AuthenticationTokenRequest(email, password, rememberMe);
            var request = HttpRequestFactory.GetHttpRequest()
                .WithUrl(JastUrls.Api.Authentication.AuthenticationToken)
                .WithPostHttpMethod()
                .WithContent(Serialization.ToJson(authenticationRequest), HttpContentTypes.Json)
                .WithHeaders(headers);

            var downloadStringResult = request.DownloadString(cancellationToken);
            if (downloadStringResult.IsSuccess)
            {
                return Serialization.FromJson<AuthenticationTokenResponse>(downloadStringResult.Content);
            }

            if (downloadStringResult.IsCancelled)
            {
                return null;
            }

            _logger.Error(downloadStringResult.Error, "Failed to retrieve authentication token");
            if (downloadStringResult.HttpStatusCode is HttpStatusCode.Unauthorized)
            {
                throw new InvalidLoginCredentialsException(authenticationRequest.Email, authenticationRequest.Password);
            }

            throw new AuthenticationErrorException(authenticationRequest.Email, authenticationRequest.Password, downloadStringResult.HttpStatusCode);
        }

        public async Task<GameTranslationsResponse> GetGameTranslationsAsync(AuthenticationToken token, int translationId, CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token.Token.UrlDecode()}",
                ["Accept-Encoding"] = "utf-8"
            };

            var translationsUrl = string.Format(@"https://app.jastusa.com/api/v2/shop/account/game-translations/{0}", translationId);
            var request = HttpRequestFactory.GetHttpRequest()
                .WithUrl(translationsUrl)
                .WithHeaders(headers);

            var result = await request.DownloadStringAsync(cancellationToken);
            return result.IsSuccess ? Serialization.FromJson<GameTranslationsResponse>(result.Content) : null;
        }

        public async Task<List<Variant>> GetProductsAsync(AuthenticationToken token, CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token.Token.UrlDecode()}",
                ["Accept-Encoding"] = "utf-8"
            };

            var products = new List<Variant>();
            var page = 1;
            while (true)
            {
                var url = JastUrls.Api.Account.GetGames(page);
                var request = HttpRequestFactory.GetHttpRequest()
                    .WithUrl(url)
                    .WithHeaders(headers);

                var result = await request.DownloadStringAsync(cancellationToken);
                if (!result.IsSuccess)
                {
                    _logger.Info($"Stopped retrieving games in page {page - 1}");
                    break;
                }

                var response = Serialization.FromJson<GetGamesResponse>(result.Content);
                var variants = response.Products?.Select(x => x.Variant).ToList();
                products.AddRange(variants);
                if (!variants.Any() || page == response.Pages)
                {
                    break;
                }

                page++;
            }

            return products;
        }

        public async Task<Uri> GenerateDownloadLinkAsync(
            AuthenticationToken token,
            int gameId,
            int gameLinkId,
            CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token.Token.UrlDecode()}",
                ["Accept"] = "application/json",
                ["Accept-Encoding"] = "utf-8"
            };

            var requestBody = new GenerateLinkRequest(gameId, gameLinkId);
            var request = HttpRequestFactory.GetHttpRequest()
                .WithUrl(JastUrls.Api.Account.GenerateLink)
                .WithPostHttpMethod()
                .WithContent(Serialization.ToJson(requestBody), HttpContentTypes.Json)
                .WithHeaders(headers);

            var result = await request.DownloadStringAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.Error(result.Error, "Failed to generate download link");
                return null;
            }

            return Serialization.FromJson<GenerateLinkResponse>(result.Content).Url;
        }
    }
}
