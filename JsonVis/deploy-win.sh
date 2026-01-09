#!/bin/bash
dotnet publish -c Release --self-contained true /p:PublishProfile=win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
