# ?? COPY-PASTE READY COMMANDS

## ?? PACKAGE MANAGER CONSOLE COMMANDS

Buka: **Tools ? NuGet Package Manager ? Package Manager Console**

Kemudian pilih salah satu:

---

## ? OPTION A: INSTALL SEMUA SEKALIGUS (RECOMMENDED)

Copy-paste semua command ini ke Package Manager Console:

```powershell
Install-Package CommunityToolkit.WinUI.Controls -Version 8.2.251219; `
Install-Package CommunityToolkit.WinUI.UI -Version 8.2.251219; `
Install-Package Serilog -Version 4.0.0; `
Install-Package Serilog.Sinks.File -Version 6.0.0; `
Install-Package FluentValidation -Version 11.8.1; `
Install-Package MediatR -Version 12.1.1; `
Install-Package AutoMapper -Version 13.0.1; `
Install-Package Polly -Version 8.2.0
```

---

## ? OPTION B: INSTALL ONE BY ONE (SAFE)

Jika ingin lebih aman, run satu per satu:

```powershell
# PHASE 1: Essential
Install-Package CommunityToolkit.WinUI.Controls -Version 8.2.251219
Install-Package CommunityToolkit.WinUI.UI -Version 8.2.251219
Install-Package Serilog -Version 4.0.0
Install-Package Serilog.Sinks.File -Version 6.0.0
Install-Package FluentValidation -Version 11.8.1

# Tunggu semua selesai, kemudian:

# PHASE 2: Recommended
Install-Package MediatR -Version 12.1.1
Install-Package AutoMapper -Version 13.0.1
Install-Package Polly -Version 8.2.0
```

---

## ? OPTION C: EDIT .CSPROJ (FASTEST)

### Langkah-langkah:
1. **Right-click project SMART_NOC** ? Edit Project File
2. **Cari section** `<ItemGroup>` yang ada `<PackageReference Include="CommunityToolkit`
3. **Paste ini** sebelum tag `</ItemGroup>` penutup:

```xml
<PackageReference Include="CommunityToolkit.WinUI.Controls" Version="8.2.251219" />
<PackageReference Include="CommunityToolkit.WinUI.UI" Version="8.2.251219" />
<PackageReference Include="Serilog" Version="4.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
<PackageReference Include="FluentValidation" Version="11.8.1" />
<PackageReference Include="MediatR" Version="12.1.1" />
<PackageReference Include="AutoMapper" Version="13.0.1" />
<PackageReference Include="Polly" Version="8.2.0" />
```

4. **Save file** (Ctrl+S)
5. **Close editor**
6. **Build ? Clean Solution**
7. **Build ? Rebuild Solution**

---

## ?? POST-INSTALLATION STEPS

Setelah install selesai, run ini di Package Manager Console:

```powershell
# 1. Clean solution
Update-Package -Reinstall

# 2. Rebuild dari console
Build-Project

# 3. Verify packages installed
Get-Package
```

---

## ? QUICK SUMMARY

| Item | Details |
|------|---------|
| Total Packages to Install | 8 packages |
| Total Size | ~27 MB |
| Installation Time | 15-20 minutes |
| Complexity | Easy |
| Risk Level | Very Low |
| Benefit | Very High |

---

## ? CHECKLIST SAAT INSTALL

- [ ] Close all open documents in Visual Studio
- [ ] Save all changes (Ctrl+Shift+S)
- [ ] Make sure Internet connection is stable
- [ ] Make sure you have write access to project
- [ ] Have backup of .csproj file

---

## ?? IF SOMETHING GOES WRONG

### Error: "Package Not Found"
```
Solution: Update NuGet to latest version
Tools ? Extensions and Updates ? NuGet Package Manager ? Update
```

### Error: "Dependency Conflict"
```
Solution: Update conflicting package first
Example: Install-Package Newtonsoft.Json -Version 13.0.4
```

### Error: "Project Won't Build"
```
Solution: Clean and rebuild
Build ? Clean Solution
Build ? Rebuild Solution
```

---

## ?? VERIFICATION AFTER INSTALL

Jalankan ini untuk verify semua packages:

```csharp
// Di MainWindow.xaml.cs - tambahkan test code

using Serilog;
using FluentValidation;
using CommunityToolkit.WinUI.Animations;
using MediatR;
using AutoMapper;

// Seharusnya tidak ada red squiggle errors
// Jika ada, berarti package belum terinstall

public void TestPackages()
{
    // Test 1: Serilog
    var logger = new LoggerConfiguration().CreateLogger();
    
    // Test 2: FluentValidation
    var validator = new AbstractValidator<object>();
    
    // Test 3: CommunityToolkit
    var animation = AnimationHelper.CreateFadeInAnimation(null);
    
    // Test 4: MediatR (needs DI setup)
    // var mediator = serviceProvider.GetRequiredService<IMediator>();
    
    // Test 5: AutoMapper
    // var config = new MapperConfiguration(cfg => { });
}
```

---

## ?? READY?

**Pilih method install favorit Anda dan mulai!**

Setelah install selesai, laporkan dengan format:

```
? INSTALASI SELESAI
- CommunityToolkit.WinUI.Controls: ?
- CommunityToolkit.WinUI.UI: ?
- Serilog: ?
- Serilog.Sinks.File: ?
- FluentValidation: ?
- MediatR: ?
- AutoMapper: ?
- Polly: ?
- Status Build: ? SUCCESS
```

---

## ?? SETELAH INSTALL

Saya akan:
1. ? Generate major UI update code
2. ? Create advanced components
3. ? Setup validation system
4. ? Implement logging
5. ? Add CQRS patterns
6. ? Create beautiful interfaces
7. ? Add modern features

---

**LET'S GO!** ??

