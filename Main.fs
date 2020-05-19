namespace GrepThing

module GrepThing =
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Avalonia.Layout
    open System
    open System.IO
    open System.Text.RegularExpressions
    open Elmish

    type State =
        { Directory: string
          FileQuery: string
          TextQuery: string
          Status: string
          SearchResult: SearchResult }

    let emptySearchResult = { FileMatches = []; FilesSearched = 0 }

    let init () =
        { Directory = String.Empty
          FileQuery = String.Empty
          TextQuery = String.Empty
          Status = "OK"
          SearchResult = emptySearchResult },
        Cmd.none

    type Msg =
        | NewSearchQuery of SearchQueryUpdate
        | NewDirectorySearchParameters of DirectorySearchParameters
        | FileSearched of FileMatch Option

    and SearchQueryUpdate =
        | Directory of string
        | FileQuery of string
        | TextQuery of string
                
    let status searchResult =
        let matchingLinesCount =
            searchResult.FileMatches
            |> List.sumBy (fun fileMatch -> fileMatch.TextMatches.Length)

        sprintf
            "Found %A matching lines in %A files (of %A searched)."
            matchingLinesCount
            searchResult.FileMatches.Length
            searchResult.FilesSearched

    let cmdGetFiles (queryInput: QueryInput) =
        Cmd.OfAsync.perform
            (fun (directory, shouldSearchFile) ->
                async {
                    return directory 
                    |> Directory.EnumerateFiles 
                    |> Seq.filter shouldSearchFile
                })
            (queryInput.Directory, queryInput.ShouldSearchFile)
            (fun files -> 
                NewDirectorySearchParameters
                    { Files = files
                      IsTextMatch = queryInput.IsTextMatch })

    let validateQueryInput directory fileQuery textQuery =
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

        ResultBuilder() {
            let! directory =
                if Directory.Exists directory then
                    Ok directory
                else
                    Error "Directory does not exist."

            let! shouldSearchFile =
                fileQuery
                |> getRegex
                |> Result.map (fun regex -> (Path.GetFileName: string -> string) >> regex.IsMatch)

            let! isTextMatch =
                textQuery
                |> assertIsAtleastTwoChars
                |> Result.bind getRegex
                |> Result.map (fun regex -> regex.IsMatch)

            return
                { Directory = directory
                  ShouldSearchFile = shouldSearchFile
                  IsTextMatch = isTextMatch }
        }

    let searchFile (isTextMatch: string -> bool) (file: string) : FileMatch Option Async =
        async {
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

            return
                if textMatches.IsEmpty then
                    None
                else
                    Some 
                        { Filename = file
                          Lines = lineCount
                          TextMatches = textMatches } }
                      
    let searchDirectory (parameters: DirectorySearchParameters) : Cmd<_> =
        Seq.map
            (fun file -> Cmd.OfAsync.perform (searchFile parameters.IsTextMatch) file FileSearched)
            parameters.Files
        |> Cmd.batch

    let update (msg: Msg) (state: State) : State * Cmd<_> =
        match msg with
        | NewSearchQuery searchQueryUpdate ->
            let directory, fileQuery, textQuery =
                match searchQueryUpdate with
                | Directory directory -> directory, state.FileQuery, state.TextQuery
                | FileQuery fileQuery -> state.Directory, fileQuery, state.TextQuery
                | TextQuery textQuery -> state.Directory, state.FileQuery, textQuery

            let cmd, status =
                match validateQueryInput directory fileQuery textQuery with
                | Ok queryInput ->
                    cmdGetFiles queryInput, status state.SearchResult
                | Error error ->
                    Cmd.none, error

            { state with
                Directory = directory
                FileQuery = fileQuery
                TextQuery = textQuery
                Status = status
                SearchResult = emptySearchResult },
            cmd
        | NewDirectorySearchParameters searchParameters ->
            { state with
                SearchResult = emptySearchResult
                Status = status emptySearchResult },
            searchDirectory searchParameters
        | FileSearched fileMatchOption ->
            match fileMatchOption with
            | Some fileMatch ->
                let searchResult =
                    { FileMatches = state.SearchResult.FileMatches @ [ fileMatch ]
                      FilesSearched = state.SearchResult.FilesSearched + 1 }
                { state with
                    SearchResult = searchResult
                    Status = status searchResult },
                Cmd.none
            | None ->
                let searchResult = { state.SearchResult with FilesSearched = state.SearchResult.FilesSearched + 1 }
                { state with SearchResult = searchResult }, Cmd.none

    let fontSize = 18.0

    let labelledTextBox dispatch (label: string) (text: string) (msg: string -> Msg) =
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
                          TextBox.verticalAlignment VerticalAlignment.Center ] ] ]
        :> Avalonia.FuncUI.Types.IView

    let queryInputControls state dispatch =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.children
              <| List.map (fun (label, text, msg) -> labelledTextBox dispatch label text msg)
                     [ "Directory", state.Directory, Directory >> NewSearchQuery
                       "File", state.FileQuery, FileQuery >> NewSearchQuery
                       "Text", state.TextQuery, TextQuery >> NewSearchQuery ] ]

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
                            [ TextBlock.fontSize fontSize
                              TextBlock.text state.Status ] ] ]
                  DataGrid.create
                      [ DataGrid.items <| List.collect toGridRows state.SearchResult.FileMatches
                        DataGrid.autoGenerateColumns true ] ] ]
