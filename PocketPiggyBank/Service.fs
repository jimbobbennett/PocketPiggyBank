namespace PocketPiggyBank.Services

open System
open Microsoft.WindowsAzure.MobileServices
open Xamarin.Essentials

type AzureService (authFunc) =

    let userIdKey = "userid"
    let authTokenKey = "authtoken"

    let client = new MobileServiceClient(Constants.functionUrl)

    let loadClient() =
        async {
            let! userId = SecureStorage.GetAsync userIdKey |> Async.AwaitTask
            let! authToken = SecureStorage.GetAsync authTokenKey |> Async.AwaitTask

            if not (String.IsNullOrWhiteSpace userId) && not (String.IsNullOrWhiteSpace authToken) then
                let user = new MobileServiceUser(userId)
                user.MobileServiceAuthenticationToken <- authToken
                client.CurrentUser <- user
        }

    let saveClient() =
        async {
            do! SecureStorage.SetAsync(userIdKey, client.CurrentUser.UserId) |> Async.AwaitTask
            do! SecureStorage.SetAsync(authTokenKey, client.CurrentUser.MobileServiceAuthenticationToken) |> Async.AwaitTask
        }

    let removeClient() =
        async {
            do! SecureStorage.SetAsync(userIdKey, null) |> Async.AwaitTask
            do! SecureStorage.SetAsync(authTokenKey, null) |> Async.AwaitTask
        }

    let auth() =
        async {
            let! isAuth = authFunc client
            if isAuth then do! saveClient()
        }

    member this.LoggedInUser() =
        async {
            do! loadClient()
            return client.CurrentUser |> Option.ofObj
        }

    member this.LogIn() =
        async {
            let! l = this.LoggedInUser()
            match l with
            | None -> do! auth()
            | Some _ -> ()
            return! this.LoggedInUser()
        }

    member this.LogOut() =
        async {
            do! removeClient()
            client.CurrentUser <- null
        }
