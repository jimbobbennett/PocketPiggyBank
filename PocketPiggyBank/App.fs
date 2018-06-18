namespace PocketPiggyBank

open System.Globalization
open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open PocketPiggyBank.Services
open Xamarin.Forms

module App = 
    type Model =
        {
            Balance : decimal
            CurrencySymbol : string
            AzureService : AzureService
        }
        with 
            member this.IsLoggedIn() = 
                this.AzureService.IsLoggedIn()

    type Msg =
        | Spend of decimal
        | Add of decimal
        | NeedRefresh
        | Login

    let init azureService =
         fun () -> { Balance = 0.0m; CurrencySymbol = NumberFormatInfo.CurrentInfo.CurrencySymbol; AzureService = azureService }, Cmd.none

    let update msg model =
        match msg with
        | Spend x -> {model with Balance = model.Balance - x}, Cmd.none
        | Add x -> {model with Balance = model.Balance + x}, Cmd.none
        | NeedRefresh -> model, Cmd.none
        | Login -> model, Cmd.none

    let createMainPage model =
        Xaml.NavigationPage(
                    pages = [
                        Xaml.ContentPage(
                            title="Pocket Piggy Bank",
                            content=Xaml.StackLayout(padding=20.0,
                                horizontalOptions=LayoutOptions.Center,
                                verticalOptions=LayoutOptions.Center,
                                children = [
                                    Xaml.Label(text = sprintf "%s%.2f" model.CurrencySymbol model.Balance)
                                ])
                        ).BarBackgroundColor(Color.Orange)
                         .BarTextColor(Color.White)
                    ])

    let login model =
        async {
            do! model.AzureService.LogIn()
            dispatch Login
        }

    let createLoginPage model = 
        Xaml.ContentPage(
                content=Xaml.Grid(
                            padding = 20.0,
                            rowdefs = [
                                        box "*"
                                        box "auto"
                                        box "*"
                                     ],
                            children = [
                                        Xaml.Button(
                                                    text = "Log in with Facebook",
                                                    backgroundColor = Color.Orange,
                                                    textColor = Color.White,
                                                    fontSize = Device.GetNamedSize(NamedSize.Large, typeof<Button>),
                                                    command = (fun () -> login model |> Async.StartImmediate)
                                                   ).GridRow(1)
                                       ]
                            )
                )

    let view (model : Model) dispatch =
        match model.IsLoggedIn() |> Async.RunSynchronously with
        | true -> createMainPage model
        | false -> createLoginPage model

type App(authFunc) as app =
    inherit Application()

    let azureService = new AzureService(authFunc)

    let program = Program.mkProgram (azureService |> App.init) App.update App.view
    let runner = 
        program
        |> Program.withConsoleTrace
        |> Program.withDynamicView app
        |> Program.run
