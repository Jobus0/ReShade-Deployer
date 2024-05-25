# ReShade Deployer
### A centralized alternative to the official ReShade installer

![Main Window](Readme/MainWindow.png)

## Who is this for?
Those who want to:
- Keep a single shared Shaders folder for all their games instead of downloading new copies with every install.
- Update the .dll:s across all games to the latest version with a single click.
- Have your personal preconfigured ReShade.ini and ReShadePreset.ini files automatically installed.
- Quickly install ReShade from the (right-click) context menu on a game .exe or shortcut.

## Features
A minimal installer program that lets you:
- Pick graphics API (DirectX 10 or later, DirectX 9, OpenGL, Vulkan).
- Automatically pick the rendering API and 32/64-bit versions by analysing the executable.
- Toggle a checkbox to install the 'Add-on Support' version.
- Add a 'Deploy ReShade' option to Windows's context (right-click) menu for .exe files and shortcuts.
- Update all deployed ReShade .dll:s with a single click.
  - Possible because it creates symlinks instead of copying the .dll:s.
- Update the program itself with a single click.

## Installation
1. Download the [latest release](https://github.com/Jobus0/ReShade-Deployer/releases/latest).
2. Extract **ReShade Deployer.exe** anywhere you want it.
   1. This is where you must keep your **Shaders** and **Textures** folders.
   2. If you already have a folder for **Shaders** and **Textures**, you can place **ReShade Deployer.exe** in the same folder.
3. Run the program and allow the first-time setup.
4. (Optional) Enable **Context Menu Deploy** from the **⚙** options menu.
5. (Optional) Put any shader and texture files into the **Shaders** and **Textures** folders.
6. (Optional) Put a **ReShade.ini** and/or **ReShadePreset.ini** next to **ReShade Deployer.exe** to make the deployer automatically include those .ini:s when deploying to games.
   1. When your custom **ReShade.ini** is deployed, some specific settings (like the search paths) will be overriden or created if missing. 

### Folder Structure
```
ReShade
├── lib                             # Contains the ReShade .dll:s, shader cache, and deployer settings. Can be ignored.
├── Shaders                         # Put your shaders here.
│   └── *.fx/fxh
├── Textures                        # Put your textures here.
│   └── *.png
├── ReShade Deployer.exe
├── ReShade.ini                     # Optional
└── ReShadePreset.ini               # Optional
```

## Usage
There are two ways to use ReShade Deployer: Running it normally, or running it from Windows's context menu.

### Normal
1. Run the program.
2. Select the game you want to deploy ReShade to using the 'Select Game' button.
3. The target graphics API (DirectX, Vulkan, etc) should be automatically selected. If not, select it manually. If you are unsure, check the API section of the game's [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Home) page.
4. Press the 'Deploy to Game' button.
5. If an existing ReShade preset is detected, it will ask whether to overwrite it.

### Context Menu
To enable this, run the program, and from the **⚙** options menu, check the **Context Menu Deploy** option.
1. Right-click any game .exe or shortcut in Windows's file explorer or desktop, and select 'Deploy ReShade'.
2. The target graphics API (DirectX, Vulkan, etc) should be automatically selected. If not, select it manually. If you are unsure, check the API section of the game's [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Home) page.
3. Press the 'Deploy to Game' button.
4. If an existing ReShade preset is detected, it will ask whether to overwrite it.

## Uninstallation
1. Uncheck **Context Menu Deploy** from the **⚙** options menu if it was previously checked.
2. Delete **ReShade Deployer.exe**.

## Contributing
Pull requests and request issues are welcome.

### Languages
Want to help translate ReShade Deployer into another language? ReShade Deployer uses the .NET XML resource (.resx) system for all UI text.

For example, to translate to French, just duplicate *UIStrings.resx* and name it *UIStrings.fr.resx* and start translating the values. The program language is automatically selected based on your OS's language.