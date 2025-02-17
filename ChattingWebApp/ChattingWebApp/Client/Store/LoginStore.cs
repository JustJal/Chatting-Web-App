﻿using ChattingWebApp.Shared.Models;
using Fluxor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace ChattingWebApp.Client.Store
{
    public record LoginState
    {
        public bool Authenticating { get; init; }
        public bool IsAuthenticate { get; init; }
        public AuthenticationRequest AuthRequest { get; init; }
        public string Message { get; init; }
    }

    public class LoginFeature : Feature<LoginState>
    {
        public override string GetName() => nameof(LoginState);

        protected override LoginState GetInitialState()
        {
            return new LoginState()
            {
                Authenticating = false,
                IsAuthenticate = false,
                AuthRequest = new(),
                Message = string.Empty

            };
        }
    }

    public class LoginValidateJwtAction
    {
        public AuthenticationRequest AuthenticationRequest { get; }
        public LoginValidateJwtAction(AuthenticationRequest authenticationRequest)
        {
            AuthenticationRequest = authenticationRequest;
        }
    }

    public class LoginSetErrorMessageAction
    {
        public string message { get; }
        public LoginSetErrorMessageAction(string message)
        {
            this.message = message;
        }
    }
    public class LoginSetIsAuthenticateAction
    {
        public bool isAuthenticated { get; }

        public LoginSetIsAuthenticateAction(bool isAuthenticated)
        {
            this.isAuthenticated = isAuthenticated;
        }
    }
    public class LoginSetAuthenticatingAction{}

    public class LoginReducers
    {
        [ReducerMethod(typeof(LoginSetAuthenticatingAction))]
        public static LoginState OnAuthenticating(LoginState state)
        {
            return state with
            {
                Authenticating = true,
                IsAuthenticate = false
            };
        }
        [ReducerMethod]
        public static LoginState OnSetIsAuthenticate(LoginState state, LoginSetIsAuthenticateAction action)
        {
            return state with
            {
                IsAuthenticate = action.isAuthenticated
            };
        }
        [ReducerMethod]
        public static LoginState OnSetIsAuthenticate(LoginState state, LoginSetErrorMessageAction action)
        {
            return state with
            {
                Message = action.message,
                Authenticating = false
            };
        }
    } 

    public class LoginEffects
    {
        private readonly HttpClient httpClient;
        private readonly ILocalStorageService localStorage;
        private readonly NavigationManager navigationManager;

        public LoginEffects(HttpClient _httpClient, ILocalStorageService _localStorage, NavigationManager _navigationManager)
        {
            httpClient = _httpClient;
            localStorage = _localStorage;
            navigationManager = _navigationManager;
        }

        [EffectMethod]
        public async Task OnValidatingJwtToken(LoginValidateJwtAction action, IDispatcher dispatcher)
        {
            dispatcher.Dispatch(new LoginSetAuthenticatingAction());

            var httpResponse = await httpClient.PostAsJsonAsync<AuthenticationRequest>("user/authenticatejwt", action.AuthenticationRequest);
            var authResponse = await httpResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

            if (string.IsNullOrEmpty(authResponse.Token))
            {
                dispatcher.Dispatch(new LoginSetIsAuthenticateAction(false));
                dispatcher.Dispatch(new LoginSetErrorMessageAction("Username or Password is invalid"));
                return;
            }
            dispatcher.Dispatch(new LoginSetIsAuthenticateAction(true));
            await localStorage.SetItemAsync("jwt_token", authResponse.Token);
            await httpClient.PutAsJsonAsync($"user/loginstatus", action.AuthenticationRequest.Nickname);
            navigationManager.NavigateTo("/profile", true);
        }
    }
}
