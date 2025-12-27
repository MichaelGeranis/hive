#!/bin/bash
set -e

# Build script for Hive backend
# Creates self-contained executables for macOS and Windows

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT="$PROJECT_ROOT/src/Hive.Api/Hive.Api.csproj"
OUTPUT_DIR="$PROJECT_ROOT/src/Hive.Desktop/backend"

echo "Building Hive backend..."
echo "Project: $API_PROJECT"
echo "Output: $OUTPUT_DIR"

# Clean previous builds
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build for the current platform by default
if [[ "$1" == "all" ]]; then
    TARGETS=("osx-arm64" "osx-x64" "win-x64")
elif [[ -n "$1" ]]; then
    TARGETS=("$1")
elif [[ "$(uname)" == "Darwin" ]]; then
    if [[ "$(uname -m)" == "arm64" ]]; then
        TARGETS=("osx-arm64")
    else
        TARGETS=("osx-x64")
    fi
else
    TARGETS=("win-x64")
fi

for RID in "${TARGETS[@]}"; do
    echo ""
    echo "=== Building for $RID ==="

    TARGET_DIR="$OUTPUT_DIR/$RID"

    dotnet publish "$API_PROJECT" \
        --configuration Release \
        --runtime "$RID" \
        --self-contained true \
        --output "$TARGET_DIR" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true

    echo "Built: $TARGET_DIR"

    # List the output
    if [[ "$RID" == win-* ]]; then
        ls -lh "$TARGET_DIR/Hive.Api.exe" 2>/dev/null || ls -lh "$TARGET_DIR"
    else
        ls -lh "$TARGET_DIR/Hive.Api" 2>/dev/null || ls -lh "$TARGET_DIR"
    fi
done

echo ""
echo "Build complete!"
