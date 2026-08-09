#!/bin/bash
rm -rf ./build
dotnet publish src/Starter                                         --configuration Release -o ./build # -p:DefineConstants=DEBUG_LOGS
dotnet publish src/Starter.ApplicationSearchEngine                 --configuration Release --ucr -f net10.0 -p:EnableWindowsTargeting=true -o ./build/Plugins/ApplicationSearchEngine
dotnet publish src/Starter.EverythingSearchEngine                  --configuration Release -o ./build/Plugins/EverythingSearchEngine
dotnet publish src/Starter.UrlSearchEngine/Starter.UrlSearchEngine --configuration Release -o ./build/Plugins/UrlSearchEngine
dotnet publish src/Starter.WebSearchEngine                         --configuration Release -o ./build/Plugins/WebSearchEngine
dotnet publish src/Starter.WorkspaceSearchEngine                   --configuration Release -o ./build/Plugins/WorkspaceSearchEngine
dotnet publish src/Starter.Calculator                              --configuration Release -o ./build/Plugins/Calculator
