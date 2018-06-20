namespace PocketPiggyBank.Droid
open System

open Android.App
open Android.Content
open Android.Content.PM
open Android.Runtime
open Android.Views
open Android.Widget
open Android.OS
open Xamarin.Forms.Platform.Android
open Microsoft.WindowsAzure.MobileServices;

type Resources = PocketPiggyBank.Droid.Resource

[<Activity (Label = "PocketPiggyBank.Droid", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = (ConfigChanges.ScreenSize ||| ConfigChanges.Orientation))>]
type MainActivity() as this =
    inherit FormsAppCompatActivity()

    let auth (client : MobileServiceClient) (p : MobileServiceAuthenticationProvider) = 
        async {
            let! user = client.LoginAsync(this, p, "pocketpiggybank") |> Async.AwaitTask
            return user <> null
        }

    override this.OnCreate (bundle: Bundle) =
        FormsAppCompatActivity.TabLayoutResource <- Resources.Layout.Tabbar
        FormsAppCompatActivity.ToolbarResource <- Resources.Layout.Toolbar

        base.OnCreate (bundle)

        Xamarin.Forms.Forms.Init (this, bundle)

        this.LoadApplication (new PocketPiggyBank.App(auth))
