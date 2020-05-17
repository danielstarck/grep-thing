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
            let textMatches, lineCount = 
                Seq.fold
                    (fun (matchingLines, linesRead) line ->
                        let lineNumber = linesRead + 1

                        if line |> isTextMatch then
                            matchingLines @ [ { LineNumber = lineNumber; Text = line } ], lineNumber
                        else
                            matchingLines, lineNumber)
                    ([], 0)
                    (File.ReadLines file)

            if textMatches.IsEmpty then
                None
            else
                Some 
                    { Filename = file
                      Lines = lineCount
                      TextMatches = textMatches }

        Directory.GetFiles directory
        |> Array.filter shouldSearchFile
        |> Array.choose toFileMatch
        |> Array.toList

    let setSearchResultAndStatus (state: State) : State =
        let getRegex (query: string) : Result<Regex, string> =
            try
                query |> Regex |> Ok
            with
                | _ -> Error <| sprintf @"""%A"" is not valid regex." query

        let assertIsAtleastTwoChars (textQuery: string) =
            if textQuery.Length < 2 then
                Error "Text query must be of length 2 or greater."
            else
                Ok textQuery

        let result =
            ResultBuilder() {
                let! directory =
                    if Directory.Exists state.Directory then
                        Ok state.Directory
                    else
                        Error "Directory does not exist."

                let! shouldSearchFile =
                    state.FileQuery
                    |> getRegex
                    |> Result.map (fun regex -> (Path.GetFileName: string -> string) >> regex.IsMatch)

                let! isTextMatch =
                    state.TextQuery
                    |> assertIsAtleastTwoChars
                    |> Result.bind getRegex
                    |> Result.map (fun regex -> regex.IsMatch)

                return search directory shouldSearchFile isTextMatch
            }

        match result with
        | Ok searchResult ->
            let status =
                let lineCount =
                    searchResult
                    |> List.sumBy (fun fileMatch -> fileMatch.TextMatches.Length)

                sprintf
                    "Found %A matching lines in %A files."
                    lineCount
                    searchResult.Length

            { state with
                SearchResult = searchResult
                Status = status }
        | Error error ->
            { state with
                SearchResult = []
                Status = error }

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
              Line = "-"
              Text = sprintf "%A matches in %A lines." fileMatch.TextMatches.Length fileMatch.Lines }

        let matchRows =
            fileMatch.TextMatches
            |> List.map (fun (textMatch: TextMatch) ->
                { File = fileMatch.Filename
                  Line = string textMatch.LineNumber
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
