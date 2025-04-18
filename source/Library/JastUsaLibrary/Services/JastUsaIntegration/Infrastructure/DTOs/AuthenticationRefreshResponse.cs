﻿using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JastUsaLibrary.Services.JastUsaIntegration.Infrastructure.DTOs
{
    public class AuthenticationRefreshResponse
    {
        [SerializationPropertyName("token")]
        public string Token { get; set; }

        [SerializationPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}