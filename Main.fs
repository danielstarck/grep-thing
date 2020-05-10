namespace GrepThing

module GrepThing =
    open Avalonia.Controls
    open Avalonia.Controls.Primitives
    open Avalonia.FuncUI.DSL
    open Avalonia.Layout
    open System

    type State =
        { Directory: string
          FileQuery: string
          TextQuery: string }

    let init =
        { Directory = @"C:\repos\NewApp"
          FileQuery = "*.*"
          TextQuery = String.Empty }

    type Msg =
        | NewDirectory of string
        | NewFileQuery of string
        | NewTextQuery of string
        | NoOp

    let update (msg: Msg) (state: State): State =
        match msg with
        | NewDirectory directory -> { state with Directory = directory }
        | NewFileQuery fileQuery -> { state with FileQuery = fileQuery }
        | NewTextQuery textQuery -> { state with TextQuery = textQuery }
        | NoOp -> state

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

    let view (state: State) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                [ queryInputControls state dispatch
                  Grid.create
                      [ Grid.rowDefinitions "Auto, Auto"
                        Grid.columnDefinitions "Auto, Auto"
                        Grid.children
                            [ TextBlock.create
                                [ TextBlock.text "AAA"
                                  Grid.column 0
                                  Grid.row 0 ]
                              TextBlock.create
                                  [ TextBlock.text "BBB"
                                    Grid.column 0
                                    Grid.row 1 ]
                              TextBlock.create
                                  [ TextBlock.text "CCC"
                                    Grid.column 1
                                    Grid.row 0 ]
                              TextBlock.create
                                  [ TextBlock.text "DDD"
                                    Grid.column 1
                                    Grid.row 1 ] ] ] ] ]
