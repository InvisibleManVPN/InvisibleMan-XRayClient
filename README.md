# Invisible Man - XRay Client

> A client for xray core

Invisible Man XRay is an open-source and free client that supports [xray core](https://github.com/XTLS/Xray-core). It provides an easy-to-use interface to configure and manage proxies and allows users to switch between different server locations.

![Image1](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/blob/master/Images/image-1.png)
![Image2](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/blob/master/Images/image-2.png)

## Getting started

- If you are new to this, please download the application from [releases](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/releases/latest).

- But if you want to get the source of the client, follow these steps:
  - Download the [requirements](#requirements)
  - Clone a copy of the repository:
    ```
    git clone "https://github.com/InvisibleManVPN/InvisibleMan-XRayClient.git"
    ```
  - Change to the directory:
    ```
    cd InvisibleMan-XRay
    ```
  - Make `XRayCore.dll` file and copy to the `/InvisibleMan-XRay/Libraries` directory:
    ```
    cd XRay-Wrapper
    go build --buildmode=c-shared -o XRayCore.dll -trimpath -ldflags "-s -w -buildid=" .
    md ..\InvisileMan-XRay\Libraries
    copy XRayCore.dll ..\InvisibleMan-XRay\Libraries   
    ```
  - Run the project:
    ```
    cd ..\InvisibleMan-XRay
    dotnet run
    ```

## Requirements

- Go https://go.dev/dl/
- .Net https://dotnet.microsoft.com/download

## Contributing

You can help this project by reporting problems, suggestions or contributing to the code.

### Report a problem or suggestion

Go to our [Issue tracker](https://github.com/InvisibleManVPN/InvisibleMan-XRayClient/issues) and check if your problem/suggestion is already reported. If not, create a new issue with a descriptive title and detail your suggestion or steps to reproduce the problem.

### Contribute to the code

If you know how to code, we welcome you to send fixes and new features!

## Contacts

- Web [invisiblemanvpn.github.io](https://invisiblemanvpn.github.io)
- Email [invisiblemanvpn@gmail.com](mailto:invisiblemanvpn@gmail.com)