namespace GrepThing

[<AutoOpen>]
module Types =
    type TextMatch = { Line: int; Text: string }

    type FileMatch = { Filename: string; TextMatches: TextMatch list }

    type SearchResult = FileMatch list

    type GridRow =
        { File: string
          Line: string
          Text: string }
