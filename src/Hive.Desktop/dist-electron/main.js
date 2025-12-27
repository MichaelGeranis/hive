"use strict";
const electron = require("electron");
const path = require("path");
const child_process = require("child_process");
const http = require("http");
function _interopNamespaceDefault(e) {
  const n = Object.create(null, { [Symbol.toStringTag]: { value: "Module" } });
  if (e) {
    for (const k in e) {
      if (k !== "default") {
        const d = Object.getOwnPropertyDescriptor(e, k);
        Object.defineProperty(n, k, d.get ? d : {
          enumerable: true,
          get: () => e[k]
        });
      }
    }
  }
  n.default = e;
  return Object.freeze(n);
}
const path__namespace = /* @__PURE__ */ _interopNamespaceDefault(path);
const http__namespace = /* @__PURE__ */ _interopNamespaceDefault(http);
const BACKEND_PORT = 5e3;
const HEALTH_CHECK_URL = `http://localhost:${BACKEND_PORT}/health`;
const HEALTH_CHECK_INTERVAL = 500;
const HEALTH_CHECK_TIMEOUT = 3e4;
let backendProcess = null;
let isShuttingDown = false;
function getBackendPath() {
  const isDev2 = process.env.NODE_ENV === "development" || !electron.app.isPackaged;
  if (isDev2) {
    return "";
  }
  const platform = process.platform;
  const arch = process.arch;
  let rid;
  if (platform === "darwin") {
    rid = arch === "arm64" ? "osx-arm64" : "osx-x64";
  } else if (platform === "win32") {
    rid = "win-x64";
  } else {
    rid = "linux-x64";
  }
  const executableName = platform === "win32" ? "Hive.Api.exe" : "Hive.Api";
  const resourcesPath = process.resourcesPath;
  return path__namespace.join(resourcesPath, "backend", rid, executableName);
}
function checkHealth() {
  return new Promise((resolve) => {
    const req = http__namespace.get(HEALTH_CHECK_URL, (res) => {
      resolve(res.statusCode === 200);
    });
    req.on("error", () => {
      resolve(false);
    });
    req.setTimeout(2e3, () => {
      req.destroy();
      resolve(false);
    });
  });
}
async function waitForHealth() {
  const startTime = Date.now();
  while (Date.now() - startTime < HEALTH_CHECK_TIMEOUT) {
    if (await checkHealth()) {
      return true;
    }
    await new Promise((resolve) => setTimeout(resolve, HEALTH_CHECK_INTERVAL));
  }
  return false;
}
async function startBackend() {
  var _a, _b;
  const backendPath = getBackendPath();
  if (!backendPath) {
    console.log("Development mode: skipping backend spawn");
    const isHealthy2 = await waitForHealth();
    if (!isHealthy2) {
      console.warn("Backend health check failed - ensure backend is running on port 5000");
    }
    return;
  }
  console.log(`Starting backend from: ${backendPath}`);
  const env = {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: "Production",
    UseInMemoryDatabase: "false",
    ASPNETCORE_URLS: `http://localhost:${BACKEND_PORT}`
  };
  backendProcess = child_process.spawn(backendPath, [], {
    env,
    stdio: ["ignore", "pipe", "pipe"],
    windowsHide: true
  });
  (_a = backendProcess.stdout) == null ? void 0 : _a.on("data", (data) => {
    console.log(`[Backend] ${data.toString().trim()}`);
  });
  (_b = backendProcess.stderr) == null ? void 0 : _b.on("data", (data) => {
    console.error(`[Backend Error] ${data.toString().trim()}`);
  });
  backendProcess.on("error", (error) => {
    console.error("Failed to start backend:", error);
  });
  backendProcess.on("exit", (code, signal) => {
    if (!isShuttingDown) {
      console.error(`Backend exited unexpectedly with code ${code}, signal ${signal}`);
    }
    backendProcess = null;
  });
  console.log("Waiting for backend to become healthy...");
  const isHealthy = await waitForHealth();
  if (isHealthy) {
    console.log("Backend is healthy and ready");
  } else {
    console.error("Backend failed to become healthy within timeout");
    throw new Error("Backend failed to start");
  }
}
async function stopBackend() {
  if (!backendProcess) {
    return;
  }
  isShuttingDown = true;
  console.log("Stopping backend...");
  return new Promise((resolve) => {
    const timeout = setTimeout(() => {
      console.log("Backend did not exit gracefully, forcing kill");
      backendProcess == null ? void 0 : backendProcess.kill("SIGKILL");
      resolve();
    }, 5e3);
    backendProcess.on("exit", () => {
      clearTimeout(timeout);
      console.log("Backend stopped");
      resolve();
    });
    if (process.platform === "win32") {
      backendProcess.kill();
    } else {
      backendProcess.kill("SIGTERM");
    }
  });
}
const isDev = process.env.NODE_ENV === "development" || !electron.app.isPackaged;
let mainWindow = null;
let backendStarted = false;
function createWindow() {
  mainWindow = new electron.BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 768,
    webPreferences: {
      preload: path__namespace.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false
    },
    titleBarStyle: "hiddenInset",
    trafficLightPosition: { x: 15, y: 15 },
    show: false,
    backgroundColor: "#f8fafc"
  });
  mainWindow.once("ready-to-show", () => {
    mainWindow == null ? void 0 : mainWindow.show();
  });
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    electron.shell.openExternal(url);
    return { action: "deny" };
  });
  if (isDev) {
    mainWindow.loadURL("http://localhost:5173");
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(path__namespace.join(__dirname, "../dist/index.html"));
  }
  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}
electron.app.whenReady().then(async () => {
  try {
    await startBackend();
    backendStarted = true;
  } catch (error) {
    console.error("Failed to start backend:", error);
    if (!isDev) {
      electron.app.quit();
      return;
    }
  }
  createWindow();
  electron.app.on("activate", () => {
    if (electron.BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});
electron.app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    electron.app.quit();
  }
});
electron.app.on("before-quit", async (event) => {
  if (backendStarted) {
    event.preventDefault();
    await stopBackend();
    backendStarted = false;
    electron.app.quit();
  }
});
