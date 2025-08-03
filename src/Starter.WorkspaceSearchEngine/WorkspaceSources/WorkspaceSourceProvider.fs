module Starter.WorkspaceSearchEngine.WorkspaceSourceProvider

// type JetBrainsIde =
//     | IntelliJ
//     | PyCharm
//     | PhpStorm
//     | GoLand
//     | Rider
//     | CLion
//     | RustRover
//     | WebStorm
//     | RubyMine
//
// type Software =
//     | VsCode of insiders: bool
//     | VisualStudio
//     | JetBrains of JetBrainsIde
//     | GitKraken
//     | Godot

let loadWorkspaceSources pluginPath =
    [| WorkspaceSources.VsCode.builder false
       WorkspaceSources.VsCode.builder true
       WorkspaceSources.JetBrains.getBuilder "Rider" "Rider" "rider64.exe" "Rider" "recentSolutions.xml" Icons.IconName.rider
       WorkspaceSources.JetBrains.getBuilder "PyCharm" "PyCharm" "pycharm64.exe" "PyCharm" "recentProjects.xml" Icons.IconName.pyCharm
       WorkspaceSources.JetBrains.getBuilder "IntelliJ IDEA Ultimate" "IntelliJ" "idea64.exe" "IdeaIC" "recentProjects.xml" Icons.IconName.intelliJ
       WorkspaceSources.JetBrains.getBuilder "IntelliJ IDEA Community Edition" "IntelliJ" "idea64.exe" "IdeaIC" "recentProjects.xml" Icons.IconName.intelliJ |]
    |> Array.choose (WorkspaceSourceBuilder.build pluginPath)
    |> Array.sortBy _.Name
