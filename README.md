# ReShade Deployer
### A centralized alternative to the official ReShade installer

![Main Window](Readme/MainWindow.png)

## Who is this for?
Those who want to:
- Keep a single shared Shaders folder for all their games instead of downloading new copies with every install.
- Update the .dll:s across all games to the latest version with a single click.
- Have your personal preconfigured ReShade.ini and ReShadePreset.ini files automatically installed.
- Quickly install ReShade by right-clicking a game .exe or shortcut.

## Features
A minimal installer program that lets you:
- Pick graphics API (DirectX 10 or later, DirectX 9, OpenGL, Vulkan).
- Automatically pick between 64-bit and 32-bit by analysing the executable.
- Toggle a checkbox to install the 'Add-on Support' version.
- Add a 'Deploy ReShade' option to Window's context (right-click) menu for .exe files and shortcuts.
  - This will open the normal program window, but the 'Select Game' button is replaced by a 'Deploy to Game.exe' button so you don't need to navigate to the executable.

## Contributing
Pull requests and request issues are welcome.

### Languages
Want to help translate ReShade Deployer into another language? ReShade Deployer uses the .NET XML resource (.resx) system for all UI text.

For example, to translate to French, just duplicate *UIStrings.resx* and name it *UIStrings.fr.resx* and start translating the values. The program language is automatically selected based on your OS's language.