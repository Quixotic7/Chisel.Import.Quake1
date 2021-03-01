# Chisel Quake1 Map Importer
Import Quake 1 maps into Chisel Brushes. 

Based off of the [Valve Map Format 2006 Importer](https://github.com/Henry00IS/Chisel.Import.Source) by [Henry00IS](https://github.com/Henry00IS).

## Features

- Support for Trenchbroom hierarchy. Brushes are grouped into gameobjects representing layers and groups. 
- Brushes are grouped by class type. 
- Valve 220 format well supported. 
- Quake Standard format works but some textures may be oriented wrong. Rotations not imported yet. 

## Requirements

You will need Chisel to use this addon. You can grab the latest version at https://github.com/RadicalCSG/Chisel.Prototype

 Unity 2020.2.b11 or newer is required.

## How to import textures

Export your textures from your wads using TexMex. Import them to a folder in Unity. 

Select "Assets/Chisel/Quake 1 Importer/Create Materials For Source Textures" in Unity. 

Select all your textures, then click the "Create" button. A new folder of materials will be added. 

The default shader for the materials can also be changed. 

I recommend changing texture filtering to "Point (no filter)" for that crisp, pixelated look. 

## How to import a map

Create a new empty game object in your scene.

Add the script "Q1MapImporter" to the gameobject. 

Press the button "Import Map"

Select your map in the file browser.

Wait.

Enjoy your map inside of Unity!

## Known Issues

- Complex brushes may break Chisel and will not be imported.
- Quake 1 standard texture orientations might not be correct.
- Importing large maps requires a lot of memory to process and might crash Unity. Close other applications and try again.

## Version History

**20210301** 

	-	Initial release. 

