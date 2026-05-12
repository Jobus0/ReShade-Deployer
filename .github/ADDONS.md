# Advanced Add-ons Usage

Instead of just putting **.addon32** and **.addon64** files directly into the **Add-ons** folder, you can create a sub-folder for an add-on. This sub-folder can hold the add-on binaries alongside any other files and folders that should be symlinked or copied along with the add-on when deployed.

## Features

The following features are entirely optional—each feature can be used without the others.

### Include Additional Files
Any files placed in the add-on's sub-folder will be symlinked or copied to the game's directory when deployed.

### Add-on Specific Shaders and Textures
You can put **Shaders** and **Textures** folders inside the add-on folder. These paths will automatically be included in the deployed **ReShade.ini** file as add-on specific shaders/textures, without cluttering your main shaders/textures folders.

### Non-binary Add-ons
If a sub-folder doesn't contain any **.addon32** or **.addon64** files, it will still become available to choose from the add-on dropdown menu, even with 'Add-on Support' toggled off. This is useful for deploying other game mods alongside ReShade.

### Advanced Configuration (Add-on.ini)
You can place an optional `Add-on.ini` configuration file in the add-on sub-folder to adjust its deployment behavior. The following options are available:

- **`OverrideCopy`**: Controls how files are deployed for the add-on. Note that copied files cannot be automatically undeployed.
  - `Default`: Add-on binaries (`.addon32`/`.addon64`) are symlinked, while configuration and other files are copied.
  - `AlwaysCopy`: Always copy all files instead of creating symlinks.
  - `AlwaysSymlinks`: Always create symlinks for all files (including configuration files).
- **`ReShadeDllNameOverride`**: An optional name for the deployed ReShade DLL. When set, this overrides the default name derived from the graphics API (e.g., `dxgi.dll`). You can also specify a relative path (like `Custom\MyReShade.dll`) to place the ReShade DLL inside a sub-directory.
- **`SetupFile`**: An optional relative path to a runnable setup script (e.g., `.bat`, `.ps1`, `.exe`) that will be executed at the end of the deployment process.

### Add-ons Folder Structure
```
Add-ons
│── My Add-on
│   ├── *.addon32/addon64
│   ├── Shaders                 # Put your add-on specific shaders here.
│   │   └── *.fx/fxh
│   ├── Textures                # Put your add-on specific textures here.
│   │   └── *.png
│   ├── Add-on.ini              # Advanced per add-on config
│   └── *                       # Any other file you want to include
└── *.addon32/addon64
```

## Example Use Case: OptiScaler

To set up [OptiScaler](https://github.com/optiscaler/OptiScaler) using the Add-ons system:

1. Extract the contents of `Optiscaler_*.zip` into a new folder inside your `Add-ons` directory (e.g., `Add-ons/OptiScaler`).
2. Inside that folder, edit `OptiScaler.ini` and set `LoadReshade=auto` to `true`.
3. Create an `Add-on.ini` file in the same folder with this content:

```ini
OverrideCopy = AlwaysCopy
ReShadeDllNameOverride = ReShade64.dll
SetupFile = setup_windows.bat
```

This configuration tells ReShade Deployer to always copy the files (instead of symlinking), override the name of the ReShade DLL to `ReShade64.dll` during deployment (which OptiScaler requires), and execute `setup_windows.bat` after deploying.
