# FloodzillaWeb

This is the source code for the SVPA Floodzilla Gage Network website, including the administrative tools.

## Prerequisites

Install node.js from https://nodejs.org/en/.

## Building and Deployment

The project file builds with Visual Studio 2019.

To build the react-based front end, run 'npm install' and 'npm run build' in ..\react-fz-client; then run copybuild.bat to copy the client into this project.

To deploy to Azure, right-click on the FloodzillaWeb project in Solution Explorer and choose "Publish...".

We are currently using the following settings for deployment:

* Configuration: Release
* Target Framework: netcoreapp3.1
* Deployment Mode: Framework-Dependent
* Target Runtine: Portable
* File Publish Options: Remove additional files at destination


## TODO
* Support multiple regions and organizations
* Remove usage of EntityFramework
* Remove Models\FzModels
* Rework Startup.cs to be more .NET 6 friendly
* Remove or rework ApplicationCache and RecoveryCache
* Use builtin JSON instead of Newtonsoft to simplify integration?
* Extract SVPA-specific text
* Remove all direct references to SQL
