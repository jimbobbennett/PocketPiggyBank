namespace PocketPiggyBank.Services

open Microsoft.WindowsAzure.MobileServices;
open Xamarin.Essentials;

type AzureService (authFunc) =

    let userIdKey = "userid"
    let authTokenKey = "authtoken"

    let client = new MobileServiceClient(Constants.functionUrl)

    let loadClient() =
        async {
            let! userId = SecureStorage.GetAsync userIdKey |> Async.AwaitTask
            let! authToken = SecureStorage.GetAsync authTokenKey |> Async.AwaitTask

            match userId, authToken with
            | (null, _) | (_, null) | ("", _) | (_, "") -> ()
            | (_, _) -> let user = new MobileServiceUser(userId)
                        user.MobileServiceAuthenticationToken <- authToken
                        client.CurrentUser <- user
            ()
        }

    let saveClient() =
        async {
            do! SecureStorage.SetAsync(userIdKey, client.CurrentUser.UserId) |> Async.AwaitTask |> Async.Ignore
            do! SecureStorage.SetAsync(authTokenKey, client.CurrentUser.MobileServiceAuthenticationToken) |> Async.AwaitTask |> Async.Ignore
        }

    let auth() =
        async {
            let! isAuth = authFunc client
            if isAuth then do! saveClient()
        }

    member this.IsLoggedIn() =
        async {
            do! loadClient()
            return client.CurrentUser <> null
        }

    member this.LogIn() =
        async {
            do! loadClient()
            let! l = this.IsLoggedIn()
            if (not l) then do! auth()
        }
