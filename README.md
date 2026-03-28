[![Русский](https://img.shields.io/badge/lang-Русский-blue)](README.ru.md)

# CityEdit

CityEdit is a cross-platform profile editor for Subway Surfers City. It decrypts, parses, modifies, and re-encrypts the game's local save file, allowing full control over surfers, boards, skins, wallet, statistics, purchases, and season pass state.

The application runs on both desktop (Windows, Linux, macOS) and Android. On Android, it uses Shizuku to access the game's protected `Android/data` directory without root.

**[Download latest release](https://github.com/cataIystdev/cityEdit/releases/latest)**

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Profile File Format](#profile-file-format)
- [Encryption Details](#encryption-details)
- [Project Structure](#project-structure)
- [Build Instructions](#build-instructions)
- [Running Tests](#running-tests)
- [Checksum Verification](#checksum-verification)
- [Android-Specific Details](#android-specific-details)
- [Usage](#usage)
- [Credits](#credits)
- [License](#license)

## Features

**Surfers:**
- Unlock/lock any surfer
- Set level (1-20) per surfer
- Edit high scores
- Manage individual skin unlock states

**Boards:**
- Unlock/lock any board
- Set level (1-20) per board

**Wallet:**
- Edit currency values (coins, keys, headstarts, score boosters, super sneakers, jetpacks, magnets, mystery boxes, mega headstarts, trophies)

**Statistics:**
- Edit run counts (total, campaign, trial)
- Edit stomp/tarp bounce/bubble bounce/board activation counters
- Set player level and XP

**Purchases:**
- View full purchase history with timestamps
- Add arbitrary purchase entries with custom product IDs and dates
- Remove individual purchase records or entire product entries

**Season Pass:**
- Toggle season pass purchased state
- Set season pass point total

**Quick Actions (bulk operations):**
- Unlock/lock all surfers (with optional level override)
- Unlock/lock all boards (with optional level override)
- Unlock/lock all skins
- Set all currencies to maximum (999,999)
- Set season pass to maximum (99,999 points)

**Android Integration:**
- Direct read/write to game profile via Shizuku (no root required)
- Force-stop game before save to prevent auto-save race conditions
- Launch game directly from editor with fresh profile data
- Filesystem sync after write to guarantee data persistence

## Architecture

CityEdit follows the MVVM (Model-View-ViewModel) pattern using Avalonia UI and CommunityToolkit.Mvvm.

```
+------------------+     +-------------------+     +----------------+
|  Views (AXAML)   |<--->|  ViewModels (C#)  |<--->| ProfileService |
+------------------+     +-------------------+     +----------------+
                                                          |
                                                   +------+------+
                                                   |             |
                                            +------+--+   +-----+-------+
                                            | Crypto  |   | GameData    |
                                            | (AES)   |   | (Databases) |
                                            +---------+   +-------------+
```

- **Views** define the UI layout in AXAML with code-behind for platform-specific logic.
- **ViewModels** contain all business logic, data binding properties, and commands.
- **ProfileService** manages the in-memory profile state (a `Dictionary<string, JsonElement>`), handling all read/write/update operations on the parsed JSON structure.
- **Crypto** handles AES-128-CTR encryption and decryption of the binary profile file.
- **GameData** contains static databases of all known surfer IDs, board IDs, skin IDs, wallet tags, and purchase product IDs, mapped to human-readable display names.
- **IFileAccessService** abstracts platform-specific file I/O. Desktop uses direct filesystem access. Android uses Shizuku shell commands for privileged access.

## Profile File Format

The game stores its save data at:

| Platform | Path |
|----------|------|
| Android  | `/storage/emulated/0/Android/data/com.sybogames.subway.surfers.game/files/enc/profile` |
| Desktop  | Varies by OS. Typically in the game's local application data directory. |

The file is a binary blob with the following layout:

```
+--------+--------+---------------------------+
| IV     | KEY    | ENCRYPTED_DATA            |
| 16 B   | 16 B   | Variable length           |
+--------+--------+---------------------------+
```

- **IV** (16 bytes): Initialization vector for AES-CTR.
- **KEY** (16 bytes): AES-128 encryption key, stored in plaintext.
- **ENCRYPTED_DATA** (remaining bytes): AES-128-CTR encrypted JSON payload.

The decrypted payload is a JSON object with the following top-level structure:

```json
{
  "version": 1,
  "lastUpdated": 1711632000000,
  "profile": "<nested JSON string>",
  "hash": "..."
}
```

The `profile` field contains a JSON string (not an object) that, when parsed, yields the actual game data: `surferProfiles`, `boardProfiles`, `surferSkinProfiles`, `wallet`, `purchaseHistory`, `seasonPassPurchased`, `seasonPassPoints`, and various statistics fields.

## Encryption Details

The game uses AES-128 in CTR (Counter) mode. The implementation specifics:

1. The counter block is initialized from the IV (16 bytes).
2. For each 16-byte block of data, the counter is encrypted using AES-ECB to produce a keystream block.
3. The keystream block is XOR'd with the data block to produce ciphertext (or plaintext, since CTR is symmetric).
4. The counter is incremented from byte 15 down to byte 8 (big-endian, lower 8 bytes only).

The key and IV are stored in plaintext at the beginning of the file. There is no key derivation or password protection; the encryption is obfuscation-grade, not security-grade.

## Project Structure

```
CityEdit/
  CityEdit.slnx                    Solution file (SLNX format)
  src/
    CityEdit/                       Shared library and desktop entry point
      Core/
        Constants.cs                App-wide constants (version, crypto params, limits)
      Crypto/
        AesCtrCipher.cs             AES-128-CTR implementation
        ProfileCrypto.cs            Profile file encrypt/decrypt (binary <-> JSON)
      Models/
        GameData/
          SurferDatabase.cs         Surfer ID -> name mapping (all known surfers)
          BoardDatabase.cs          Board ID -> name mapping with owner surfer info
          SkinDatabase.cs           Skin ID -> name mapping with owning surfer info
          WalletTags.cs             Currency DataTag -> display name mapping
          PurchaseDatabase.cs       Known purchasable product ID database
      Services/
        IFileAccessService.cs       Platform-agnostic file access interface
        DesktopFileAccessService.cs  Desktop implementation (direct File.ReadAllBytes)
        ProfileService.cs           In-memory profile state manager
      ViewModels/
        MainWindowViewModel.cs      Primary ViewModel: load, save, edit, launch
        Items/
          ItemViewModels.cs         Individual item VMs (surfer, board, skin, wallet, stat, purchase)
      Views/
        MainWindow.axaml(.cs)       Desktop main window with sidebar navigation
        MobileMainView.axaml(.cs)   Android main view with horizontal tab navigation
        ShizukuStatusView.axaml(.cs) Shizuku permission/status overlay
        QuickActionsView.axaml(.cs)  Bulk operations panel
        SurfersView.axaml(.cs)       Surfer list with skin sub-items
        BoardsView.axaml(.cs)        Board list
        StatsView.axaml(.cs)         Statistics editor
        WalletView.axaml(.cs)        Currency editor
        PurchasesView.axaml(.cs)     Purchase history editor
        SeasonPassView.axaml(.cs)    Season pass editor
      Converters/
        HalfWidthConverter.cs       Adaptive width converter for skin grid layout
      Styles/
        AppTheme.axaml              Custom dark theme and control styles
      App.axaml(.cs)                Application entry point and lifetime management
      Program.cs                    Desktop program entry point
      CityEdit.csproj               Multi-target project (net10.0 + net10.0-android)
    CityEdit.Android/               Android host project
      MainActivity.cs               Activity with Avalonia integration and GPU config
      Services/
        AndroidFileAccessService.cs  Shizuku-based file access, game process management
      Java/
        ShizukuBridge.cs             .NET-to-Shizuku interop (reflection-based shell exec)
      Libs/
        shizuku-api.aar             Shizuku API bindings
        shizuku-provider.aar        Shizuku content provider
        shizuku-shared.aar          Shizuku shared utilities
        shizuku-aidl.aar            Shizuku AIDL interfaces
      Resources/                    Android resources (layouts, drawables, values)
      AndroidManifest.xml           App manifest with Shizuku provider declaration
      CityEdit.Android.csproj       Android-specific project file
  tests/
    CityEdit.Tests/
      CryptoTests.cs                AES-CTR and ProfileCrypto unit/integration tests
      CityEdit.Tests.csproj         Test project (xUnit)
```

## Build Instructions

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- For Android builds: Android SDK with API level 24+ (installed via `dotnet workload install android`)

### Desktop Build

```bash
# Debug build
dotnet build src/CityEdit/CityEdit.csproj -f net10.0

# Release build
dotnet publish src/CityEdit/CityEdit.csproj -f net10.0 -c Release

# Run directly
dotnet run --project src/CityEdit/CityEdit.csproj -f net10.0
```

### Android Build

```bash
# Install Android workload (one-time setup)
dotnet workload install android

# Build signed APK
dotnet publish src/CityEdit.Android/CityEdit.Android.csproj -f net10.0-android -c Release
```

The signed APK will be at:
```
src/CityEdit.Android/bin/Release/net10.0-android/com.catalyst.cityedit-Signed.apk
```

### Install on Device

```bash
adb install -r src/CityEdit.Android/bin/Release/net10.0-android/com.catalyst.cityedit-Signed.apk
```

## Running Tests

```bash
dotnet test tests/CityEdit.Tests/CityEdit.Tests.csproj
```

The crypto tests include both unit tests (AES-CTR round-trip, partial blocks, invalid inputs) and integration tests that operate on a real `profile` file if one is present in the repository root. Integration tests are silently skipped when no profile file is found.

## Checksum Verification

After building a release APK, you can verify its integrity:

```bash
# SHA-256
sha256sum src/CityEdit.Android/bin/Release/net10.0-android/com.catalyst.cityedit-Signed.apk

# MD5
md5sum src/CityEdit.Android/bin/Release/net10.0-android/com.catalyst.cityedit-Signed.apk
```

For desktop builds:

```bash
# Linux/macOS
sha256sum src/CityEdit/bin/Release/net10.0/publish/CityEdit

# Windows (PowerShell)
Get-FileHash src\CityEdit\bin\Release\net10.0\publish\CityEdit.exe -Algorithm SHA256
```

Compare the output against checksums published in the release notes to verify the binary has not been tampered with.

## Android-Specific Details

### Shizuku Requirement

On Android, the game stores its profile in `/storage/emulated/0/Android/data/`, which is inaccessible to third-party apps since Android 11. CityEdit uses [Shizuku](https://shizuku.rikka.app/) to execute shell commands with elevated privileges, bypassing this restriction without requiring root.

Shizuku must be running and authorized before CityEdit can load a profile. The app automatically detects Shizuku status and prompts the user if it is unavailable or unauthorized.

### Game Process Management

When saving or launching the game, CityEdit performs the following sequence to prevent data loss:

1. **Force-stop the game** (`am force-stop`) -- ensures the game process is terminated and cannot auto-save its state over the editor's changes.
2. **Wait 500ms** -- guarantees the process is fully terminated.
3. **Write the profile** -- serializes the in-memory state, encrypts it, writes via Shizuku shell (`base64` + `cp`), and calls `sync` to flush filesystem buffers.
4. **Launch the game** -- starts the game via `monkey` command, which bypasses the `FLAG_STOPPED` state that `force-stop` sets, avoiding cold-start delays.

### GPU Rendering

The Android build is configured to use Vulkan as the primary rendering backend with EGL as fallback. This eliminates scroll jank on high-refresh-rate displays (90/120Hz).

## Usage

### Desktop

1. Run the application.
2. Click "Open Profile" and select the `profile` file from the game's data directory.
3. Edit any values using the tabbed interface (Quick Actions, Surfers, Boards, Stats, Wallet, Purchases, Season Pass).
4. Click "Save" to write changes back to the same file, or "Save As" to export to a different location.

### Android

1. Install Shizuku and start it (via Wireless Debugging or root).
2. Open CityEdit. It will automatically request Shizuku authorization.
3. Once authorized, the profile loads automatically from the game's data directory.
4. Edit values as needed.
5. Press "Save" to write changes to the game's profile file.
6. Press "Launch Game" to force-stop the game, write the latest profile, and start the game with your changes applied.

## Credits

**Author:** CatalystDev
**Contact:** catalyst@raitokyokai.tech

## License

This project is licensed under the [MIT License](LICENSE).
