# DW5820e-SMS

A Windows 11 WinUI 3 desktop app that sends and receives SMS messages through the **Dell DW5820e LTE modem** (or any compatible Mobile Broadband device) using the Windows Runtime (WinRT) SMS APIs.

---

## Features

- **Send SMS (MO-SMS)** via the modem using `Windows.Devices.Sms.SmsDevice2`.
- **Receive SMS (MT-SMS)** in real time via `SmsMessageReceivedTriggerDetails`; messages appear in the list automatically.
- **Message history** stored in a local SQLite database (`%LOCALAPPDATA%\DW5820e-SMS\sms.db`).
- **Export** all messages to **CSV** or **JSON** using the system `FileSavePicker`.
- **Multiple account support**: if more than one SIM/account is detected, you can choose which one to use.
- Mica backdrop, fluent design, clear error messages when no modem is found or an API call fails.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Windows 11 (build 22000+) | Required for WinUI 3 and WinRT SMS APIs |
| .NET 8 SDK | <https://dotnet.microsoft.com/download> |
| Windows App SDK 1.8+ | Installed automatically via NuGet |
| DW5820e (or compatible modem) | Mobile data must be enabled; the modem must appear as a Mobile Broadband device in Device Manager |
| App deployed as MSIX package | Required for the `cellularMessaging` / `sms` restricted capabilities |

> **Important – STA threading**  
> WinRT SMS APIs require an STA (Single-Threaded Apartment) COM thread. The WinUI 3 UI thread is STA, so all calls to `SmsService` must originate there (or be marshalled back to it). Never call `.Result` or `.Wait()` on the UI thread.

---

## Building

```powershell
# Restore packages
dotnet restore

# Build (Debug, x64)
dotnet build -c Debug -p:Platform=x64

# Run tests (no modem required)
dotnet test DW5820e-SMS.Tests/DW5820e-SMS.Tests.csproj
```

To create an MSIX package for deployment:

```powershell
dotnet publish DW5820e-SMS/DW5820e-SMS.csproj -c Release -p:Platform=x64
```

Or open `DW5820e-SMS.slnx` in **Visual Studio 2022 v17.8+** and use **Build → Publish**.

---

## Running

Because the app uses the `cellularMessaging` and `sms` restricted capabilities it **must run as a packaged MSIX app** (side-load or store install). Running the raw `.exe` directly will not grant the required capabilities.

1. Build and deploy the MSIX package (Visual Studio: right-click project → *Package and Publish* → *Create App Packages…*).
2. Install the MSIX on the target machine.
3. Launch **DW5820e SMS** from the Start menu.

---

## Architecture

```
DW5820e-SMS/
├── Models/
│   └── SmsMessage.cs          – Data model (Id, PhoneNumber, Body, Direction, Timestamp, Status)
├── Services/
│   ├── ISmsService.cs         – Interface for WinRT SMS operations
│   ├── SmsService.cs          – WinRT implementation (SmsDevice2, MobileBroadbandAccount)
│   ├── IDbService.cs          – Interface for persistence
│   ├── DbService.cs           – SQLite persistence via Microsoft.Data.Sqlite
│   └── ExportService.cs       – CSV + JSON export (static helpers)
├── ViewModels/
│   └── MainViewModel.cs       – Observable state, commands, wires services together
├── MainWindow.xaml/.cs        – Full UI: account picker, message list, compose area, export buttons
├── App.xaml/.cs               – Application entry point
└── Package.appxmanifest       – MSIX manifest with sms/cellularMessaging capabilities

DW5820e-SMS.Tests/
├── DbServiceTests.cs          – Unit tests for SQLite persistence
└── ExportServiceTests.cs      – Unit tests for CSV/JSON export
```

---

## Notes on the DW5820e and PowerShell reference

The original investigation confirmed that the correct WinRT types are under `Windows.Devices.Sms` (not `Windows.Networking.NetworkOperators`). The working PowerShell proof-of-concept used:

```powershell
# Must run Windows PowerShell 5.1 in STA mode:
powershell.exe -STA

[Windows.Networking.NetworkOperators.MobileBroadbandAccount,
 Windows.Networking.NetworkOperators, ContentType=WindowsRuntime]::AvailableNetworkAccountIds

$accountId = (above)[0]
$smsDev = [Windows.Devices.Sms.SmsDevice,
           Windows.Devices.Sms, ContentType=WindowsRuntime]::FromNetworkAccountIdAsync($accountId).GetAwaiter().GetResult()

$msg = New-Object Windows.Devices.Sms.SmsTextMessage
$msg.To = "+1XXXXXXXXXX"
$msg.Body = "Test"
$smsDev.SendMessageAsync($msg).GetAwaiter().GetResult()
```

This app replicates the same API calls using `SmsDevice2` (the modern API) in C# with proper async/await patterns on the STA UI thread.

---

## License

MIT
