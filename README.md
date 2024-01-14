# STCEngine
This is a 2D game engine made for a graduate thesis for the STC program. It allows the user to create a simple 2D game and easily modify the level using JSON. It is mostly made for RPGs, but other genres are possible as well.

# Demos
The "STCEngine Demos" directory contains two demos to demonstrate the basic functionality, as well as a Czech user manual on controls and level editing.
To start a demo, run "STCEngine Demos/RPG Demo/net6.0-windows/STCEngine.exe" or "STCEngine Demos/Platformer Demo/net6.0-windows/STCEngine.exe".
## RPG Demo showcase:
Combat mechanics: 
![Návrh bez názvu](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/b72f9621-d09f-4d60-92e2-396d92df3754)
NPC dialogue:
![Návrh bez názvu (1)](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/4375507a-888b-443f-ac52-025ca5dbeda3)
Dropping items:
![Návrh bez názvu (2)](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/081d81aa-942f-4328-bec2-61786a95e3c9)



# Modification
Simple level editing is possible by changing the JOSN files in the "Assets" folder in the same folder as is the .exe file of the demo you wish to edit. Details on how to create GameObjects is in the user manual at "STCEngine Demos/Uživatelská příručka.docx"
Example of a JSON GameObject configuration:
![image](https://github.com/StudentTraineeCenter/STCEngine/assets/146582539/5188959a-f96a-48e9-aa0c-386f364a3e90)

Game logic modification is possible by editing the "Test Apka STC Engine/Game.cs" script and building the application. (to open the project, go to the "Test Apka STC Engine" directory and open STCEngine.sln using Visual Studio) 

This branch contains the source code for the platformer demo, if you wish to see the RPGs source code, switch to the main branch.

Thank you for checking this out! :)
