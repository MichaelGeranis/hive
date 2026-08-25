# Hive — Debugging

Local debugging setups for the backend and the desktop client.
For architecture see [ARCHITECTURE.md](../ARCHITECTURE.md); for getting the app running at
all see [README.md](../README.md).

Ports: the backend listens on **5002**, the Vite dev server on **5173**.

---

## Backend

### Using Visual Studio Code

1. **Install the C# extension** (C# Dev Kit recommended)

2. **Create launch configuration** (`.vscode/launch.json`):
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": ".NET Core Launch (API)",
         "type": "coreclr",
         "request": "launch",
         "preLaunchTask": "build",
         "program": "${workspaceFolder}/src/Hive.Api/bin/Debug/net9.0/Hive.Api.dll",
         "args": [],
         "cwd": "${workspaceFolder}/src/Hive.Api",
         "stopAtEntry": false,
         "env": {
           "ASPNETCORE_ENVIRONMENT": "Development"
         }
       },
       {
         "name": ".NET Core Attach",
         "type": "coreclr",
         "request": "attach"
       }
     ]
   }
   ```

3. **Create build task** (`.vscode/tasks.json`):
   ```json
   {
     "version": "2.0.0",
     "tasks": [
       {
         "label": "build",
         "command": "dotnet",
         "type": "process",
         "args": [
           "build",
           "${workspaceFolder}/Hive.sln",
           "/property:GenerateFullPaths=true",
           "/consoleloggerparameters:NoSummary"
         ],
         "problemMatcher": "$msCompile"
       }
     ]
   }
   ```

4. **Set breakpoints** and press `F5` to start debugging.

### Using Visual Studio

1. Open `Hive.sln` in Visual Studio
2. Set `Hive.Api` as the startup project
3. Set breakpoints in your code
4. Press `F5` to start debugging

### Using JetBrains Rider

1. Open the solution in Rider
2. Select the `Hive.Api` run configuration
3. Click the Debug button or press `Shift+F9`

### Command-line Debugging

For quick debugging without an IDE:

```bash
# Enable detailed logging
cd src/Hive.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --verbosity detailed
```

### Debugging Tests

```bash
# Debug a specific test
dotnet test --filter "FullyQualifiedName~TestMethodName" --logger "console;verbosity=detailed"
```

## Frontend

### Using Visual Studio Code

1. **Install the recommended extensions**:
   - JavaScript Debugger (built-in)
   - Electron Debug

2. **Create launch configuration** (`.vscode/launch.json`):
   ```json
   {
     "version": "0.2.0",
     "configurations": [
       {
         "name": "Debug Electron Main",
         "type": "node",
         "request": "launch",
         "cwd": "${workspaceFolder}/src/Hive.Desktop",
         "runtimeExecutable": "${workspaceFolder}/src/Hive.Desktop/node_modules/.bin/electron",
         "args": ["."],
         "env": {
           "NODE_ENV": "development"
         }
       },
       {
         "name": "Debug Electron Renderer",
         "type": "chrome",
         "request": "attach",
         "port": 9222,
         "webRoot": "${workspaceFolder}/src/Hive.Desktop/src"
       }
     ]
   }
   ```

### Using Chrome DevTools

1. **Start the app in development mode**
   ```bash
   cd src/Hive.Desktop
   npm run electron:dev
   ```

2. **Open DevTools** in the Electron window:
   - Press `Ctrl+Shift+I` (Windows/Linux) or `Cmd+Option+I` (macOS)
   - Or use the menu: View > Toggle Developer Tools

3. **Debug React components**:
   - Use the Sources tab to set breakpoints
   - Use the React Developer Tools extension for component inspection

### Debugging Network Requests

1. Open Chrome DevTools in the Electron window
2. Go to the **Network** tab
3. Monitor API calls to `http://localhost:5002/api/*`
4. Check request/response payloads and headers

## Common Issues

- **API Connection Issues**: Ensure the backend is running on port 5002
- **CORS Errors**: Check that the API allows requests from localhost:5173
- **Authentication Errors**: Verify Basic Auth credentials in `src/services/api.ts`

- **Backend Not Starting**: Another process may hold port 5002 — run `make kill-backend`.
- **Empty Data in Development**: In-memory mode starts empty and resets on every restart.
  Run `make backend-sqlite` if you need data to persist between runs.
- **Changes Not Appearing**: The Electron renderer hot-reloads; the .NET backend does not.
  Restart the backend after changing C# code, or use `dotnet watch`.

## Scripts

Two helper scripts in `scripts/` cover the awkward cases:

| Script | Use |
|--------|-----|
| `debug-setup.sh` | Prepare a local debugging environment |
| `debug-prod-app.sh` | Attach to an already-packaged production build |
