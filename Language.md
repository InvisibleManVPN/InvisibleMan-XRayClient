# Add new language to Invisible Gorilla XRay

> Here are the instructions for adding a new language to the app

Invisible Gorilla XRay supports various languages.

By doing these steps you can add your language to the app:

- Clone a copy of the repository:
    ```
    git clone "https://github.com/hvkeyn/InvisibleGorilla-XRayClient.git"
    ```
- Switch to the `develop` branch:
    ```
    git checkout develop
    ```
- Go to the `InvisibleGorilla-XRay/Assets/Localization` and duplicate the `en-US.xaml`.
- Rename the duplicated file to your language.
- Now try to translate all the terms into your language.
- Go to the `InvisibleGorilla-XRay/Windows/SettingsWindow/SettingsWindow.xaml.cs` and add your language to the `Languages` dictionary:
    ```csharp
    private static readonly Dictionary<string, string> Languages = new Dictionary<string, string>() {
        { "en-US", "English" },
        { "xx-XX", "Example" }  // Your language
    };
    ```
- Test the app and push your changes as a pull request.
