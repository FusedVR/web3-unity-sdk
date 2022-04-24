# FusedVR Web3 Authentication

This SDK is a wrapper to around the [FusedVR Crypto APIs](https://crypto.fusedvr.com). This SDK enables Unity games to authenticate a user via their browser or mobile wallets. Simply call the login method on the Web3Manager static class and provide an email and appId (currently not required). This will trigger an email to be sent to the user with a magic link that will trigger either authneticating against their mobile wallet or desktop wallet. 

Please refer to the API documentation or comments in the Web3Manager class in Unity for information on the capabilities for authentication and reading the state of the user's wallet.

The goal of this service is enable game developers interested in building play 2 earn games a secure, robust authentication system so that they know their players are who they say they are and are not trying to cheat the system. 

## Unity Setup

Simply import this Github Repo as a Unity Package via the Unity Package Manager **Add from Git URL** : https://github.com/FusedVR/web3-unity-sdk.git

Once imported, you should be able to make async calls against the Web3Manager static class for 
- Login(email, appId)
- GetMagicLink(email, appId)
- GetAddress()
- GetNativeBalance(chain)
- GetERC20Tokens(chain)
- GetNFTTokens(chain)
