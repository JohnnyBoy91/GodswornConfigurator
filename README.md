# GodswornConfigurator
Features tested and working on single player only
## Features

* Customizable faction starting conditions & resource settings
* Customize Unit and Hero stats
* Customize Hero Skills
* Treiden commander mode scenario
* Create your own defense challenges on the Tervete map
* Customize Damage Type Modifiers
* Customizable worshipper spawn rate parameters
* Customizable level xp requirements & stat scaling
* Add Marauder to Saule Warcamp
* Customize other miscellaneous settings
* WIP spectator mode(not working in multiplayer, need to test with all players having mod installed?)

* Full config lists here: https://github.com/JohnnyBoy91/GodswornConfigurator/tree/1.0.04/GodSwornModding/Templates
## Download

https://github.com/JohnnyBoy91/GodswornModding/releases

## Source Code

https://github.com/JohnnyBoy91/GodswornModding/blob/main/GodSwornModding/ModManager.cs

## Installation & Setup Guide
 
Requires BepInEx Bleeding Edge build for IL2CPP
https://builds.bepinex.dev/projects/bepinex_be

Tested on Build# 729, December 2nd 2024 35f6b1b, "BepInEx Unity (IL2CPP) for Windows (x64) games"

Follow Installation Guide for IL2CPP Unity
https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html
1. Download Bepinex and confirm you have correct version
2. Extract into game root folder
3. Run the game and allow some time to generate modding files
4. Download this mod from the "Releases" section
5. Drop this mod into "Bepinex/plugins" folder.
6. This mod contains a DataConfig.txt file in it's folder, configure it to suit your needs and play the game.

## Uninstallation

* To remove completely, delete bepinex folder and files from game directory(the folder and files added during installation step 3). May have to also verify game files on steam.
* To disable the mod temporarily, move the mod folder out of the "Bepinex/plugins" folder, also the ModDataConfig file has a true/false toggle at the top to disable modding settings like unit stats on startup if you want to quickly switch back to vanilla stats
