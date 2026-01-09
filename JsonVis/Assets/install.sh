#!/bin/bash
# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# The parent directory of Assets is the actual publish folder
PUBLISH_PATH="$SCRIPT_DIR/.."

APP_NAME="jsonvis"
INSTALL_DIR="/opt/$APP_NAME"
BINARY_SOURCE_NAME="JsonVis"

# Check if build was successful
if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Build failed. Please check the logs above."
    exit 1
fi

echo ""
echo "-------------------------------------------------------"
echo " Installing to $INSTALL_DIR..."
echo "-------------------------------------------------------"

# Check for root privileges
if [ "$EUID" -ne 0 ]; then
  echo "PERMISSION DENIED: Please run this script with sudo:"
  echo "sudo ./install.sh"
  exit 1
fi

# Clean up old installation and create directory
rm -rf "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR"

# Copy published files to /opt/
if [ -d "$PUBLISH_PATH" ]; then
    cp -r "$PUBLISH_PATH"/* "$INSTALL_DIR/"
else
    echo "ERROR: Publish directory not found at $PUBLISH_PATH"
    exit 1
fi

# Set executable permissions
chmod +x "$INSTALL_DIR/$BINARY_SOURCE_NAME"

# Create symbolic link in /usr/local/bin
# This makes the app available globally as 'jsonvis'
ln -sf "$INSTALL_DIR/$BINARY_SOURCE_NAME" /usr/local/bin/$APP_NAME

echo "-------------------------------------------------------"
echo " SUCCESS: Installation complete!"
echo "-------------------------------------------------------"
echo "You can now start the application by typing: $APP_NAME"
echo ""
