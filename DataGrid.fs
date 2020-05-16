namespace Avalonia.FuncUI.DSL

[<AutoOpen>]
module DataGrid =
    open Avalonia.Controls
    open Avalonia.FuncUI.Types
    open Avalonia.FuncUI.Builder
    open System.Collections

    let create (attrs: IAttr<DataGrid> list): IView<DataGrid> = ViewBuilder.Create<DataGrid>(attrs)

    type DataGrid with

        static member items<'t when 't :> DataGrid>(value: IEnumerable): IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<IEnumerable>(DataGrid.ItemsProperty, value, ValueNone)

        static member autoGenerateColumns<'t when 't :> DataGrid>(value: bool): IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<bool>(DataGrid.AutoGenerateColumnsProperty, value, ValueNone)
