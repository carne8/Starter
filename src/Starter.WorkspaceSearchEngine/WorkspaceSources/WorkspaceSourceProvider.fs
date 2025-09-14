module Starter.WorkspaceSearchEngine.WorkspaceSourceProvider

// type Software =
//     | VisualStudio
//     | GitKraken
//     | Godot

let loadWorkspaceSources pluginPath =
    [| WorkspaceSources.VsCode.builder false
       WorkspaceSources.VsCode.builder true
       WorkspaceSources.JetBrains.getBuilder "Rider" "Rider" "rider64.exe" "Rider" "recentSolutions.xml" Icons.IconName.rider
       WorkspaceSources.JetBrains.getBuilder "PyCharm" "PyCharm" "pycharm64.exe" "PyCharm" "recentProjects.xml" Icons.IconName.pyCharm
       WorkspaceSources.JetBrains.getBuilder "IntelliJ IDEA Ultimate" "IntelliJ" "idea64.exe" "IdeaIC" "recentProjects.xml" Icons.IconName.intelliJ
       WorkspaceSources.JetBrains.getBuilder "IntelliJ IDEA Community Edition" "IntelliJ" "idea64.exe" "IdeaIC" "recentProjects.xml" Icons.IconName.intelliJ
       WorkspaceSources.JetBrains.getBuilder "GoLand" "GoLand" "goland64.exe" "GoLand" "recentProjects.xml" Icons.IconName.goLand
       WorkspaceSources.JetBrains.getBuilder "PhpStorm" "PhpStorm" "phpstorm64.exe" "PhpStorm" "recentProjects.xml" Icons.IconName.phpStorm
       WorkspaceSources.JetBrains.getBuilder "WebStorm" "WebStorm" "webstorm64.exe" "WebStorm" "recentProjects.xml" Icons.IconName.webStorm
       WorkspaceSources.JetBrains.getBuilder "RubyMine" "RubyMine" "rubymine64.exe" "RubyMine" "recentProjects.xml" Icons.IconName.rubyMine
       WorkspaceSources.JetBrains.getBuilder "RustRover" "RustRover" "rustrover64.exe" "RustRover" "recentProjects.xml" Icons.IconName.rustRover
       WorkspaceSources.JetBrains.getBuilder "CLion" "CLion" "clion64.exe" "CLion" "recentProjects.xml" Icons.IconName.cLion
       WorkspaceSources.AndroidStudio.builder |]
    |> Array.choose (WorkspaceSourceBuilder.build pluginPath)
    |> Array.sortBy _.Name
