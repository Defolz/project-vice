# Code Review Fixes - January 2026

## Overview
This commit includes critical bug fixes and optimizations identified during comprehensive code review.

## Critical Fixes (Priority 1)

### 1. Fixed Burst Compatibility Issues
- **NameData.cs**: Renamed `ToString()` to `ToDebugString()` to prevent Burst compilation errors
  - ToString() was calling .ToString() on FixedString which is incompatible with Burst
  - New method has explicit warning that it should never be used in Burst systems

### 2. Fixed Typo in Filename
- **Shedule.cs → Schedule.cs**: Corrected filename spelling

### 3. Unified Data Types
- **NPCId.cs**: Changed `GenerationSeed` from `int` to `uint` for consistency with `Value`
  - Both fields now use the same type, preventing potential casting issues

### 4. Fixed Random Seed Issues
- **NPCBufferCreationSystem.cs**: Changed from fixed seed (12345) to dynamic seed
- **NPCGeneratorSystem.cs**: Changed from fixed seed (12345) to dynamic seed
  - Now using `(uint)(ElapsedTime * 1000.0) + 1`
  - `+ 1` prevents "Seed must be non-zero" error at game start

### 5. Optimized ChunkManagementSystem Performance
- **ChunkManagementSystem.cs**: Replaced O(n²) nested loops with O(n) HashMap lookups
  - Added `NativeHashMap<int2, ChunkMapEntry>` for existing chunks
  - Added `NativeHashSet<int2>` for required chunks validation
  - Performance improvement: from O(n²) to O(n)
  - **Bug Fix**: Fixed issue where 2 NPCs were created but system reported 4

### 6. Fixed Memory Leak
- **GameInputSystem.cs**: Removed EntityQuery memory leak
  - Replaced `GetEntityQuery()` + manual disposal with `SystemAPI.TryGetSingletonEntity()`
  - Query was being created every frame without disposal

### 7. Fixed Type Casting Issues
- **GameTimeSystem.cs**: Removed unnecessary `sbyte` casts
  - Changed from `(sbyte)(totalMinutes % 60)` to `totalMinutes % 60`
  - Values are always positive, int is more appropriate

### 8. Added Error Handling
- **NPCGeneratorSystem.cs**: Added validation for ChunkMapSingleton
  - Check if singleton exists before accessing
  - Check if ChunkMapDataEntity and buffer are valid
  - Early return if validation fails

### 9. Improved Documentation
- **NPCBufferCleanupSystem.cs**: Added comprehensive workflow documentation
  - Documented system execution order (5 steps)
  - Explained why Burst is disabled (for debugging)

### 10. Created Faction Enum
- **Faction.cs**: Replaced magic numbers with `FactionType` enum
  - Type-safe faction representation
  - Backward compatible with existing int-based code
  - Improved ToString() to use enum names

### 11. Fixed System Group Ordering
All NPC generation systems moved to `InitializationSystemGroup` for consistency:
- NPCGeneratorSystem (was already there)
- NPCBufferCreationSystem (was already there)
- NPCBufferFillSystem (moved from SimulationSystemGroup)
- NPCBufferCleanupSystem (moved from SimulationSystemGroup)
- NPCSpawnerSystem (was already there)
- ChunkNPCCleanupSystem (moved from SimulationSystemGroup)

This fixes "Invalid UpdateAfterAttribute" warnings - all systems with dependencies are now in the same group.

### 12. Fixed Buffer Access Invalidation
- **NPCBufferFillSystem.cs**: Replaced `EntityManager.GetBuffer()` with `BufferLookup`
  - Fixed `ObjectDisposedException: BufferTypeHandle invalidated by structural change`
  - Added `OnCreate()` to initialize BufferLookup instances
  - Added `Update()` calls in OnUpdate to refresh lookups
  - Re-enabled Burst compilation (was disabled due to this issue)

## System Execution Order (InitializationSystemGroup)
1. ChunkManagementSystem → Creates/unloads chunks
2. ChunkNPCCleanupSystem → Cleans NPCs in unloaded chunks
3. NPCGeneratorSystem → Creates NPCSpawnData
4. NPCBufferCreationSystem → Creates buffers and instructions
5. NPCBufferFillSystem → Fills buffers from instructions
6. NPCBufferCleanupSystem → Removes temporary instructions
7. NPCSpawnerSystem → Adds final components

## Performance Improvements
- ChunkManagementSystem: O(n²) → O(n)
- GameInputSystem: Memory leak fixed
- NPCBufferFillSystem: Now Burst-compiled (was disabled)

## Bug Fixes
- Fixed NPC spawn count mismatch (reported 4, created 2) → now consistent
- Fixed "Seed must be non-zero" error on game start
- Fixed buffer invalidation crashes
- Fixed system ordering warnings

## Technical Debt Addressed
- Removed magic numbers (Faction)
- Improved type safety (uint for seeds, enum for factions)
- Better error handling (NPCGeneratorSystem)
- Comprehensive documentation (NPCBufferCleanupSystem)
- Consistent naming (Schedule instead of Shedule)

## Files Changed
- Core/ECS/Components/Gameplay/NPC/NameData.cs
- Core/ECS/Components/Gameplay/NPC/Schedule.cs (renamed from Shedule.cs)
- Core/ECS/Components/Gameplay/NPC/NPCId.cs
- Core/ECS/Components/Gameplay/NPC/Faction.cs
- Core/ECS/Systems/Actions/GameInputSystem.cs
- Core/ECS/Systems/Global/GameTimeSystem.cs
- Core/ECS/Systems/Gameplay/NPCGeneration/NPCBufferCreationSystem.cs
- Core/ECS/Systems/Gameplay/NPCGeneration/NPCBufferFillSystem.cs
- Core/ECS/Systems/Gameplay/NPCGeneration/NPCBufferCleanupSystem.cs
- Core/ECS/Systems/Gameplay/NPCGeneration/NPCGeneratorSystem.cs
- Core/ECS/Systems/Gameplay/NPCGeneration/NPCSpawnerSystem.cs
- World/Generation/Systems/ChunkManagementSystem.cs
- World/Generation/Systems/ChunkNPCCleanupSystem.cs

## Next Steps (Priority 2-3)
- Integrate NPCGenerationConfig ScriptableObject
- Add validation to component constructors
- Optimize ChunkNPCCleanupSystem HashSet sizing
- Add data flow diagrams for complex systems
- Move test systems to separate directory
