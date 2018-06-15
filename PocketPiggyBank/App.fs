namespace PocketPiggyBank

open Xamarin.Forms
open PocketPiggyBank.Services

type App(authFunc) =
    inherit Application()

    let azureService = new AzureService(authFunc)

    do
        base.MainPage <- new MainPage()
