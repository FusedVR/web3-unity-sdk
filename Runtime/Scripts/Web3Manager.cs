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

//TODO Add Test and Throw Errors
namespace FusedVR.Web3 {
    /// <summary>
    /// A wrapper class around https://crypto.fusedvr.com APIs for authenticating against the blockchain
    /// Via a magic link sent to a user's email address
    /// </summary>
    public class Web3Manager {

        #region Constants
        //generic response class
        public class Response<T> { public T response; } 

        //public url host for api endpoints
        public readonly static string host = "https://crypto-stage.fusedvr.com/api"; 

        //string enum class for chain ids to the api
        public class CHAIN {
            public static CHAIN eth = new CHAIN("eth");
            public static CHAIN ropsten = new CHAIN("ropsten");
            public static CHAIN rinkeby = new CHAIN("rinkeby");
            public static CHAIN goerli = new CHAIN("goerli");
            public static CHAIN polygon = new CHAIN("polygon");
            public static CHAIN mumbai = new CHAIN("mumbai");
            public static CHAIN bsc = new CHAIN("bsc");
            public static CHAIN bsctestnet = new CHAIN("bsc testnet");

            private CHAIN(string value) { Id = value; }

            public string Id { get; private set; }
        }

        //prefix for storing in player prefs
        private readonly static string BEARER_PREFIX_KEY = "crypto.fusedvr.bearer";
        #endregion

        #region Class Variables
        public string UUID { get; } //unique id for the user the web3manager is responsible for
        public string AppID { get; } //app id for the user stored from login

        private string registerCode; //private instance of code
        public string RegisterCode { get { return registerCode; } } //registration code used during authentication
        #endregion

        /// <summary>
        /// Constructor for the Web3 Manager
        /// </summary>
        private Web3Manager(string uuid, string appId) {
            UUID = uuid;
            AppID = appId;
        }

        #region Authentication
        /// <summary>
        /// Static call to the /fused/register with an appId and a unique id for the client and creates Web3Manager
        /// This unique id will be used to reference the player across sessions if the bearer token is still active
        /// If the unique id is an email, then an email be sent to the player to allow them to authenticate
        /// Tokens are saved between requests. If you would like to ignore this, please set ignoreCache = true
        /// </summary>
        public static async Task<Web3Manager> Register(string uuid, string appId, bool ignoreCache = false) {
            Web3Manager mngr = new Web3Manager(uuid, appId);
            if (!ignoreCache) {
                string token = mngr.GetBearerToken();
                if (token != null) {
                    return mngr;
                }
            }

            string code = await mngr.Register(); //will throw an error if something wrong with the APIs
            return mngr;
        }

        /// <summary>
        /// Calls the /fused/register with an appId and a unique id for given Web3Manager object
        /// This unique id will be used to reference the player across sessions if the bearer token is still active
        /// If the unique id is an email, then an email be sent to the player to allow them to authenticate
        /// </summary>
        public async Task<string> Register() {
            WWWForm form = new WWWForm();
            form.AddField("email", UUID);
            form.AddField("appId", AppID);
            string url = host + "/fused/register";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                string code = "";
                jsonMap.TryGetValue("code", out code); //TODO: throw error if missing

                registerCode = code;
                return RegisterCode;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }

        /// <summary>
        /// Called after Calls the /fused/login with an appId and email address
        /// Requires a long polling against the API to ensure the user has time to authenticate
        /// Timeout is approximately 6 minutes
        /// </summary>
        public async Task<bool> AwaitLogin() {
            if (GetBearerToken() != null) { //we are already logged in
                return true;
            }

            if (RegisterCode == null) {
                throw new Exception("No Registration Code avaliable. Call Register first.");
            }

            WWWForm form = new WWWForm();
            form.AddField("code", RegisterCode);
            form.AddField("appId", AppID);
            string url = host + "/fused/login";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                string bearerToken = "";
                jsonMap.TryGetValue("token", out bearerToken); //TODO: throw error if missing
               
                registerCode = null; //reset code after login
                PlayerPrefs.SetString(GetBearerKey(), bearerToken);
                return true;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }

        public void Logout() {
            PlayerPrefs.DeleteKey(GetBearerKey());
        }

        /// <summary>
        /// Requires that the Login Function has been called for the user and app
        /// Calls the /fused/getMagicLink endpoint to get the link that authenticates the user 
        /// This link should be displayed to the user in the application to enable them to authenticate the service
        /// </summary>
        public async Task<string> GetMagicLink() {
            if (GetBearerToken() != null && RegisterCode == null) { //we are already logged in
                throw new Exception("Already logged in succesfully. Re-register before calling Magic Link");
            } else if (RegisterCode == null) {
                throw new Exception("Token expired. Call Register first.");
            }

            WWWForm form = new WWWForm();
            form.AddField("code", RegisterCode);
            form.AddField("appId", AppID);
            string url = host + "/fused/getMagicLink";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                string value = ""; //get dictionary value
                jsonMap.TryGetValue("magicLink", out value); //TODO: throw error if missing
                return value;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }
        #endregion

        #region Authenticated Crypto APIs
        /// <summary>
        /// Requires that the user is authenticated
        /// Returns the public address that is located within the JWT token
        /// Please note you can also call the /account API with the token to get the public address
        /// </summary>
        public string GetAddress() {
            Dictionary<string, string> decode = DecodeJWT(GetBearerToken());
            return decode["address"];
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the currency balance for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public async Task<string> GetNativeBalance(CHAIN chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain.Id);
            string url = host + "/account/balance";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + GetBearerToken() );
            await webRequest.SendWebRequest();

            Dictionary<string, string> jsonMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                string value = ""; //get dictionary value
                jsonMap.TryGetValue("balance", out value); //TODO: throw error if missing
                return value;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the erc-20 token balances for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public async Task<List<Dictionary<string, string>>> GetERC20Tokens(CHAIN chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain.Id);
            string url = host + "/account/erc20";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + GetBearerToken() );
            await webRequest.SendWebRequest();

            List< Dictionary<string, string> > jsonMap = JsonConvert.DeserializeObject<
                List< Dictionary<string, string> > >
            (System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                return jsonMap;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }

        /// <summary>
        /// Requires that the user is authenticated
        /// Gets the nft balances for the user on the given chain.
        /// i.e. eth, polygon, bsc, ropsten, mumbai
        /// </summary>
        public async Task<List<Dictionary<string, string>>> GetNFTTokens(CHAIN chain) {
            WWWForm form = new WWWForm();
            form.AddField("chain", chain.Id);
            string url = host + "/account/nfts";
            UnityWebRequest webRequest = UnityWebRequest.Post(url, form);
            webRequest.SetRequestHeader("Authorization", "Bearer " + GetBearerToken() );
            await webRequest.SendWebRequest();

            List<Dictionary<string, string>> jsonMap = JsonConvert.DeserializeObject<
                List<Dictionary<string, string>>>
            (System.Text.Encoding.UTF8.GetString(webRequest.downloadHandler.data));
            webRequest.Dispose();
            if (jsonMap != null) {
                return jsonMap;
            } else {
                throw new Exception("Network Error : Response not JSON \n" + webRequest.downloadHandler.data);
            }
        }

        #endregion

        #region Bearer Token
        /// <summary>
        /// Get the Player Prefs Key to lookup the Bearer Token for FusedVR Chain Auth API Requests
        /// </summary>
        public string GetBearerKey() {
            return BEARER_PREFIX_KEY + "." + UUID + "." + AppID;
        }

        /// <summary>
        /// Get the Bearer Token for FusedVR Chain Auth API Requests
        /// </summary>
        public string GetBearerToken() {
            string token = PlayerPrefs.GetString(GetBearerKey());

            if (ValidToken(token)) { //only return a valid token
                return token;
            }

            return null;
        }

        /// <summary>
        /// Check if the token is valid by validating app Id and expiration
        /// </summary>
        private bool ValidToken(string token) {
            Dictionary<string, string> data = DecodeJWT(token);
            if (data != null) {
                long exp = long.Parse(data["exp"]);
                if (data["appId"] == AppID && DateTimeOffset.Now.ToUnixTimeSeconds() < exp) {
                    return true;
                }
            }

            return false;
        }

        // example implmentation for decoding a JWT bearer token
        public static Dictionary<string, string> DecodeJWT(string token) {
            var parts = token?.Split('.');
            if (parts.Length > 2) {
                var decode = parts[1];
                var padLength = 4 - decode.Length % 4;
                if (padLength < 4) {
                    decode += new string('=', padLength);
                }
                var bytes = Convert.FromBase64String(decode);
                string userInfo = System.Text.Encoding.ASCII.GetString(bytes);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(userInfo);
            }

            return null;
        }
    }
    #endregion

    #region Boiler Plate Classes for Web Requests
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
    #endregion
}

