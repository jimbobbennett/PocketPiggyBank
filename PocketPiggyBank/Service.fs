namespace PocketPiggyBank.Services

open System
open System.Collections.Generic
open System.Net.Http
open Microsoft.WindowsAzure.MobileServices
open Newtonsoft.Json.Linq
open Xamarin.Essentials

type OnlineBalance = { TotalBalance : float }

type IncomingTransaction = { Amount : float; Description : string }

type AzureService (authFunc) =

    let userIdKey = "userid"
    let authTokenKey = "authtoken"
    let balanceApi = "balance"

    let client = new MobileServiceClient(Constants.functionUrl)

    let loadClient() =
        async {
            let! userId = SecureStorage.GetAsync userIdKey |> Async.AwaitTask
            let! authToken = SecureStorage.GetAsync authTokenKey |> Async.AwaitTask

            if not (String.IsNullOrWhiteSpace userId) && not (String.IsNullOrWhiteSpace authToken) then
                let user = new MobileServiceUser(userId)
                user.MobileServiceAuthenticationToken <- authToken
                client.CurrentUser <- user
            ()
        }

    let saveClient() =
        async {
            do! SecureStorage.SetAsync(userIdKey, client.CurrentUser.UserId) |> Async.AwaitTask 
            do! SecureStorage.SetAsync(authTokenKey, client.CurrentUser.MobileServiceAuthenticationToken) |> Async.AwaitTask
        }

    let removeClient() =
        async {
            do! SecureStorage.SetAsync(userIdKey, "") |> Async.AwaitTask 
            do! SecureStorage.SetAsync(authTokenKey, "") |> Async.AwaitTask
        }

    let auth p =
        async {
            let! isAuthorized = authFunc client p
            if isAuthorized then 
                do! saveClient()
        }

    member this.IsLoggedIn() =
        async {
            do! loadClient()
            return client.CurrentUser <> null
        }

    member this.LogIn (p : MobileServiceAuthenticationProvider) =
        async {
            do! loadClient()
            let! l = this.IsLoggedIn()
            if (not l) then do! auth p
        }

     member this.LogOut() =
        async {
            do! removeClient()
            client.CurrentUser <- null
        }

    member this.GetLatestBalance() =
        async {
            let! balance = client.InvokeApiAsync<OnlineBalance>(balanceApi, 
                                                                HttpMethod.Get,
                                                                new Dictionary<string, string>()) |> Async.AwaitTask
            return balance.TotalBalance
        }

    member this.AdjustBalance amount =
        async {
            do! client.InvokeApiAsync(balanceApi, JToken.FromObject({Amount = amount; Description = ""})) |> Async.AwaitTask |> Async.Ignore
            return! this.GetLatestBalance()
        }