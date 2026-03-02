# Unattended Server
#### _A BepInEx5 mod to Automate starting matches in Super Battle Golf_

# TO-DO
#### Re-add automatic messaging at 60 seconds and 10 seconds remaining on the countdown.
#### Add configurations to set number of holes and set courses. Right now it is always 27 holes in order.

## Installation

- [Install BepInEx 5](https://github.com/BepInEx/BepInEx/releases) 
- Download and drop the UnattendedServer.dll file in the \BepInEx\plugins folder of your Super Battle Golf Installation
- After opening the game for the first time, you can adjust the custom player limit in \BepInEx\config\com.kingcox22.sbg.unattended.server.cfg

## Building for source
*set the env variable ``SUPER_BATTLE_GOLF_PATH`` to your Super Battle Golf installation directory.*
```sh
dotnet build
```




