#!/bin/bash

# Debug Production App
# Opens the installed app with remote debugging enabled

echo "========================================="
echo "Hive - Production App Debugger"
echo "========================================="
echo ""

APP_PATH="/Applications/Hive.app"

if [ ! -d "$APP_PATH" ]; then
    echo "Error: Hive.app not found at $APP_PATH"
    echo "Build and install the app first: make electron-build-mac"
    exit 1
fi

echo "Starting Hive.app with remote debugging..."
echo ""
echo "To open DevTools:"
echo "  1. The app window should open"
echo "  2. Press: Cmd + Option + I"
echo "  3. Or right-click anywhere and select 'Inspect Element'"
echo ""
echo "Check logs at:"
echo "  ~/Library/Application Support/hive-desktop/hive-debug.log"
echo ""
echo "Press Ctrl+C to stop the app"
echo ""

# Kill any existing instances
pkill -f "Hive.app" 2>/dev/null
sleep 1

# Launch with environment variables for debugging
ELECTRON_ENABLE_LOGGING=1 \
ELECTRON_DEBUG=1 \
  open -a "$APP_PATH" \
  --stderr /tmp/hive-stderr.log \
  --stdout /tmp/hive-stdout.log

# Wait a moment for the app to start
sleep 3

# Show backend status
echo "Backend status:"
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "  ✓ Backend is running on port 5000"

    if curl -s http://localhost:5000/health > /dev/null 2>&1; then
        echo "  ✓ Health endpoint responding"
        echo "    Response: $(curl -s http://localhost:5000/health)"
    else
        echo "  ✗ Health endpoint not responding"
    fi
else
    echo "  ✗ Backend not running on port 5000"
    echo "    The embedded backend should start automatically"
fi

echo ""
echo "Recent logs:"
tail -5 ~/Library/Application\ Support/hive-desktop/hive-debug.log 2>/dev/null || echo "  No logs yet"

echo ""
echo "To view live logs:"
echo "  tail -f ~/Library/Application\ Support/hive-desktop/hive-debug.log"
echo ""
echo "App is running. Check the window and press Cmd+Option+I for DevTools."
