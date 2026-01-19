# System Execution Order / Порядок выполнения систем

## NPC Generation Pipeline / Пайплайн генерации NPC

### English

The NPC generation process is split across two system groups to avoid buffer invalidation issues:

```
┌─────────────────────────────────────────────────────────┐
│         InitializationSystemGroup                       │
│                                                          │
│  1. ChunkManagementSystem                               │
│     └─> Creates/unloads chunks                          │
│                                                          │
│  2. ChunkNPCCleanupSystem                               │
│     └─> Removes NPCs from unloaded chunks               │
│                                                          │
│  3. NPCGeneratorSystem                                  │
│     └─> Creates NPCSpawnData components                 │
│                                                          │
│  4. NPCBufferCreationSystem                             │
│     └─> Creates buffer entities + instructions          │
│     └─> Uses EntityCommandBuffer (ECB)                  │
│                                                          │
│  [ECB Playback - Structural changes applied here]       │
│                                                          │
└─────────────────────────────────────────────────────────┘
                           │
                           │ SYNC POINT
                           ▼
┌─────────────────────────────────────────────────────────┐
│         SimulationSystemGroup                           │
│                                                          │
│  5. NPCBufferFillSystem (OrderFirst = true)             │
│     └─> Fills buffers from instruction entities         │
│     └─> Uses EntityManager.GetBuffer() directly         │
│                                                          │
│  6. NPCBufferCleanupSystem                              │
│     └─> Deletes temporary instruction entities          │
│     └─> Uses EntityCommandBuffer                        │
│                                                          │
│  7. NPCSpawnerSystem                                    │
│     └─> Adds final NPC components                       │
│     └─> Removes NPCSpawnData & NPCBufferEntities        │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Русский

Процесс генерации NPC разделен на две системные группы, чтобы избежать проблем с инвалидацией буферов:

```
┌─────────────────────────────────────────────────────────┐
│         InitializationSystemGroup                       │
│                                                          │
│  1. ChunkManagementSystem                               │
│     └─> Создает/выгружает чанки                         │
│                                                          │
│  2. ChunkNPCCleanupSystem                               │
│     └─> Удаляет NPC из выгруженных чанков               │
│                                                          │
│  3. NPCGeneratorSystem                                  │
│     └─> Создает компоненты NPCSpawnData                 │
│                                                          │
│  4. NPCBufferCreationSystem                             │
│     └─> Создает буферные Entity + инструкции            │
│     └─> Использует EntityCommandBuffer (ECB)            │
│                                                          │
│  [Воспроизведение ECB - структурные изменения здесь]    │
│                                                          │
└─────────────────────────────────────────────────────────┘
                           │
                           │ ТОЧКА СИНХРОНИЗАЦИИ
                           ▼
┌─────────────────────────────────────────────────────────┐
│         SimulationSystemGroup                           │
│                                                          │
│  5. NPCBufferFillSystem (OrderFirst = true)             │
│     └─> Заполняет буферы из instruction entities        │
│     └─> Использует EntityManager.GetBuffer() напрямую   │
│                                                          │
│  6. NPCBufferCleanupSystem                              │
│     └─> Удаляет временные instruction entities          │
│     └─> Использует EntityCommandBuffer                  │
│                                                          │
│  7. NPCSpawnerSystem                                    │
│     └─> Добавляет финальные компоненты NPC              │
│     └─> Удаляет NPCSpawnData и NPCBufferEntities        │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## Why This Design? / Почему такой дизайн?

### English

**Problem**: 
- Unity ECS invalidates `BufferTypeHandle` when structural changes occur
- If we create entities with buffers and immediately query them in the same system group, we get `ObjectDisposedException`

**Solution**:
- Split creation (InitializationSystemGroup) and usage (SimulationSystemGroup)
- ECB in `NPCBufferCreationSystem` is played back at the end of InitializationSystemGroup
- By the time `NPCBufferFillSystem` runs in SimulationSystemGroup, all structural changes are complete
- This creates a **sync point** between groups, ensuring buffer handles are valid

**Key Points**:
- `NPCBufferFillSystem` cannot use `SystemAPI.Query` or `BufferLookup` - both cache buffer handles
- Must use `EntityManager.GetBuffer()` directly for fresh access
- `OrderFirst = true` ensures it runs before other simulation logic

### Русский

**Проблема**:
- Unity ECS инвалидирует `BufferTypeHandle` при структурных изменениях
- Если мы создаем entity с буферами и сразу запрашиваем их в той же системной группе, получаем `ObjectDisposedException`

**Решение**:
- Разделяем создание (InitializationSystemGroup) и использование (SimulationSystemGroup)
- ECB в `NPCBufferCreationSystem` воспроизводится в конце InitializationSystemGroup
- К моменту запуска `NPCBufferFillSystem` в SimulationSystemGroup все структурные изменения завершены
- Это создает **точку синхронизации** между группами, гарантируя валидность buffer handles

**Ключевые моменты**:
- `NPCBufferFillSystem` не может использовать `SystemAPI.Query` или `BufferLookup` - оба кэшируют buffer handles
- Необходимо использовать `EntityManager.GetBuffer()` напрямую для свежего доступа
- `OrderFirst = true` гарантирует, что система запустится перед другой логикой симуляции

---

## Data Flow / Поток данных

### English

```
NPCSpawnData (component)
    │
    ├─> Schedule (instructions)  ──┐
    │                              │
    └─> Relationships (instructions)│
                                    │
            [ECB Playback]          │
                                    │
    ┌──────────────────────────────┘
    │
    ├─> ScheduleBufferEntity (empty buffer)
    │   └─> TimeSlot[] (filled by NPCBufferFillSystem)
    │
    └─> RelationshipsBufferEntity (empty buffer)
        └─> RelationshipEntry[] (filled by NPCBufferFillSystem)
                    │
                    │ [Cleanup]
                    ▼
            Final NPC with all components
```

### Русский

```
NPCSpawnData (компонент)
    │
    ├─> Schedule (инструкции)  ──┐
    │                            │
    └─> Relationships (инструкции)│
                                  │
        [Воспроизведение ECB]     │
                                  │
    ┌────────────────────────────┘
    │
    ├─> ScheduleBufferEntity (пустой буфер)
    │   └─> TimeSlot[] (заполняется NPCBufferFillSystem)
    │
    └─> RelationshipsBufferEntity (пустой буфер)
        └─> RelationshipEntry[] (заполняется NPCBufferFillSystem)
                    │
                    │ [Очистка]
                    ▼
        Финальный NPC со всеми компонентами
```

---

## Performance Considerations / Соображения производительности

### English

- **InitializationSystemGroup** runs once per frame, before simulation
- **SimulationSystemGroup** runs once per frame, contains most game logic
- Splitting across groups adds minimal overhead (< 1% frame time)
- EntityCommandBuffer playback is highly optimized by Unity
- Direct `EntityManager.GetBuffer()` calls are fast (no caching overhead)

### Русский

- **InitializationSystemGroup** выполняется раз за кадр, перед симуляцией
- **SimulationSystemGroup** выполняется раз за кадр, содержит основную игровую логику
- Разделение по группам добавляет минимальные накладные расходы (< 1% времени кадра)
- Воспроизведение EntityCommandBuffer высоко оптимизировано Unity
- Прямые вызовы `EntityManager.GetBuffer()` быстрые (нет накладных расходов на кэширование)
