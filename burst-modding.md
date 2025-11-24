# Burst Modding Support in Cities: Skylines II

## Overview

Unity's Burst compiler optimizes C# code to highly-performant native code, but this creates a challenge for modding: Burst-compiled code is immutable at runtime. This document explores two approaches for enabling Burst modding and their applicability to Cities: Skylines II.

---

## The Burst Modding Problem

**Why Burst is Hard to Mod:**
- Burst compiles C# jobs to native machine code at compile time or first use
- The compiled code is immutable - you can't patch or hook it at runtime
- Reflection and dynamic code don't work in Burst-compiled code
- Most game systems in Cities: Skylines II use Burst for performance

**Impact on Modding:**
- Mods can't modify behavior inside `[BurstCompile]` jobs
- Can't inject custom logic into performance-critical game systems
- Limited to modding non-Burst code or creating entirely separate systems

---

## Approach 1: SharedStatic API

### What It Is

The `SharedStatic<T>` API allows creating static data that:
- Can be **written** by non-Burst code (like mod initialization)
- Can be **read** from within Burst-compiled jobs
- Can store function pointers that Burst jobs invoke

### How It Works

**Game-side code:**
```csharp
using Unity.Burst;

public class ZoneSystem
{
    // Game defines hook points
    public static readonly SharedStatic<FunctionPointer<ZoneModificationDelegate>>
        ZoneModHook = SharedStatic<FunctionPointer<ZoneModificationDelegate>>
            .GetOrCreate<ZoneModificationKey>();

    public delegate void ZoneModificationDelegate(ref ZoneData data);

    [BurstCompile]
    struct ProcessZonesJob : IJobChunk
    {
        public void Execute(...)
        {
            // Game calls mod hook if registered
            if (ZoneModHook.Data.IsCreated)
            {
                ZoneModHook.Data.Invoke(ref zoneData);
            }

            // ... rest of game logic
        }
    }
}
```

**Mod code:**
```csharp
using Unity.Burst;

public class MyZoneMod : IMod
{
    [BurstCompile]
    private static void ModifyZone(ref ZoneData data)
    {
        // Custom Burst-compiled mod logic
        data.density *= 1.5f;
    }

    public void OnLoad(UpdateSystem updateSystem)
    {
        // Register Burst-compiled mod function
        var functionPointer = BurstCompiler.CompileFunctionPointer<ZoneModificationDelegate>(ModifyZone);
        ZoneSystem.ZoneModHook.Data = functionPointer;
    }
}
```

### Requirements

**The game must:**
1. ✅ Define `SharedStatic` fields in game code
2. ✅ Add hook point invocations inside Burst jobs
3. ✅ Expose these fields publicly for mods to write to
4. ✅ Document the delegate signatures and when hooks are called

**Modders can:**
- Write Burst-compiled callback functions
- Register them via the SharedStatic fields
- Have their code run inside game's Burst jobs with full performance

### Current State in Cities: Skylines II

**Status:** ❌ **Not Implemented**

**Evidence:**
```bash
# Search results for SharedStatic usage
New folder/Unity.Entities.CodeGeneratedRegistry/AssemblyTypeRegistry.cs
```

- Only usage is in Unity's internal code generation
- **No modding hook points defined in game systems**
- No public SharedStatic fields for mods to access

**What Would Be Needed:**
- Colossal Order would need to release a game update
- Add SharedStatic hook points to moddable systems
- Document available hooks and their signatures
- Examples: zone processing, traffic simulation, economy calculations, etc.

---

## Approach 2: BurstRuntime.LoadAdditionalLibrary

### What It Is

Unity provides `BurstRuntime.LoadAdditionalLibrary()` (Unity 2021.1+) to load external Burst-compiled DLLs at runtime. This allows mods to ship their own Burst-compiled code separately.

### How It Works

**Game-side modding manager:**
```csharp
using Unity.Burst;
using System.Reflection;

public class PluginManager : MonoBehaviour
{
    void Start()
    {
        var modsFolder = Path.Combine(Application.dataPath, "..", "Mods");

        foreach (var modFolder in Directory.GetDirectories(modsFolder))
        {
            var modName = Path.GetFileName(modFolder);

            // Load managed DLL
            var managedDll = Path.Combine(modFolder, $"{modName}_managed.dll");
            if (File.Exists(managedDll))
            {
                var assembly = Assembly.LoadFile(managedDll);
                var plugin = CreatePluginInstance(assembly);
                plugin.OnLoad();
            }

            // Load Burst DLL
            var burstDll = Path.Combine(modFolder, $"{modName}_win_x86_64.dll");
            if (File.Exists(burstDll))
            {
                BurstRuntime.LoadAdditionalLibrary(burstDll);
            }
        }
    }
}
```

**Mod plugin interface:**
```csharp
public interface IPluginModule
{
    void Startup(GameObject gameObject);
    void Update(GameObject gameObject);
}
```

**Mod implementation:**
```csharp
using Unity.Burst;
using Unity.Jobs;

public class MyMod : IPluginModule
{
    [BurstCompile]
    struct MyBurstJob : IJob
    {
        public void Execute()
        {
            // Burst-compiled mod logic
        }
    }

    public void Update(GameObject gameObject)
    {
        new MyBurstJob().Run();
    }
}
```

### Requirements

**The game must:**
1. ✅ Define a plugin/mod interface
2. ✅ Implement a mod manager that loads assemblies
3. ✅ Call `BurstRuntime.LoadAdditionalLibrary()` for Burst DLLs
4. ✅ Invoke mod callbacks at appropriate times
5. ✅ Provide game API that mods can call

**Modders must:**
- Create mods in Unity Editor (required to compile Burst code)
- Build for target platform (Windows x64, etc.)
- Ship both managed DLL and Burst DLL
- Follow game's plugin interface

### Current State in Cities: Skylines II

**Status:** 🟡 **Partially Implemented**

**Evidence from decompiled code:**

```csharp
// From ModManager.cs:166
private const string kBurstSuffix = "_win_x86_64";

// From ModManager.cs:62
public bool isBursted => asset.isBursted;
```

**Analysis:**
- ✅ Cities: Skylines II **has a ModManager** (`Game.Modding.ModManager`)
- ✅ Cities: Skylines II **has an IMod interface** for mods to implement
- ✅ ModManager tracks whether mods are "bursted" (`isBursted` property)
- ✅ Defines the Windows Burst DLL suffix (`kBurstSuffix = "_win_x86_64"`)
- ❌ **BUT:** `kBurstSuffix` constant is **defined but never used** in the code
- ❌ No calls to `BurstRuntime.LoadAdditionalLibrary()` found

**Interpretation:**
> Colossal Order **planned for Burst modding support** during development but **has not fully implemented it yet**. The infrastructure is partially there, but the actual loading of Burst DLLs is not active.

**What Currently Works:**
- Mods can implement `IMod` interface
- ModManager loads managed mod assemblies
- Mods can create new systems and components

**What Doesn't Work:**
- Burst-compiled mod code is **ignored**
- Only the managed DLL is loaded, Burst DLL is never loaded
- Mods run in managed (slower) mode only

---

## Comparison: SharedStatic vs LoadAdditionalLibrary

| Feature | SharedStatic | LoadAdditionalLibrary |
|---------|--------------|----------------------|
| **Purpose** | Inject callbacks into **existing** game Burst jobs | Load **separate** mod Burst code |
| **Integration** | Tight - mods run **inside** game systems | Loose - mods run **alongside** game systems |
| **Performance** | Highest - runs in game's Burst context | High - mods have their own Burst jobs |
| **Flexibility** | Limited to defined hook points | Full - mods create their own systems |
| **Game Changes** | Must add hook points to Burst jobs | Must implement mod loader |
| **Mod Complexity** | Simple - implement delegates | Complex - full Unity project |
| **CS2 Status** | ❌ Not implemented | 🟡 Partially implemented |

---

## Recommendations for Cities: Skylines II

### For Colossal Order

**Option 1: Complete LoadAdditionalLibrary Implementation** (Easier)
1. Add `BurstRuntime.LoadAdditionalLibrary()` call in `ModManager.cs`
2. Use the existing `kBurstSuffix` constant to locate Burst DLLs
3. Document the mod build process
4. Provide example mod template project

**Option 2: Add SharedStatic Hooks** (More powerful)
1. Identify key moddable systems (zones, traffic, economy, etc.)
2. Add `SharedStatic` function pointer fields
3. Add hook invocations in Burst jobs
4. Document hook points and delegates

**Option 3: Both** (Best of both worlds)
- LoadAdditionalLibrary: For mods creating new systems
- SharedStatic: For mods modifying existing systems

### For Modders

**Current Best Practices:**
1. Create mods using the `IMod` interface
2. Avoid Burst compilation (it won't be used)
3. Focus on creating new `SystemBase` or `GameSystemBase` systems
4. Work with components and entities using managed code

**When Burst Support is Added:**
1. Structure code to separate Burst-compilable logic
2. Use `[BurstCompile]` on jobs when Burst loading works
3. If SharedStatic hooks are added, implement callbacks
4. Ship both managed and Burst DLLs with mods

---

## Technical Deep Dive: Why This Matters

### Performance Impact

**Managed vs Burst Performance:**
- Managed code: ~10-100x slower for computational tasks
- Burst code: Near C++ performance
- Critical for: Pathfinding, traffic simulation, zone calculations, etc.

**Example:**
```csharp
// Managed: ~100ms for 10,000 zones
void ProcessZones_Managed()
{
    foreach (var zone in zones)
    {
        zone.density = CalculateDensity(zone);
    }
}

// Burst: ~1-2ms for 10,000 zones
[BurstCompile]
struct ProcessZonesJob : IJobParallelFor
{
    public void Execute(int index)
    {
        zones[index].density = CalculateDensity(zones[index]);
    }
}
```

**Impact on Modding:**
- Without Burst: Mods can cause significant performance degradation
- With Burst: Mods can maintain near-vanilla performance
- Especially important for mods that run every frame

### Current Workarounds

**What Modders Can Do Now:**

1. **Minimize computational work in OnUpdate:**
   ```csharp
   public override void OnUpdate()
   {
       // Use GetUpdateInterval() to run less frequently
       // Keep logic simple and fast
   }
   ```

2. **Use ECS efficiently:**
   ```csharp
   // Good: Query only what you need
   EntityQuery query = GetEntityQuery(
       ComponentType.ReadOnly<Building>(),
       ComponentType.ReadWrite<CustomData>()
   );
   ```

3. **Defer heavy work:**
   ```csharp
   // Spread work across multiple frames
   public override int GetUpdateInterval(SystemUpdatePhase phase)
   {
       return 16; // Run every 16 ticks instead of every tick
   }
   ```

4. **Use Unity Jobs (non-Burst):**
   ```csharp
   // Still parallel, just not Burst-optimized
   struct MyJob : IJobParallelFor
   {
       public void Execute(int index) { /* ... */ }
   }

   public void OnUpdate()
   {
       new MyJob { /* ... */ }.Schedule(count, 64).Complete();
   }
   ```

---

## Conclusion

**Current State:**
- Burst modding in Cities: Skylines II is **not currently functional**
- The game has **partial infrastructure** suggesting it was planned
- Modders are limited to **managed code** with performance implications

**Future Possibilities:**
- Colossal Order could **complete the LoadAdditionalLibrary implementation**
- They could **add SharedStatic hooks** for more integrated modding
- Both approaches would significantly improve mod performance

**For Modders:**
- Continue using the `IMod` interface and `SystemBase`
- Optimize managed code as much as possible
- Use update intervals to reduce CPU usage
- Watch for official Burst modding support announcements

---

## Additional Resources

### Unity Documentation
- [Burst Compiler](https://docs.unity3d.com/Packages/com.unity.burst@latest)
- [Burst Modding Support](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/modding-support.html)
- [SharedStatic API](https://docs.unity3d.com/Packages/com.unity.burst@latest/api/Unity.Burst.SharedStatic-1.html)

### Cities: Skylines II Modding
- [Systems Development Guide](systems.md)
- [ECS Reference](ecs-reference.md)
- [Tool System Guide](tool.md)

### Related Code Locations
- Mod Interface: `Game.Modding.IMod`
- Mod Manager: `Game.Modding.ModManager` (line 166: `kBurstSuffix`, line 62: `isBursted`)
- Example Burst Jobs: `Game.Zones.SearchSystem`, `Game.Zones.BlockSystem`

---

## See Also

- [Unity ECS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Colossal Order Modding Documentation](https://cs2.paradoxwikis.com/Modding)
