#!/bin/bash

# Hive Setup & Debug Script
# Run this to check your development environment

set -e

echo "========================================="
echo "Hive - Setup Diagnostic"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

check_command() {
    if command -v $1 &> /dev/null; then
        echo -e "${GREEN}✓${NC} $1: $(command -v $1)"
        if [ ! -z "$2" ]; then
            echo "  Version: $($1 $2 2>&1 | head -1)"
        fi
        return 0
    else
        echo -e "${RED}✗${NC} $1: Not found"
        return 1
    fi
}

check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1; then
        local process=$(lsof -Pi :$1 -sTCP:LISTEN | tail -n +2)
        echo -e "${YELLOW}⚠${NC}  Port $1: IN USE"
        echo "$process"
        return 1
    else
        echo -e "${GREEN}✓${NC} Port $1: Available"
        return 0
    fi
}

echo "1. Checking Prerequisites"
echo "-------------------------"
check_command dotnet "--version"
check_command node "--version"
check_command npm "--version"
echo ""

echo "2. Checking Port Availability"
echo "-----------------------------"
PORT_5000_OK=0
PORT_5173_OK=0

if ! check_port 5000; then
    PORT_5000_OK=1
    echo -e "${YELLOW}   → This is usually macOS AirPlay Receiver${NC}"
    echo -e "${YELLOW}   → Disable it in: System Settings → General → AirDrop & Handoff${NC}"
fi

check_port 5173 || PORT_5173_OK=1
echo ""

echo "3. Checking Project Structure"
echo "-----------------------------"
if [ -f "Hive.sln" ]; then
    echo -e "${GREEN}✓${NC} Solution file found"
else
    echo -e "${RED}✗${NC} Hive.sln not found - run from project root"
fi

if [ -f "src/Hive.Api/Hive.Api.csproj" ]; then
    echo -e "${GREEN}✓${NC} Backend project found"
else
    echo -e "${RED}✗${NC} Backend project not found"
fi

if [ -f "src/Hive.Desktop/package.json" ]; then
    echo -e "${GREEN}✓${NC} Frontend project found"
else
    echo -e "${RED}✗${NC} Frontend project not found"
fi
echo ""

echo "4. Checking Database"
echo "-------------------"
DB_PATH="$HOME/Library/Application Support/Hive/hive.db"
if [ -f "$DB_PATH" ]; then
    echo -e "${GREEN}✓${NC} Database exists: $DB_PATH"
    echo "  Size: $(du -h "$DB_PATH" | cut -f1)"
    echo "  Modified: $(stat -f "%Sm" "$DB_PATH")"
else
    echo -e "${YELLOW}⚠${NC}  Database not found (will be created on first run)"
    echo "  Expected: $DB_PATH"
fi
echo ""

echo "5. Checking Dependencies"
echo "------------------------"
if [ -d "src/Hive.Desktop/node_modules" ]; then
    echo -e "${GREEN}✓${NC} Node modules installed"
else
    echo -e "${YELLOW}⚠${NC}  Node modules not found - run 'npm install' in src/Hive.Desktop"
fi

if [ -d "src/Hive.Core/bin" ]; then
    echo -e "${GREEN}✓${NC} Backend compiled"
else
    echo -e "${YELLOW}⚠${NC}  Backend not compiled - run 'dotnet build Hive.sln'"
fi
echo ""

echo "6. Testing Backend (if running)"
echo "--------------------------------"
if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo "Backend is running on port 5000"

    # Test health endpoint
    if curl -s http://localhost:5000/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} Health endpoint responding"
        echo "  Response: $(curl -s http://localhost:5000/health)"
    else
        echo -e "${RED}✗${NC} Health endpoint not responding"
    fi

    # Test API with auth
    if curl -s -u admin:admin123 http://localhost:5000/api/directreports > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} API authentication working"
    else
        echo -e "${YELLOW}⚠${NC}  API authentication check failed (might be empty data)"
    fi
else
    echo -e "${YELLOW}⚠${NC}  Backend not running on port 5000"
    echo "  Start with: make backend-sqlite"
fi
echo ""

echo "7. Checking Logs"
echo "----------------"
LOG_PATH="$HOME/Library/Application Support/Hive/hive-debug.log"
if [ -f "$LOG_PATH" ]; then
    echo -e "${GREEN}✓${NC} Debug log exists: $LOG_PATH"
    echo "  Last 5 lines:"
    tail -5 "$LOG_PATH" | sed 's/^/  /'
else
    echo -e "${YELLOW}⚠${NC}  No debug log yet (created when running production app)"
fi
echo ""

echo "========================================="
echo "Summary"
echo "========================================="

ISSUES=0
if [ $PORT_5000_OK -eq 1 ]; then
    echo -e "${RED}✗${NC} Port 5000 is in use - disable AirPlay Receiver"
    ISSUES=$((ISSUES + 1))
fi

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗${NC} .NET SDK not installed"
    ISSUES=$((ISSUES + 1))
fi

if ! command -v node &> /dev/null; then
    echo -e "${RED}✗${NC} Node.js not installed"
    ISSUES=$((ISSUES + 1))
fi

if [ $ISSUES -eq 0 ]; then
    echo -e "${GREEN}✓ Setup looks good!${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Run backend:   make backend-sqlite"
    echo "  2. Run frontend:  make electron-dev"
else
    echo -e "${RED}✗ Found $ISSUES issue(s) - see above${NC}"
    echo ""
    echo "See INSTALLATION.md for detailed setup instructions"
fi
echo ""
