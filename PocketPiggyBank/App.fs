namespace PocketPiggyBank

open System.Globalization
open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Microsoft.WindowsAzure.MobileServices
open Newtonsoft.Json
open PocketPiggyBank.Services
open Xamarin.Forms

module App =
    type UIMode = Display | MoneyIn | MoneyOut

    type IntermediateState =
        | None
        | AdjustingBalance of float
     
    type Model =
        {
            Balance : float
            CurrencySymbol : string
            UIMode : UIMode

            [<JsonIgnore>]
            IntermediateState : IntermediateState

            [<JsonIgnore>]
            AzureService : AzureService

            [<JsonIgnore>]
            IsBusy : bool
        }
        with 
            member this.IsLoggedIn() = this.AzureService.IsLoggedIn()
            member this.LogIn (p : MobileServiceAuthenticationProvider) = this.AzureService.LogIn p
            member this.LogOut() = this.AzureService.LogOut()
            member this.GetLatestBalance() = this.AzureService.GetLatestBalance()
            member this.AdjustBalance amount = this.AzureService.AdjustBalance amount

    type Msg =
        | RefreshedBalance of float
        | ChangeMode of UIMode
        | ChangeBusy of bool
        | UpdateIntermediateState of IntermediateState
        | None

    let loadModel azureService =
        match Application.Current.Properties.TryGetValue "Model" with
        | true, value -> let m = JsonConvert.DeserializeObject<Model>(value :?> string)
                         {m with AzureService = azureService; IsBusy = true; IntermediateState = IntermediateState.None}
        | _           -> { 
                            Balance = 0.; 
                            CurrencySymbol = NumberFormatInfo.CurrentInfo.CurrencySymbol; 
                            AzureService = azureService;
                            UIMode = Display;
                            IntermediateState = IntermediateState.None;
                            IsBusy = true;
                         }

    let init azureService () =
        let model = loadModel azureService

        let onInit = async {
                         let! l = azureService.IsLoggedIn()
                         match l with
                         | true -> let! b = azureService.GetLatestBalance()
                                   return (RefreshedBalance(b))
                         | false -> return (ChangeBusy(false))
                     } |> Cmd.ofAsyncMsg 
        model, onInit

    let update msg model =
        let newModel = match msg with
                        | RefreshedBalance x -> {model with Balance = x; IsBusy = false}
                        | ChangeMode x -> {model with UIMode = x}
                        | ChangeBusy b -> {model with IsBusy = b}
                        | UpdateIntermediateState x -> {model with IntermediateState = x}
                        | None -> model
        Application.Current.Properties.["Model"] <- JsonConvert.SerializeObject(newModel)
        async {
            do! Application.Current.SavePropertiesAsync() |> Async.AwaitTask
        } |> Async.Start
        newModel, Cmd.none

    let adjustBalance (model : Model) dispatch =
        async {
            match model.IntermediateState with
            | AdjustingBalance x -> dispatch (ChangeBusy(true))
                                    let! newBalance = model.AdjustBalance x
                                    do! Async.Sleep(5000)
                                    dispatch (UpdateIntermediateState(IntermediateState.None))
                                    dispatch (RefreshedBalance(newBalance))
                                    dispatch (ChangeMode(UIMode.Display))
            | _ -> dispatch None
         }

    let refreshBalance (model : Model) dispatch =
        async {
             let! l = model.IsLoggedIn()
             if l then
                 let! b = model.GetLatestBalance()
                 dispatch (RefreshedBalance(b)) 
         }

    let login (model : Model) dispatch p =
        async {
            do! model.LogIn p
            do! refreshBalance model dispatch
        }

    let createBusyLayer () =
        View.Grid(
            backgroundColor = Color.FromHex "#A0000000",
            children = [
                View.ActivityIndicator(
                    isRunning = true,
                    color = Color.White,
                    scale = 2.
                )
            ]
        )

    let createMoneyInOutView model dispatch n =
        View.Grid(
            backgroundColor = Color.FromHex "#50000000",
            padding = 40.,
            rowdefs = [box "*"; box "*"; box "*"],
            children = [
                View.Frame(
                    content = View.Grid(
                        columnSpacing = 20.,
                        rowdefs = [box "*"; box "*"],
                        coldefs = [box "*"; box "*"],
                        children = [
                            View.Entry(
                                placeholder = "Enter the amount",
                                fontSize = 24.,
                                textChanged = (fun args -> dispatch (UpdateIntermediateState(AdjustingBalance((float args.NewTextValue) * n))))
                            ).GridColumnSpan(2).GridRow(0)
                            View.Button(
                                text = (if model.UIMode = MoneyIn then "Add" else "Remove"),
                                backgroundColor = (if model.UIMode = MoneyIn then Color.Green else Color.Red),
                                textColor = Color.White,
                                fontSize = 24.,
                                command = (fun () -> adjustBalance model dispatch |> Async.StartImmediate)
                            ).GridColumn(0).GridRow(1)
                            View.Button(
                                text = "Cancel",
                                backgroundColor = Color.DarkGray,
                                textColor = Color.White,
                                fontSize = 24.,
                                command = (fun () -> dispatch (UpdateIntermediateState(IntermediateState.None))
                                                     dispatch (ChangeMode(UIMode.Display)))
                            ).GridColumn(1).GridRow(1)
                        ]
                    )
                ).GridRow(1)
            ]
        )

    let createMainPage  model dispatch =
        View.NavigationPage(
            pages = [
                View.ContentPage(
                    title="Pocket Piggy Bank",
                    content=View.Grid(
                        children = [
                            yield View.Grid(
                                padding = 20.0,
                                rowdefs = [box "*"; box "auto"; box "*"; box "auto"; box "*"; box "auto"; box "*"],
                                children = [
                                    View.Image(
                                        source = "Pig",
                                        horizontalOptions = LayoutOptions.Center,
                                        verticalOptions = LayoutOptions.Center,
                                        aspect = Aspect.AspectFit,
                                        margin = Thickness(0.,0.,0.,20.)
                                    ).GridRow(3)
                                    View.Label(
                                        text = sprintf "%s%.2f" model.CurrencySymbol model.Balance,
                                        horizontalOptions = LayoutOptions.Center,
                                        verticalOptions = LayoutOptions.Center,
                                        fontSize = 48.
                                    ).GridRow(3)
                                    View.Button(
                                        text = "Put money in",
                                        fontAttributes = FontAttributes.Bold,
                                        backgroundColor = Color.FromHex "#F806D2",
                                        textColor = Color.White,
                                        fontSize = 24.,
                                        command = (fun () -> dispatch (ChangeMode(MoneyIn)))
                                    ).GridRow(1)
                                    View.Button(
                                        text = "Take money out",
                                        fontAttributes = FontAttributes.Bold,
                                        backgroundColor = Color.FromHex "#F806D2",
                                        textColor = Color.White,
                                        fontSize = 24.,
                                        command = (fun () -> dispatch (ChangeMode(MoneyOut)))
                                    ).GridRow(5)
                                ])
                            if model.UIMode = MoneyIn then
                                yield createMoneyInOutView model dispatch 1.
                            elif model.UIMode = MoneyOut then
                                yield createMoneyInOutView model dispatch -1.

                            if model.IsBusy then
                                yield createBusyLayer()
                        ]
                    )
                ).BarBackgroundColor(Color.Orange)
                 .BarTextColor(Color.White)
            ])

    let createLoginPage model dispatch = 
        View.ContentPage(
            content = View.Grid(
                padding = 20.0,
                rowdefs = [box "*"; box "auto"; box "auto"; box "*"],
                children = [
                            View.Button(
                                        text = "Log in with Facebook",
                                        backgroundColor = Color.Orange,
                                        textColor = Color.White,
                                        fontSize = Device.GetNamedSize(NamedSize.Large, typeof<Button>),
                                        command = (fun () -> login model dispatch MobileServiceAuthenticationProvider.Facebook |> Async.StartImmediate)
                                       ).GridRow(1)
                            View.Button(
                                        text = "Log in with Twitter",
                                        backgroundColor = Color.Blue,
                                        textColor = Color.White,
                                        fontSize = Device.GetNamedSize(NamedSize.Large, typeof<Button>),
                                        command = (fun () -> login model dispatch MobileServiceAuthenticationProvider.Twitter |> Async.StartImmediate)
                                       ).GridRow(2)
                           ]
            )
        )

    let view (model : Model) dispatch =
        match model.IsLoggedIn() |> Async.RunSynchronously with
        | true -> createMainPage model dispatch
        | false -> createLoginPage model dispatch

type App(authFunc) as app =
    inherit Application()

    let azureService = new AzureService(authFunc)

    let program = Program.mkProgram (azureService |> App.init) App.update App.view
    let runner = 
        program
        |> Program.withConsoleTrace
        |> Program.runWithDynamicView app

    #if DEBUG
    do runner.EnableLiveUpdate ()
    #endif