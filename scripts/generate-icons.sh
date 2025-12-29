#!/bin/bash

# Generate app icons from SVG source
# Prerequisites:
#   - macOS: brew install librsvg imagemagick
#   - Linux: apt-get install librsvg2-bin imagemagick
#   - Windows: Install ImageMagick

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
ASSETS_DIR="$PROJECT_ROOT/src/Hive.Desktop/src/assets"
SVG_SOURCE="$ASSETS_DIR/icon.svg"

echo "Generating icons from: $SVG_SOURCE"

# Check if SVG exists
if [ ! -f "$SVG_SOURCE" ]; then
    echo "Error: SVG source not found at $SVG_SOURCE"
    exit 1
fi

# Create temp directory for icon generation
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

echo "Creating PNG files..."

# Generate PNG files at various sizes
SIZES=(16 32 64 128 256 512 1024)
for size in "${SIZES[@]}"; do
    if command -v rsvg-convert &> /dev/null; then
        rsvg-convert -w $size -h $size "$SVG_SOURCE" > "$TEMP_DIR/icon_${size}x${size}.png"
    elif command -v convert &> /dev/null; then
        convert -background none -resize ${size}x${size} "$SVG_SOURCE" "$TEMP_DIR/icon_${size}x${size}.png"
    else
        echo "Error: Neither rsvg-convert nor ImageMagick convert found"
        exit 1
    fi
    echo "  Created ${size}x${size} PNG"
done

# Copy 512x512 as the main Linux icon
cp "$TEMP_DIR/icon_512x512.png" "$ASSETS_DIR/icon.png"
echo "Created: icon.png (512x512)"

# Generate ICO for Windows (multiple sizes embedded)
if command -v convert &> /dev/null; then
    convert "$TEMP_DIR/icon_16x16.png" \
            "$TEMP_DIR/icon_32x32.png" \
            "$TEMP_DIR/icon_64x64.png" \
            "$TEMP_DIR/icon_128x128.png" \
            "$TEMP_DIR/icon_256x256.png" \
            "$ASSETS_DIR/icon.ico"
    echo "Created: icon.ico (Windows)"
else
    echo "Warning: ImageMagick not found, skipping ICO generation"
fi

# Generate ICNS for macOS
if [[ "$OSTYPE" == "darwin"* ]]; then
    ICONSET_DIR="$TEMP_DIR/icon.iconset"
    mkdir -p "$ICONSET_DIR"

    # Create iconset with required sizes
    cp "$TEMP_DIR/icon_16x16.png" "$ICONSET_DIR/icon_16x16.png"
    cp "$TEMP_DIR/icon_32x32.png" "$ICONSET_DIR/icon_16x16@2x.png"
    cp "$TEMP_DIR/icon_32x32.png" "$ICONSET_DIR/icon_32x32.png"
    cp "$TEMP_DIR/icon_64x64.png" "$ICONSET_DIR/icon_32x32@2x.png"
    cp "$TEMP_DIR/icon_128x128.png" "$ICONSET_DIR/icon_128x128.png"
    cp "$TEMP_DIR/icon_256x256.png" "$ICONSET_DIR/icon_128x128@2x.png"
    cp "$TEMP_DIR/icon_256x256.png" "$ICONSET_DIR/icon_256x256.png"
    cp "$TEMP_DIR/icon_512x512.png" "$ICONSET_DIR/icon_256x256@2x.png"
    cp "$TEMP_DIR/icon_512x512.png" "$ICONSET_DIR/icon_512x512.png"
    cp "$TEMP_DIR/icon_1024x1024.png" "$ICONSET_DIR/icon_512x512@2x.png"

    iconutil -c icns -o "$ASSETS_DIR/icon.icns" "$ICONSET_DIR"
    echo "Created: icon.icns (macOS)"
else
    echo "Note: ICNS generation requires macOS. Run this script on a Mac to generate icon.icns"
fi

echo ""
echo "Icon generation complete!"
echo "Generated files in: $ASSETS_DIR"
ls -la "$ASSETS_DIR"
