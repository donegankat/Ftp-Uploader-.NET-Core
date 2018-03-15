# Ftp-Uploader-.NET-Core
Console app used to upload files to an FTP directory

## Notes
- Currently broken because this code was copied from a regular .NET project into this .NET Core 2 project only to discover that WinSCP (the FTP client) does not work in .NET Core.
- Also this code needs some serious TLC for loading data from appSettings.json (and also implementing the usage of those settings throughout the app).
