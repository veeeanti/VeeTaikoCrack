@echo off
if exist ".\bin\Release\net6.0\VeeTaikoCrack.dll" goto:squeekycleenbuild
echo Restoring...
dotnet restore
echo Building release DLL...
dotnet build --configuration Release
copy ".\bin\Release\net6.0\VeeTaikoCrack.dll" ".\VeeTaikoCrack.dll"
echo Built plugin can be found here, place it in ".\BepInEx\Plugins" and run Taiko.
pause>nul
exit

:squeekycleenbuild
echo Cleaning first...
dotnet clean
echo Restoring...
dotnet restore
echo Now building...
dotnet build --configuration Release
copy ".\bin\Release\net6.0\VeeTaikoCrack.dll" ".\VeeTaikoCrack.dll"
echo Built plugin can be found here, place it in ".\BepInEx\Plugins" and run Taiko.
pause>nul
exit