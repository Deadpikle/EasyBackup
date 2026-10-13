#!/bin/bash

VERSION="0.9.0"
NAME="EasyBackup"
SRC_DIR="src/EasyBackup.Avalonia" # user ran script from main folder
if [ ! -d "$SRC_DIR" ]; then
    SRC_DIR= "../src/EasyBackup.Avalonia" # try 
fi
if [ ! -d "$SRC_DIR" ]; then
    echo "Please run from "installers" dir or from main repo directory"
    exit 1
fi
cd "$SRC_DIR"
echo "Building release for linux-x64..."
echo "Running clean..."
rm -rf obj
dotnet clean -c Release
echo "Running restore"
dotnet restore -r linux-x64 -p:UseMonoRuntime=false
echo "Running publish..."
dotnet publish -c Release -r linux-x64 -p:StripSymbols=False -p:PublishAot=False -p:UseMonoRuntime=false
echo "Zipping up linux-x64..."
cd bin/Release/net10.0/linux-x64/publish
zip -r "../../../../$NAME $VERSION linux-x64.zip" .
cd ../../../../../
# -----
echo "Building release for linux-arm64..."
echo "Running clean..."
rm -rf obj
dotnet clean -c Release
echo "Running restore..."
dotnet restore -r linux-arm64 -p:UseMonoRuntime=false
echo "Running publish..."
dotnet publish -c Release -r linux-arm64 -p:StripSymbols=False -p:PublishAot=False -p:UseMonoRuntime=false
cd bin/Release/net10.0/linux-arm64/publish
zip -r "../../../../$NAME $VERSION linux-arm64.zip" .
cd ../../../../../