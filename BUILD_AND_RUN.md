# How to Build and Run the Twenty One Card Game

## Prerequisites

1. **Install .NET 9 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   - Verify installation:
     ```bash
     dotnet --version
     ```
     Should show version 9.x.x

2. **Code Editor** (optional but recommended)
   - Visual Studio 2022 (17.8 or later)
   - Visual Studio Code with C# extension
   - JetBrains Rider

## Step-by-Step Instructions

### Option 1: Using Command Line (Recommended)

#### Step 1: Open Terminal/Command Prompt

- **Windows**: PowerShell or Command Prompt
- **Mac/Linux**: Terminal

#### Step 2: Navigate to Project Directory

```bash
cd "C:\Users\zgdas\3rd course\ИСП\PROJECT_13"
```

Or if you're already in the project folder, verify you're in the right place:
```bash
dir
# Should see: TwentyOne.sln, TwentyOne.Server, TwentyOne.Client, TwentyOne.Shared folders
```

#### Step 3: Restore NuGet Packages and Build

```bash
dotnet restore
dotnet build
```

This will:
- Download all required NuGet packages
- Build all three projects (Server, Client, Shared)
- Verify there are no compilation errors

**Expected output**: `Build succeeded.`

#### Step 4: Start the Server (Terminal 1)

```bash
cd TwentyOne.Server
dotnet run
```

**Expected output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7000
      Now listening on: http://localhost:5000
```

**Keep this terminal window open!** The server must be running.

#### Step 5: Start the Client (Terminal 2)

Open a **new terminal window** and run:

```bash
cd "C:\Users\zgdas\3rd course\ИСП\PROJECT_13\TwentyOne.Client"
dotnet run
```

**Expected output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

#### Step 6: Open in Browser

1. Open your web browser (Chrome, Edge, Firefox, etc.)
2. Navigate to: `https://localhost:5001`
3. If you see a security warning about the SSL certificate:
   - Click "Advanced" or "Show Details"
   - Click "Proceed to localhost" or "Accept the Risk"
   - This is normal for local development

#### Step 7: Play the Game!

1. Enter your player name
2. Enter or generate a Room ID
3. Click "Join Game"
4. Wait for a second player to join (or open another browser tab/window)
5. Once both players join, the game starts automatically!

### Option 2: Using Visual Studio

1. **Open the Solution**
   - Double-click `TwentyOne.sln` or open it from Visual Studio

2. **Set Multiple Startup Projects**
   - Right-click the solution in Solution Explorer
   - Select "Properties" or "Configure Startup Projects"
   - Select "Multiple startup projects"
   - Set both `TwentyOne.Server` and `TwentyOne.Client` to "Start"
   - Click OK

3. **Build the Solution**
   - Press `Ctrl+Shift+B` or go to Build → Build Solution

4. **Run the Solution**
   - Press `F5` or click the green "Start" button
   - Both projects will start automatically
   - Browser should open automatically to the client URL

### Option 3: Using Visual Studio Code

1. **Open the Folder**
   - File → Open Folder → Select the project directory

2. **Open Terminal in VS Code**
   - Terminal → New Terminal (or `Ctrl+``)

3. **Run Server** (Terminal 1):
   ```bash
   dotnet run --project TwentyOne.Server
   ```

4. **Run Client** (Terminal 2):
   - Terminal → New Terminal
   ```bash
   dotnet run --project TwentyOne.Client
   ```

5. **Open Browser**
   - Navigate to `https://localhost:5001`

## Quick Test: Verify Everything Works

### Test 1: Build Test
```bash
dotnet build
```
Should show: `Build succeeded.` with 0 errors, 0 warnings

### Test 2: Server Test
```bash
cd TwentyOne.Server
dotnet run
```
Then open browser to `https://localhost:7000` - should see: "Twenty One Game Server is running."

### Test 3: Client Test
```bash
cd TwentyOne.Client
dotnet run
```
Then open browser to `https://localhost:5001` - should see the game interface

## Troubleshooting

### Problem: "dotnet command not found"
**Solution**: Install .NET 9 SDK from https://dotnet.microsoft.com/download

### Problem: Port already in use
**Error**: `Failed to bind to address https://localhost:7000`

**Solution**: 
1. Find what's using the port:
   ```bash
   # Windows
   netstat -ano | findstr :7000
   
   # Mac/Linux
   lsof -i :7000
   ```
2. Kill the process or change ports in `Properties/launchSettings.json`

### Problem: CORS errors in browser console
**Error**: `Access to fetch at 'https://localhost:7000' from origin 'https://localhost:5001' has been blocked by CORS policy`

**Solution**: 
1. Make sure server is running before client
2. Check that server `Program.cs` has CORS configured for client URL
3. Verify ports match in `launchSettings.json`

### Problem: "Unable to connect to server"
**Solution**:
1. Verify server is running (check Terminal 1)
2. Check server URL in `GameClientService.cs` matches server port
3. Try `http://localhost:5000` instead of `https://localhost:7000`

### Problem: SSL Certificate Error
**Solution**: 
- Click "Advanced" → "Proceed to localhost"
- Or trust the development certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

### Problem: Build Errors
**Solution**:
1. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```
2. Delete `bin` and `obj` folders, then rebuild
3. Check that all projects reference `TwentyOne.Shared`

## Running Multiple Game Instances

To test with 2 players:

1. **Method 1: Multiple Browser Tabs**
   - Open `https://localhost:5001` in two tabs
   - Use different player names
   - Use the same Room ID

2. **Method 2: Different Browsers**
   - Chrome: `https://localhost:5001`
   - Edge: `https://localhost:5001`
   - Use same Room ID

3. **Method 3: Incognito/Private Mode**
   - Open one normal window and one incognito window
   - Both can connect to the same room

## Development Tips

### Hot Reload
- Server: Changes require restart (`Ctrl+C` then `dotnet run` again)
- Client: Blazor WebAssembly supports hot reload - changes to `.razor` files may auto-refresh

### Debugging
- **Server**: Set breakpoints in `GameHub.cs` or `TwentyOneGame.cs`
- **Client**: Set breakpoints in `GameClientService.cs` or `.razor` files
- Use Visual Studio debugger (F5) or VS Code debugger

### Viewing Logs
- Server logs appear in the terminal where you ran `dotnet run`
- Client logs appear in browser Developer Tools (F12 → Console)

## Production Build

To create production-ready builds:

```bash
# Build Server
cd TwentyOne.Server
dotnet publish -c Release -o ./publish

# Build Client
cd ../TwentyOne.Client
dotnet publish -c Release -o ./publish
```

## Summary

**Quick Start (3 commands):**
```bash
# Terminal 1
cd TwentyOne.Server && dotnet run

# Terminal 2  
cd TwentyOne.Client && dotnet run

# Browser
# Open https://localhost:5001
```

That's it! The game should be running and ready to play.

