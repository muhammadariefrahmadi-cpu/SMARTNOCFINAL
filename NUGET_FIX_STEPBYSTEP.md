# ? CARA FIX NUGET ERROR - STEP BY STEP

## ?? APA YANG HARUS ANDA LAKUKAN SEKARANG

### STEP 1: Close Visual Studio
- Tutup semua window Visual Studio
- Make sure semua process closed

### STEP 2: Clear NuGet Cache
Buka **Command Prompt** atau **PowerShell** (run as admin):

```powershell
dotnet nuget locals all --clear
```

Tunggu hingga selesai (output: `Local resources cleared`)

### STEP 3: Delete Temporary Folders
```
Delete these folders:
- C:\Users\[YourUsername]\.nuget\packages\communitytools*
- C:\Users\[YourUsername]\.nuget\packages\serilog*
- C:\Users\[YourUsername]\.nuget\packages\fluentvalidation*
- C:\Users\[YourUsername]\.nuget\packages\mediatr*
- C:\Users\[YourUsername]\.nuget\packages\automapper*
- C:\Users\[YourUsername]\.nuget\packages\polly*
```

Ganti `[YourUsername]` dengan username Windows Anda.

### STEP 4: Delete Project Build Artifacts
```
Delete:
- SMART_NOC\bin folder
- SMART_NOC\obj folder
```

### STEP 5: Reopen Visual Studio

### STEP 6: Install Packages ONE BY ONE

Copy-paste these commands ke Package Manager Console satu per satu (jangan semuanya sekaligus):

```powershell
Install-Package Serilog -Version 4.0.0
```
Tunggu selesai ?

```powershell
Install-Package Serilog.Sinks.File -Version 6.0.0
```
Tunggu selesai ?

```powershell
Install-Package FluentValidation -Version 11.8.1
```
Tunggu selesai ?

```powershell
Install-Package MediatR -Version 12.1.1
```
Tunggu selesai ?

```powershell
Install-Package AutoMapper -Version 13.0.1
```
Tunggu selesai ?

```powershell
Install-Package Polly -Version 8.2.0
```
Tunggu selesai ?

```powershell
Install-Package CommunityToolkit.WinUI.UI -Version 8.2.251219
```
Tunggu selesai ?

```powershell
Install-Package CommunityToolkit.WinUI.Controls -Version 8.2.251219
```
Tunggu selesai ?

### STEP 7: Build Project
```
Build ? Clean Solution
Build ? Rebuild Solution
```

Tunggu hingga selesai (Build Succeeded?)

### STEP 8: Verify
Check Package Manager Console:
```
Get-Package
```

Seharusnya ada 8 package baru yang terinstall.

---

## ? JIKA MASIH ERROR

### OPTION A: Use Older but Stable Versions

```powershell
Install-Package Serilog -Version 3.1.1
Install-Package Serilog.Sinks.File -Version 5.0.0
Install-Package FluentValidation -Version 11.8.0
Install-Package MediatR -Version 12.0.1
Install-Package AutoMapper -Version 13.0.0
Install-Package Polly -Version 8.1.0
Install-Package CommunityToolkit.WinUI.UI -Version 8.1.240611
Install-Package CommunityToolkit.WinUI.Controls -Version 8.1.240611
```

### OPTION B: Use .NET CLI Instead

Close Visual Studio dan run di Command Prompt:

```powershell
cd C:\Users\arief\source\repos\SMART_NOC\SMART_NOC
dotnet add package Serilog --version 4.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0
dotnet add package FluentValidation --version 11.8.1
dotnet add package MediatR --version 12.1.1
dotnet add package AutoMapper --version 13.0.1
dotnet add package Polly --version 8.2.0
dotnet add package CommunityToolkit.WinUI.UI --version 8.2.251219
dotnet add package CommunityToolkit.WinUI.Controls --version 8.2.251219
```

---

## ?? SUMMARY

| Step | Action | Time |
|------|--------|------|
| 1 | Close VS | 1 min |
| 2 | Clear cache | 2 min |
| 3 | Delete folders | 2 min |
| 4 | Delete build artifacts | 1 min |
| 5 | Reopen VS | 2 min |
| 6 | Install 8 packages | 15 min |
| 7 | Build project | 5 min |
| 8 | Verify | 1 min |
| **TOTAL** | | **~29 min** |

---

## ?? REPORT FORMAT

After completion, report with this format:

```
STEP 1: ? Closed Visual Studio
STEP 2: ? Cleared NuGet cache
STEP 3: ? Deleted cache folders
STEP 4: ? Deleted build artifacts
STEP 5: ? Reopened Visual Studio
STEP 6: ? Installed 8 packages:
  - Serilog: ?
  - Serilog.Sinks.File: ?
  - FluentValidation: ?
  - MediatR: ?
  - AutoMapper: ?
  - Polly: ?
  - CommunityToolkit.WinUI.UI: ?
  - CommunityToolkit.WinUI.Controls: ?
STEP 7: ? Build successful
STEP 8: ? Verified packages

RESULT: ? ALL PACKAGES INSTALLED SUCCESSFULLY
READY FOR CODE GENERATION: YES
```

---

**Start with STEP 1 now!** ??

