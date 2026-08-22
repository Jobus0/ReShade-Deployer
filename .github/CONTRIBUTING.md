# Contributing to ReShade Deployer

Thank you for considering contributing to ReShade Deployer! All pull requests, feature requests, and bug reports are welcome.

## Reporting Bugs and Requesting Features
If you run into an issue or have an idea for a new feature, please [open an issue](../../../issues) on GitHub. Briefly describe the problem or feature, and include any relevant details or steps to reproduce if it's a bug.

## Development Setup
ReShade Deployer is a C# project built on .NET 10.
To build and run the project locally:
1. Ensure you have the [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.
2. Fork the repository and clone it to your local machine.
3. Open the project in your preferred IDE (Visual Studio, Rider, or VS Code) and build.
    - Note: You likely need to run the IDE as administrator to build due to the app requiring admin permissions for some operations, such as writing to the registry and creating symlinks.

## Submitting Pull Requests
1. Fork the project.
2. Create your feature branch (`git checkout -b feature/MyFeature`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/MyFeature`).
5. Open a Pull Request.

## Translations
Want to help translate ReShade Deployer into another language? ReShade Deployer uses the standard .NET XML resource (`.resx`) system for all UI text.

To add a new language:
1. Locate the `UIStrings.resx` file in the source code.
2. Duplicate it and append the target language code to the file name.
    - *For example, to translate to French, name the new file `UIStrings.fr.resx`.*
3. Open the new file and translate the values.
4. Submit a pull request with your new `.resx` file.

The program language is automatically selected for users based on their OS's display language.