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

    let mutable client : MobileServiceClient = null;
    let mutable vc : UIViewController = null;

    let rec getTopmostViewController (vc : UIViewController) =
        let next = vc.PresentedViewController
        match next with
        | null -> vc
        | _ -> getTopmostViewController next

    let auth (c : MobileServiceClient) = 
        client <- c
        async {
            try
                let! user = c.LoginAsync(vc, MobileServiceAuthenticationProvider.Facebook, "pocketpiggybank") |> Async.AwaitTask
                return user <> null
            with error -> System.Diagnostics.Debug.WriteLine error.Message
                          return false
        }

    override this.FinishedLaunching (app, options) =
        Forms.Init()
        this.LoadApplication (new PocketPiggyBank.App(auth))
        let ret = base.FinishedLaunching(app, options)
        vc <- getTopmostViewController UIApplication.SharedApplication.KeyWindow.RootViewController
        ret

    override this.OpenUrl(app, url, options) =
        client.ResumeWithURL url

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main(args, null, "AppDelegate")
        0
