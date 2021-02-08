# ChocolateStore

ChocolateStore is a free and open source software that allows caching of Chocolatey packages for easy offline installation  
Also cache chocolatey packages to efficiently provision multiple machines or VMs on a LAN

### LICENSE

---

[Apache 2.0](LICENSE)

### COMPILATION REQUIREMENTS

---

**Using Visual Studio**

> -   Visual Studio 2015
> -   .NET Framewrok 4.0
> -   NuGet Package Manager with "Allow NuGet to download missing packages" setting enabled

**Using command line**  
Install these tools

> -   [.NET SDK](https://dotnet.microsoft.com/download/dotnet-core)
> -   [NuGet CommandLine](https://www.nuget.org/downloads)
>     -   or install [NuGet CommandLine with chocolatey](https://chocolatey.org/packages/NuGet.CommandLine)
> -   [.NET Framework 4.6.1](https://dotnet.microsoft.com/download/dotnet-framework)

### **Instructions**

---

Clone repository to your machine:  
`git clone https://github.com/sr258/ChocolateStore.git`

Restore NuGet packages with NuGet Commandline:  
`cd .\ChocolateStore\src\ChocolateStore`  
`nuget restore .\ChocolateStore.csproj -OutputDirectory ..\..\packages`

Compile application:

-   Default build  
    `dotnet build`
-   Debug build  
    `dotnet build -c Debug`
-   Release build  
    `dotnet build -c Release`

After completing the compilation, it is possible to merge compiled files as a single standalone executable using [ILMerge](https://github.com/dotnet/ILMerge) (or [ILMerge GUI](https://wvd-vegt.bitbucket.io/ilmergegui.html))

### SYNTAX

---

`ChocolateStore <directory> <package> variable1=value1,value2 variable2=value3`

### **EXAMPLES**

---

In this example, we will store the latest version of GoogleChrome on a network share and install it from a client on the LAN.
We will also store the latest version of Firefox as an example for using variables.

1. In a command prompt, browse to the ChocolateStore executable folder.

2. Execute the following command. Note that the first argument is a network share for which the current user has "write" permissions. This will download the GoogleChrome package, download the installer and modify the package to point to the local installer.

    `ChocolateStore M:\Store GoogleChrome`

    For packages containing variables, add a third argument to the syntax, which is the name of the variable followed by the value to be assigned.
    This is mandatory in packages that use variables in the download url.  
    This argument support multiple variables and values.

    `ChocolateStore M:\Store Firefox locale=en-US`

3. From a computer that would like to have GoogleChrome installed and from which the current user has "read" permissions to the network share execute the following command:

    `cinst GoogleChrome -source M:\Store`

    - _Note that installing the package with variables is no different from packages that do not contain variables._
