# Cities: Skylines II Modding Structure Analysis

## ⚠️ IMPORTANT: Mod Structure Requirements

After analyzing the game's modding system, here's what we need to fix:

---

## Current Status: ❌ INCOMPLETE

### What We Have ✅
- ✅ Utility classes (BurstPerlinNoise, TerrainHelpers, CurveUtils)
- ✅ Data structures (LayoutParameters)
- ✅ Burst-compatible code
- ✅ Proper namespaces (Unity.*, Game.*)

### What's Missing ❌
- ❌ Proper `.csproj` file for mod compilation
- ❌ `IMod` interface implementation
- ❌ Mod entry point (`OnLoad`, `OnDispose`)
- ❌ System registration with `UpdateSystem`
- ❌ Proper assembly references

---

## Cities: Skylines II Modding Requirements

### 1. IMod Interface (REQUIRED)

Every mod MUST implement `Game.Modding.IMod`:

```csharp
namespace Game.Modding
{
    public interface IMod
    {
        void OnLoad(UpdateSystem updateSystem);
        void OnDispose();
    }
}
```

**What this means**:
- Your mod needs a main class that implements `IMod`
- `OnLoad()` is called when mod loads - register systems here
- `OnDispose()` is called when mod unloads - cleanup here

### 2. Project Structure

**Required Files**:
```
OrganicNeighborhoodMod/
├── OrganicNeighborhoodMod.csproj    ← Need to create
├── Mod.cs                            ← Main mod class (IMod)
├── Utils/
│   ├── BurstPerlinNoise.cs          ✅ Already created
│   ├── TerrainHelpers.cs            ✅ Already created
│   └── CurveUtils.cs                ✅ Already created
├── Data/
│   └── LayoutParameters.cs          ✅ Already created
└── Systems/
    └── OrganicNeighborhoodToolSystem.cs  ← Phase 2
```

### 3. .csproj Requirements

**Key Requirements**:
- Target Framework: `.NET Standard 2.1` or `.NET 4.7.2`
- C# Language Version: `12.0`
- Allow Unsafe Blocks: `true`
- References to game DLLs

**Environment Variables** (from toolchain):
- `CSII_MANAGEDPATH`: Game DLL location
- `CSII_LOCALMODSPATH`: Where mods install
- `CSII_ENTITIESVERSION`: Unity Entities version (1.3.10)

### 4. System Registration

Systems must be registered in `OnLoad()`:

```csharp
public class Mod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // Register your custom systems
        updateSystem.UpdateAt<OrganicNeighborhoodToolSystem>(
            SystemUpdatePhase.ToolUpdate);
    }

    public void OnDispose()
    {
        // Cleanup
    }
}
```

---

## What We Need to Add

### File 1: `OrganicNeighborhoodMod.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>OrganicNeighborhoodMod</AssemblyName>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>

  <ItemGroup>
    <!-- Game DLL references -->
    <Reference Include="Game">
      <HintPath>$(CSII_MANAGEDPATH)\Game.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.Entities">
      <HintPath>$(CSII_MANAGEDPATH)\Unity.Entities.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.Mathematics">
      <HintPath>$(CSII_MANAGEDPATH)\Unity.Mathematics.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.Collections">
      <HintPath>$(CSII_MANAGEDPATH)\Unity.Collections.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.Burst">
      <HintPath>$(CSII_MANAGEDPATH)\Unity.Burst.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Colossal.Mathematics">
      <HintPath>$(CSII_MANAGEDPATH)\Colossal.Mathematics.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- Add more as needed -->
  </ItemGroup>
</Project>
```

### File 2: `Mod.cs` (Main Entry Point)

```csharp
using Game;
using Game.Modding;
using Colossal.Logging;

namespace OrganicNeighborhood
{
    public class Mod : IMod
    {
        private static ILog s_Log;

        public void OnLoad(UpdateSystem updateSystem)
        {
            s_Log = LogManager.GetLogger("OrganicNeighborhoodMod");
            s_Log.Info("OrganicNeighborhoodMod loading...");

            // Register systems when they're created (Phase 2+)
            // updateSystem.UpdateAt<OrganicNeighborhoodToolSystem>(
            //     SystemUpdatePhase.ToolUpdate);

            s_Log.Info("OrganicNeighborhoodMod loaded successfully!");
        }

        public void OnDispose()
        {
            s_Log.Info("OrganicNeighborhoodMod disposing...");
            // Cleanup code here
        }
    }
}
```

---

## Answer to Your Question

### "Is this mod being built to current Skylines 2 mod specs?"

**Current Status**: ⚠️ **Partially**

**What Matches Specs**:
- ✅ Code structure (systems, utilities, data)
- ✅ Burst compilation
- ✅ Unity ECS architecture
- ✅ Proper namespaces and types

**What Needs to Be Added**:
- ❌ `.csproj` mod project file
- ❌ `IMod` implementation
- ❌ System registration
- ❌ Proper build/deployment setup

---

## Action Required

We need to add 2 files to make this a proper CS2 mod:

1. **OrganicNeighborhoodMod.csproj** - Build configuration
2. **Mod.cs** - Entry point with IMod interface

After adding these:
- ✅ Mod will compile properly
- ✅ Game will recognize it as a mod
- ✅ Mod will load when game starts
- ✅ Systems will register correctly

---

## Next Steps

**Option 1: Add mod structure now** (RECOMMENDED)
- Create `.csproj` file
- Create `Mod.cs` entry point
- Verify Phase 1 utilities work with mod loader

**Option 2: Continue as planned, add structure at Phase 5**
- Keep building utilities and systems
- Add mod structure when we integrate with game

**Which do you prefer?**

I recommend Option 1 - let's make it a proper mod NOW, then continue with Phase 2.

---

## References

- **IMod Interface**: `/New folder/Game.Modding/IMod.cs`
- **ModManager**: `/New folder/Game.Modding/ModManager.cs`
- **Toolchain**: `/New folder/Game.Modding.Toolchain/`
- **Environment Variables**: `CSII_MANAGEDPATH`, `CSII_LOCALMODSPATH`

---

**Summary**: Our code is GOOD, but we're missing the mod wrapper. Let's add it!
