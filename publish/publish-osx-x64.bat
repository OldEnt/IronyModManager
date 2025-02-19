cd ..
dotnet publish src\IronyModManager.Shared\IronyModManager.Shared.csproj  /p:PublishProfile=src\IronyModManager.Shared\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Storage.Common\IronyModManager.Storage.Common.csproj  /p:PublishProfile=src\IronyModManager.Storage.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.IO.Common\IronyModManager.IO.Common.csproj  /p:PublishProfile=src\IronyModManager.IO.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Models.Common\IronyModManager.Models.Common.csproj  /p:PublishProfile=src\IronyModManager.Models.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Parser.Common\IronyModManager.Parser.Common.csproj  /p:PublishProfile=src\IronyModManager.Parser.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Services.Common\IronyModManager.Services.Common.csproj  /p:PublishProfile=src\IronyModManager.Services.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Storage\IronyModManager.Storage.csproj  /p:PublishProfile=src\IronyModManager.Storage\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Localization\IronyModManager.Localization.csproj  /p:PublishProfile=src\IronyModManager.Localization\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.IO\IronyModManager.IO.csproj  /p:PublishProfile=src\IronyModManager.IO\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Models\IronyModManager.Models.csproj  /p:PublishProfile=src\IronyModManager.Models\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Parser\IronyModManager.Parser.csproj  /p:PublishProfile=src\IronyModManager.Parser\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Services\IronyModManager.Services.csproj  /p:PublishProfile=src\IronyModManager.Services\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.DI\IronyModManager.DI.csproj  /p:PublishProfile=src\IronyModManager.DI\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Platform\IronyModManager.Platform.csproj  /p:PublishProfile=src\IronyModManager.Platform\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Common\IronyModManager.Common.csproj  /p:PublishProfile=src\IronyModManager.Common\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager.Updater\IronyModManager.Updater.csproj  /p:PublishProfile=src\IronyModManager.Updater\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
dotnet publish src\IronyModManager\IronyModManager.csproj  /p:PublishProfile=src\IronyModManager\Properties\PublishProfiles\osx-x64.pubxml --configuration Release
xcopy "src\IronyModManager\bin\Release\net6.0\osx-x64\*.dll" "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\" /Y /S /D
xcopy "src\IronyModManager\bin\Release\net6.0\osx-x64\*.json" "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\" /Y /S /D
xcopy "src\IronyModManager\bin\Release\net6.0\osx-x64\*.pdb" "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\" /Y /S /D
xcopy "src\IronyModManager.Updater\bin\Release\net6.0\publish\osx-x64\*.*" "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\" /Y /S /D
del "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\IronyModManager.runtimeconfig.dev.json" /S /Q
del "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\IronyModManager.Updater.runtimeconfig.dev.json" /S /Q
xcopy "References\*.*" "src\IronyModManager\bin\Release\net6.0\publish\osx-x64\" /Y /S /D
cd publish