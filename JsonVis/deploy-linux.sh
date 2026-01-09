#!/bin/bash
# 1. Build
dotnet publish -c Release -r linux-x64 --self-contained true

# 2. Run the included install script with sudo
sudo bash ./bin/Release/net8.0/linux-x64/publish/Assets/install.sh
