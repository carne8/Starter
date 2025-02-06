namespace Starter

open System
open Avalonia.Controls
open Avalonia.Controls.Templates
open Starter.ViewModels

type ViewLocator() =
    interface IDataTemplate with

        member this.Build(data) =
            if isNull data then
                null
            else
                match data.GetType().FullName with
                | null -> null
                | fullName ->
                    let name = fullName.Replace("ViewModel", "View", StringComparison.Ordinal)
                    let typ = Type.GetType(name)
                    if isNull typ then
                        upcast TextBlock(Text = sprintf "Not Found: %s" name)
                    else
                        let view = Activator.CreateInstance(typ) :?> Control
                        view.DataContext <- data
                        view

        member this.Match(data) = data :? ViewModelBase
