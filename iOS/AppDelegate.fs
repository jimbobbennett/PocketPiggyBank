namespace PocketPiggyBank.iOS

open System
open UIKit
open Foundation
open Xamarin.Forms
open Xamarin.Forms.Platform.iOS
open Microsoft.WindowsAzure.MobileServices

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit FormsApplicationDelegate ()

    let rec getTopmostViewController (vc : UIViewController) =
        let next = vc.PresentedViewController
        match next with
        | null -> vc
        | _ -> getTopmostViewController next

    let auth (client : MobileServiceClient) = 
        async {
            let window = UIApplication.SharedApplication.KeyWindow
            let vc = getTopmostViewController window.RootViewController
            let! user = client.LoginAsync(vc, MobileServiceAuthenticationProvider.Facebook, "PocketPiggeyBank") |> Async.AwaitTask
            return user <> null
        }

    override this.FinishedLaunching (app, options) =
        Forms.Init()
        this.LoadApplication (new PocketPiggyBank.App(auth))
        base.FinishedLaunching(app, options)

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main(args, null, "AppDelegate")
        0
