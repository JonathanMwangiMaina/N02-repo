#!/bin/bash
# build.sh - Cross-platform build script for BlackoutClause FPS
# Usage: ./build.sh [Debug|Release] [version]
# Run from games/blackoutclause-fps/ directory

set -euo pipefail

CONFIGURATION="${1:-Release}"
VERSION="${2:-1.0.0}"
SOLUTION="BlackoutClause.sln"
ARTIFACTS_DIR="artifacts"

echo "=========================================="
echo "Building BlackoutClause FPS - $CONFIGURATION v$VERSION"
echo "=========================================="

# Clean previous builds
rm -rf "$ARTIFACTS_DIR"
mkdir -p "$ARTIFACTS_DIR"

# Restore
echo "Restoring packages..."
dotnet restore "$SOLUTION"

# Build Shared
echo "Building Shared..."
dotnet build src/BlackoutClause.Shared/BlackoutClause.Shared.csproj -c "$CONFIGURATION" --no-restore -p:Version="$VERSION"

# Build Server (NO TRIMMING - Godot uses reflection)
echo "Building Server..."
dotnet publish src/BlackoutClause.Server/BlackoutClause.Server.csproj -c "$CONFIGURATION" -o "$ARTIFACTS_DIR/server" --no-restore -p:Version="$VERSION" -p:PublishTrimmed=false

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
dotnet publish src/BlackoutClause.Client/BlackoutClause.Client.csproj -c "$CONFIGURATION" -r "$RUNTIME" --self-contained -o "$ARTIFACTS_DIR/client/$RUNTIME" --no-restore -p:Version="$VERSION"

# Run tests
echo "Running tests..."
dotnet test "$SOLUTION" -c "$CONFIGURATION" --no-build --verbosity normal

echo "=========================================="
echo "Build completed successfully!"
echo "Artifacts in: $ARTIFACTS_DIR/"
echo "=========================================="

# List artifacts
find "$ARTIFACTS_DIR" -type f \( -name "*.exe" -o -name "*.dll" -o -name "BlackoutClause*" \) | head -20