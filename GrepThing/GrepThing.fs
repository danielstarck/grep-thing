// Copyright 2018-2019 Fabulous contributors. See LICENSE.md for license.
namespace GrepThing

open System
open System.Diagnostics
open Fabulous
open Fabulous.XamarinForms
open Fabulous.XamarinForms.LiveUpdate
open Xamarin.Forms

module App = 
    type Model = 
      { Directory : string
        Files : string
        Text: string }

    type Msg = 
        | UpdateDirectory of string
        | UpdateFiles of string
        | UpdateText of string

    let initModel = { Directory = @"C:\repos\grep-thing"; Files = String.Empty; Text = String.Empty }

    let init () = initModel, Cmd.none

    let update msg model =
        match msg with
        | UpdateDirectory directory -> { model with Directory = directory }, Cmd.none
        | _ -> model, Cmd.none
        


    let view (model: Model) dispatch =
        let directoryInput =
            View.StackLayout(
                orientation = StackOrientation.Horizontal,
                children = [
                    View.Label(text = "Directory", width = 50.0)
                    View.Entry(
                        text = model.Directory,
                        textChanged = (fun (eventArgs : TextChangedEventArgs) -> UpdateDirectory eventArgs.NewTextValue |> dispatch),
                        width = 200.0)])
            
        let filesInput =
            View.StackLayout(
                orientation = StackOrientation.Horizontal,
                children = [
                    View.Label(text = "Files", width = 50.0)
                    View.Entry(
                        text = model.Files,
                        textChanged = (fun (eventArgs : TextChangedEventArgs) -> UpdateFiles eventArgs.NewTextValue |> dispatch),
                        width = 200.0)])
            
        let textInput =
            View.StackLayout(
                orientation = StackOrientation.Horizontal,
                children = [
                    View.Label(text = "Text", width = 50.0)
                    View.Entry(
                        text = model.Text,
                        textChanged = (fun (eventArgs : TextChangedEventArgs) -> UpdateText eventArgs.NewTextValue |> dispatch),
                        width = 200.0)])
                
        let tableView =
//            View.TableView(root = View.TableRoot(items = [View.TextCell("some text"); View.TextCell("some text2")]))
            let tableSection1 = View.TableSection(items = [View.TextCell "item1"; View.TextCell "item2"])
            let tableSection2 = View.TableSection(items = [View.TextCell "item1"; View.TextCell "item2"])
            
            View.TableView(backgroundColor = Color.RoyalBlue, root = View.TableRoot(items = [tableSection1; tableSection2]))
//                .Children([View.TextCell("some text"); View.TextCell("some text2")])
                
        View.ContentPage(
            content =
                View.StackLayout(padding = Thickness 20.0, verticalOptions = LayoutOptions.Center, height = 200.0,
                    children = [
                        directoryInput
                        filesInput
                        textInput
                        tableView
                        
//                View.Label(text = sprintf "%d" model.Count, horizontalOptions = LayoutOptions.Center, width=200.0, horizontalTextAlignment=TextAlignment.Center)
//                View.Button(text = "Increment", command = (fun () -> dispatch Increment), horizontalOptions = LayoutOptions.Center)
//                View.Button(text = "Decrement", command = (fun () -> dispatch Decrement), horizontalOptions = LayoutOptions.Center)
//                View.Label(text = "Timer", horizontalOptions = LayoutOptions.Center)
//                View.Switch(isToggled = model.TimerOn, toggled = (fun on -> dispatch (TimerToggled on.Value)), horizontalOptions = LayoutOptions.Center)
//                View.Slider(minimumMaximum = (0.0, 10.0), value = double model.Step, valueChanged = (fun args -> dispatch (SetStep (int (args.NewValue + 0.5)))), horizontalOptions = LayoutOptions.FillAndExpand)
//                View.Label(text = sprintf "Step size: %d" model.Step, horizontalOptions = LayoutOptions.Center) 
//                View.Button(text = "Reset", horizontalOptions = LayoutOptions.Center, command = (fun () -> dispatch Reset), commandCanExecute = (model <> initModel))
                    ]))
        

    // Note, this declaration is needed if you enable LiveUpdate
    let program = Program.mkProgram init update view

type App () as app = 
    inherit Application ()

    let runner = 
        App.program
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> XamarinFormsProgram.run app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/tools.html#live-update for further  instructions.
    //
    //do runner.EnableLiveUpdate()
#endif    

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Fabulous/Fabulous.XamarinForms/models.html#saving-application-state for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif


