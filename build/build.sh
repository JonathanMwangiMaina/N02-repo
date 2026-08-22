#!/bin/bash
# build.sh - Cross-platform build script for IndieFps
# Usage: ./build.sh [Debug|Release] [version]

set -euo pipefail

CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"
SOLUTION="IndieFps.sln"
ARTIFACTS_DIR="artifacts"

echo "=========================================="
echo "Building IndieFps - $CONFIGURATION v$VERSION"
echo "=========================================="

# Clean previous builds
rm -rf "$ARTIFACTS_DIR"
mkdir -p "$ARTIFACTS_DIR"

# Restore
echo "Restoring packages..."
dotnet restore "$SOLUTION"

# Build Shared
echo "Building Shared..."
dotnet build src/IndieFps.Shared/IndieFps.Shared.csproj -c "$CONFIGURATION" --no-restore -p:Version="$VERSION"

# Build Server
echo "Building Server..."
dotnet publish src/IndieFps.Server/IndieFps.Server.csproj -c "$CONFIGURATION" -o "$ARTIFACTS_DIR/server" --no-restore -p:Version="$VERSION" -p:PublishTrimmed=true -p:TrimMode=partial

# Detect OS for client build
OS_NAME=$(uname -s)
case "$OS_NAME" in
    Linux*)
        RUNTIME="linux-x64"
        ;;
    Darwin*)
        RUNTIME="osx-arm64"
        ;;
    CYGWIN*|MINGW*|MSYS*)
        RUNTIME="win-x64"
        ;;
    *)
        echo "Unknown OS: $OS_NAME"
        exit 1
        ;;
esac

echo "Building Client for $RUNTIME..."
dotnet publish src/IndieFps.Client/IndieFps.Client.csproj -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -o "$ARTIFACTS_DIR/client/$RUNTIME" --no-restore -p:Version="$VERSION"

# Run tests
echo "Running tests..."
dotnet test "$SOLUTION" -c "$CONFIGURATION" --no-build --verbosity normal

echo "=========================================="
echo "Build completed successfully!"
echo "Artifacts in: $ARTIFACTS_DIR/"
echo "=========================================="

# List artifacts
find "$ARTIFACTS_DIR" -type f -name "*.exe" -o -name "*.dll" -o -name "IndieFps*" | head -20