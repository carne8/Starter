namespace Starter

open System
open Avalonia.Controls
open Avalonia.Controls.Templates
open Starter.ViewModels

type ViewLocator() =
    interface IDataTemplate with

        member this.Build(data) =
            match data with
            | null -> null
            | data ->
                match data.GetType().FullName with
                | null -> null
                | fullName ->
                    let name = fullName.Replace("ViewModel", "View", StringComparison.Ordinal)

                    match Type.GetType(name) with
                    | null -> upcast TextBlock(Text = sprintf "Not Found: %s" name)
                    | type' ->
                        let view =
                            match Activator.CreateInstance(type') with
                            | null -> failwith "Failed to instantiate view"
                            | view -> view :?> Control

                        view.DataContext <- data
                        view

        member this.Match(data) = data :? ViewModelBase
