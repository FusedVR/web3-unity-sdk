/**
 * Copyright 2022 Vasanth Mohan. All rights and licenses reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace FusedVR.Web3 {
    /// <summary>
    /// A Static wrapper Class around https://crypto.fusedvr.com APIs for authenticating against the blockchain
    /// Via a magic link sent to a user's email address
    /// </summary>
    public class Web3Manager {

        public class Response<T> { public T response; } //generic response

        public readonly static string host = "https://crypto.fusedvr.com/api"; //host for apis

        public readonly static string BEARER_TOKEN_KEY = "crypto.fusedvr.bearer.token";

        /// <summary>
        /// Calls the /fused/login with an appId and email address
        /// Requires a long polling against the API to ensure the user has time to authenticate
        /// Timeout is approximately 6 minutes
        /// </summary>
        public static async Task<bool> Login(string email, string appId) {
            WWWForm form = new WWWForm();
            form.AddField("email", email);
            form.AddField("appId", appId);
            string url = host + "/fused/login";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            if (jsonMap != null) {
                string bearerToken = "";
                jsonMap.TryGetValue("token", out bearerToken);
                PlayerPrefs.SetString(BEARER_TOKEN_KEY, bearerToken);
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Requires that the Login Function has been called for the user and app
        /// Calls the /fused/getMagicLink endpoint to get the link that authenticates the user 
        /// This link should be displayed to the user in the application to enable them to authenticate the service
        /// </summary>
        public static async Task<string> GetMagicLink(string email, string appId) {
            WWWForm form = new WWWForm();
            form.AddField("email", email);
            form.AddField("appId", appId);
            string url = host + "/fused/getMagicLink";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            if (jsonMap != null) {
                string value = ""; //get dictionary value
                jsonMap.TryGetValue("magicLink", out value);
                return value;
            } else {
                return "Login Function has NOT been called";
            }
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Calls the /fused/address endpoint to get the address that the user authenticated with 
        /// Please note that this is just an example for the end point. The address is also stored within the JWT token
        /// and can be decoded using the example method below
        /// </summary>
        public static async Task<string> GetAddress() {
            WWWForm form = new WWWForm();
            string url = host + "/account";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString(BEARER_TOKEN_KEY) );
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            if (jsonMap != null) {
                string value = ""; //get dictionary value
                jsonMap.TryGetValue("address", out value);
                return value;
            } else {
                return "Error Validating";
            }
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the currency balance for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public static async Task<string> GetNativeBalance(string chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain);
            string url = host + "/account/balance";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString(BEARER_TOKEN_KEY) );
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            if (jsonMap != null) {
                string value = ""; //get dictionary value
                jsonMap.TryGetValue("balance", out value);
                return value;
            } else {
                return "Error Validating";
            }
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the erc-20 token balances for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public static async Task<List<Dictionary<string, string>>> GetERC20Tokens(string chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain);
            string url = host + "/account/erc20";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString(BEARER_TOKEN_KEY) );
            await webRequest.SendWebRequest();

            List< Dictionary<string, string> > jsonMap = JsonConvert.DeserializeObject<
                List< Dictionary<string, string> > >
            (System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            return jsonMap;
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the nft balances for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public static async Task<List<Dictionary<string, string>>> GetNFTTokens(string chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain);
            string url = host + "/account/nfts";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString(BEARER_TOKEN_KEY) );
            await webRequest.SendWebRequest();

            List<Dictionary<string, string>> jsonMap = JsonConvert.DeserializeObject<
                List<Dictionary<string, string>>>
            (System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            return jsonMap;
        }

        // example implmentation for decoding a JWT bearer token
        static void DecodeJWT(string token) {
            var parts = token.Split('.');
            if (parts.Length > 2) {
                var decode = parts[1];
                var padLength = 4 - decode.Length % 4;
                if (padLength < 4) {
                    decode += new string('=', padLength);
                }
                var bytes = System.Convert.FromBase64String(decode);
                var userInfo = System.Text.ASCIIEncoding.ASCII.GetString(bytes);
                Debug.LogError(userInfo);
            }
        }
    }

    /// <summary>
    /// Class to enable asynchronous web requests via Unity 
    /// </summary>
    public class UnityWebRequestAwaiter : INotifyCompletion {
        private UnityWebRequestAsyncOperation asyncOp;
        private Action continuation;

        public UnityWebRequestAwaiter(UnityWebRequestAsyncOperation asyncOp) {
            this.asyncOp = asyncOp;
            asyncOp.completed += OnRequestCompleted;
        }

        public bool IsCompleted { get { return asyncOp.isDone; } }

        public void GetResult() { }

        public void OnCompleted(Action continuation) {
            this.continuation = continuation;
        }

        private void OnRequestCompleted(AsyncOperation obj) {
            continuation();
        }
    }

    public static class ExtensionMethods {
        public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp) {
            return new UnityWebRequestAwaiter(asyncOp);
        }
    }
}

