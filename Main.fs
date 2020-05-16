namespace GrepThing

module GrepThing =
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Avalonia.Layout
    open System
    open System.IO
    open System.Text.RegularExpressions
    open System.Linq

    type State =
        { Directory: string
          FileQuery: string
          TextQuery: string
          Status: string
          SearchResult: SearchResult }

    let init =
        { Directory = String.Empty
          FileQuery = String.Empty
          TextQuery = String.Empty
          Status = "OK"
          SearchResult = [] }

    type Msg =
        | NewDirectory of string
        | NewFileQuery of string
        | NewTextQuery of string

    let search (directory: string) (shouldSearchFile: string -> bool) (isTextMatch: string -> bool) : SearchResult =
        let toFileMatch (file: string) : FileMatch Option =
            let textMatches: TextMatch List =
                let lines = File.ReadAllLines file

                lines
                |> Array.zip (Enumerable.Range(1, lines.Length) |> Enumerable.ToArray)
                |> Array.filter (fun (_, lineText) -> lineText |> isTextMatch)
                |> Array.map (fun (lineNumber, lineText) -> { Line = lineNumber; Text = lineText })
                |> Array.toList

            if textMatches.IsEmpty then
                None
            else
                Some { Filename = file; TextMatches = textMatches }

        Directory.GetFiles directory
        |> Array.filter shouldSearchFile
        |> Array.choose toFileMatch
        |> Array.toList

    let setSearchResultAndStatus (state: State) : State =
        let getRegex (query: string) : Regex Option =
            try
                query |> Regex |> Some
            with
                | _ -> None

        let getShouldSearchFile =
            state.FileQuery
            |> getRegex
            |> Option.map (fun regex -> (Path.GetFileName: string -> string) >> regex.IsMatch)

        let getIsTextMatch =
            state.TextQuery
            |> getRegex
            |> Option.map (fun regex -> regex.IsMatch)

        let searchResult, status =
            match 
                Directory.Exists state.Directory,
                getShouldSearchFile,
                getIsTextMatch with
                | false, _, _ -> [], "Directory does not exist"
                | _, None, _ -> [], "File: Invalid regex"
                | _, _, None -> [], "Text: Invalid regex"
                | _, Some shouldSearchFile, Some isTextMatch ->
                    search state.Directory shouldSearchFile isTextMatch, "OK"

        { state with
            SearchResult = searchResult
            Status = status }

    let update (msg: Msg) (state: State): State =
        match msg with
        | NewDirectory directory ->
            { state with Directory = directory } |> setSearchResultAndStatus
        | NewFileQuery fileQuery ->
            { state with FileQuery = fileQuery } |> setSearchResultAndStatus
        | NewTextQuery textQuery ->
            { state with TextQuery = textQuery } |> setSearchResultAndStatus

    let labelledTextBox dispatch (label: string) (text: string) (msg: string -> Msg) =
        let fontSize = 18.0

        DockPanel.create
            [ DockPanel.dock Dock.Top
              DockPanel.height 40.0
              DockPanel.verticalAlignment VerticalAlignment.Center
              DockPanel.children
                  [ TextBlock.create
                      [ TextBlock.fontSize fontSize
                        TextBlock.text label
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.width 90.0 ]

                    TextBox.create
                        [ TextBox.fontSize fontSize
                          TextBox.text text
                          TextBox.onTextChanged (msg >> dispatch)
                          TextBox.verticalAlignment VerticalAlignment.Center ] ] ] :> Avalonia.FuncUI.Types.IView

    let queryInputControls state dispatch =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.children
              <| List.map (fun (label, text, msg) -> labelledTextBox dispatch label text msg)
                     [ "Directory", state.Directory, NewDirectory
                       "File", state.FileQuery, NewFileQuery
                       "Text", state.TextQuery, NewTextQuery ] ]

    let toGridRows (fileMatch: FileMatch): GridRow list =
        let headerRow =
            { File = fileMatch.Filename
              Line = String.Empty
              Text = String.Empty }

        let matchRows =
            fileMatch.TextMatches
            |> List.map (fun (textMatch: TextMatch) ->
                { File = String.Empty
                  Line = string textMatch.Line
                  Text = textMatch.Text })

        headerRow :: matchRows

    let view (state: State) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                [ queryInputControls state dispatch
                  DockPanel.create
                    [ DockPanel.dock Dock.Top
                      DockPanel.height 40.0
                      DockPanel.verticalAlignment VerticalAlignment.Center  
                      DockPanel.children
                        [ TextBlock.create
                            [ TextBlock.fontSize 18.0
                              TextBlock.text state.Status ] ] ]
                  DataGrid.create
                      [ DataGrid.items
                        <| List.collect toGridRows state.SearchResult
                        DataGrid.autoGenerateColumns true ] ] ]
