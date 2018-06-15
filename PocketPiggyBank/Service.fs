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
            | (null, _) | (_, null) -> ()
            | (_, _) -> let user = new MobileServiceUser(userId)
                        user.MobileServiceAuthenticationToken <- authToken
                        client.CurrentUser <- user
            ()
        }

    let auth() =
        async {
            let! isAuth = authFunc client
            match isAuth with
            | true -> do! SecureStorage.SetAsync(userIdKey, client.CurrentUser.UserId) |> Async.AwaitTask |> Async.Ignore
                      do! SecureStorage.SetAsync(authTokenKey, client.CurrentUser.MobileServiceAuthenticationToken) |> Async.AwaitTask |> Async.Ignore
                      return true
            | false -> return false
        }

    member this.IsLoggedIn() =
        client.CurrentUser <> null

    member this.LogIn() =
        loadClient() |> Async.RunSynchronously
        match this.IsLoggedIn() with
        | true -> true
        | false -> auth() |> Async.RunSynchronously
