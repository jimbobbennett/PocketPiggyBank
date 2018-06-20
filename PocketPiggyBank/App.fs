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
            User: string option
        }

    type Msg =
        | Spend of decimal
        | Add of decimal
        | NeedRefresh
        | Login of string option

    let init azureService () =
        { Balance = 0.0m
          CurrencySymbol = NumberFormatInfo.CurrentInfo.CurrencySymbol
          AzureService = azureService
          User = None }, Cmd.none

    let update msg model =
        match msg with
        | Spend x -> {model with Balance = model.Balance - x}, Cmd.none
        | Add x -> {model with Balance = model.Balance + x}, Cmd.none
        | NeedRefresh -> model, Cmd.none
        | Login user -> { model with User = user }, Cmd.none

    let createMainPage model dispatch =
        Xaml.ContentPage(
            title="Pocket Piggy Bank",
            content=Xaml.StackLayout(padding=20.0,
                horizontalOptions=LayoutOptions.Center,
                verticalOptions=LayoutOptions.CenterAndExpand,
                children = [
                    match model.User with
                    | Some user ->
                        yield Xaml.Label(text = sprintf "Logged in as : %s" user)
                        yield Xaml.Label(text = sprintf "Balance: %s%.2f" model.CurrencySymbol model.Balance)
                        yield Xaml.Button(text = "Withdraw", command=(fun () -> dispatch (Spend 10.0m)), canExecute=(model.Balance > 0.0m))
                        yield Xaml.Button(text = "Deposit", command=(fun () -> dispatch (Add 10.0m)))
                        yield Xaml.Button(text = "Logout", command=(fun () -> dispatch (Login None)))
                    | None ->
                        yield Xaml.Label(text = sprintf "Not logged in")
                ])
        ).BarBackgroundColor(Color.Orange)
         .BarTextColor(Color.White)

    let login model dispatch =
        async {
            try
                let! userOpt = model.AzureService.LogIn()
                dispatch (Login userOpt)
            with e ->
                System.Diagnostics.Debug.WriteLine(sprintf "Login failed: %s" (e.ToString()))
                dispatch (Login None)
        }

    let logout model dispatch =
        async {
            do! model.AzureService.LogOut()
            dispatch (Login None)
        }

    let loginFake model dispatch =
        async {
            dispatch (Login (Some "me@piggybank.com"))
        }

    let createLoginPage model dispatch  = 
        Xaml.ContentPage(
            Xaml.Grid(
                padding = 20.0,
                rowdefs = [
                    box "*"
                    box "auto"
                    box "auto"
                    box "*"
                ],
                children = [
                    Xaml.Button(text = "Login with Facebook",
                                backgroundColor = Color.Orange,
                                textColor = Color.White,
                                fontSize = Device.GetNamedSize(NamedSize.Large, typeof<Button>),
                                command = (fun () -> login model dispatch |> Async.StartImmediate))
                        .GridRow(1)
                    Xaml.Button(text = "Login as Test User",
                                backgroundColor = Color.DarkGray,
                                textColor = Color.White,
                                fontSize = Device.GetNamedSize(NamedSize.Large, typeof<Button>),
                                command = (fun () -> loginFake model dispatch |> Async.StartImmediate))
                        .GridRow(2)
                ]
            )
        ).HasNavigationBar(false).HasBackButton(false)

    let view (model : Model) dispatch =
        Xaml.NavigationPage(
            pages = [
                let loggedInUser = model.User
                yield createMainPage model dispatch 
                if loggedInUser.IsNone then
                    yield createLoginPage model dispatch
            ]
        )

type App(authFunc) as app =
    inherit Application()

    let azureService = new AzureService(authFunc)

    let program = Program.mkProgram (azureService |> App.init) App.update App.view

    let runner = 
        program
        |> Program.withConsoleTrace
        |> Program.withDynamicView app
        |> Program.run
