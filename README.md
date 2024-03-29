# STCEngine
This is a 2D game engine made for a graduate thesis for the STC program. It allows the user to create a simple 2D game and easily modify the level using JSON. It is mostly made for RPGs, but other genres are possible as well.  
The repository contains two demos, a user manual and the source code for one of the demos.
## Features
The engine allows the user to create a simple 2D game using premade "Components" attached to "GameObjects" written in JSON. To further modify the game logic, the user has to edit the game script. (more information in the Modification section)
More components may be added in the future, for example physics simulation.
## Installation
1. Navigate to the place where you want the app to be downloaded in Command Prompt.
2. Clone the repository using `git clone https://github.com/StudentTraineeCenter/STCEngine.git` or `git clone -b Platformer https://github.com/StudentTraineeCenter/STCEngine.git`

## Demos
The `STCEngine Demos` directory contains two demos to demonstrate the basic functionality, as well as a Czech user manual on controls and level editing.
To start a demo, run `STCEngine Demos/RPG Demo/net6.0-windows/STCEngine.exe`  
or `STCEngine Demos/Platformer Demo/net6.0-windows/STCEngine.exe`.
#### Demos showcase:
Combat mechanics: 
![Návrh bez názvu](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/b72f9621-d09f-4d60-92e2-396d92df3754)
  
NPC dialogue:
![Návrh bez názvu (1)](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/4375507a-888b-443f-ac52-025ca5dbeda3)
  
Dropping and collecting items:
![Návrh bez názvu (2)](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/081d81aa-942f-4328-bec2-61786a95e3c9)
  
Platformer gravity and jumping:
![Návrh bez názvu (3)](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/8e00998f-8737-43da-8b5a-52fd0fdc9b15)


## Modification
Simple level editing is possible by changing the JSON files in the `Assets` folder in the same folder as is the .exe file of the demo you wish to edit. Details on how to create GameObjects is in the user manual at `STCEngine Demos/Uživatelská příručka.docx`.  
  
Example of a JSON GameObject configuration:  
![image](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/5188959a-f96a-48e9-aa0c-386f364a3e90)

Game logic modification is possible by editing the `Test Apka STC Engine/Game.cs` script and building the application. (to open the project in Visual Studio, open `Test Apka STC Engine/STCEngine.sln`) 

This branch contains the source code for the RPG demo, if you wish to see the platformers source code, switch to the "Platformer" branch.

Thank you for checking this out! :)
