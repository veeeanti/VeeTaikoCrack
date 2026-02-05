## Requirements
- .NET SDK 6.0
 
## Instructions
- Make sure you have [.NET SDK 6.0 installed](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.428-windows-x64-installer)
- Open a Powershell window or CMD window.
- Navigate to the plugin folder you cloned.
- Run ``dotnet restore``. This is MANDATORY. Nothing will work and you'll get 30+ errors if you don't.
- Either run ``dotnet build --configuration Release`` or just ``dotnet build`` for debug version if needed.
- Copy the built 'VeeTaikoCrack.dll' from ``"bin\Release\net6.0"`` to your ``"BepInEx\Plugins"`` folder and run the game.
  
### Easier instructions
- Make sure you have [.NET SDK 6.0 installed](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.428-windows-x64-installer)
- Run the 'build.bat' file
- Copy the copied ``"VeeTaikoCrack.dll"`` that was placed in the root of the source code folder to your ``"BepInEx\Plugins"`` folder and run the game.
- The bat script can tell if you've built it before, if it knows then it'll clean first and then build. It also only does Release, if you need Debug for any reason, change it in the bat file using notepad.

-----------------------------------------------------------------

## Details

This is an attempt at making a "fake" PlayFab authentication using your SteamID to generate the auth for it to allow you to play online multiplayer with friends. It also spoofs the game's appID to register as Spacewar on Steam.
Taiko Rhythm Festival is an IL2CPP Unity game. That makes it immensely harder to work with, it's not impossible - just extremely difficult. This plugin likely doesn't fully work, the Steam appID spoofing part works but I'm unsure of the PlayFab part.
There's not much else to say about it, other than this shit is hard. lol

------------------------------------------------------------------

## Credits

@KirigiriX is to thank for the idea to use a BepInEx plugin this way, her repo "NekogiriFix" is where I pulled inspiration from originally (my GorillaCrack, now private, was based heavily on it) and then seeing more of what she's made for other games and more than successfully.
Thank you Kirigiri!
