# NPC Generation Configuration Guide / Руководство по настройке генерации NPC

## Overview / Общее описание

**English**: The NPC generation system now uses a configurable ScriptableObject instead of hardcoded values, making it easy to adjust NPC spawn behavior without modifying code.

**Русский**: Система генерации NPC теперь использует настраиваемый ScriptableObject вместо захардкоженных значений, что позволяет легко изменять поведение спавна NPC без изменения кода.

---

## Setup Instructions / Инструкция по настройке

### 1. Create Configuration Asset / Создание конфигурационного ассета

**English**:
1. In Unity, right-click in the Project window
2. Select `Create > Project Vice > Configs > NPC Generation Config`
3. Name it (e.g., "DefaultNPCConfig")
4. Adjust values in the Inspector

**Русский**:
1. В Unity кликните правой кнопкой мыши в окне Project
2. Выберите `Create > Project Vice > Configs > NPC Generation Config`
3. Назовите его (например, "DefaultNPCConfig")
4. Настройте значения в Inspector

### 2. Add to Scene / Добавление в сцену

**English**:
1. Create an empty GameObject in your scene (e.g., "GameSettings")
2. Add the `NPCGenerationConfigAuthoring` component
3. Drag your config asset into the "Config" field
4. **Note**: If you don't assign a config, default values will be used

**Русский**:
1. Создайте пустой GameObject в вашей сцене (например, "GameSettings")
2. Добавьте компонент `NPCGenerationConfigAuthoring`
3. Перетащите ваш конфигурационный ассет в поле "Config"
4. **Примечание**: Если вы не назначите конфиг, будут использоваться значения по умолчанию

---

## Configuration Parameters / Параметры конфигурации

### Basic Spawn Settings / Основные настройки спавна

**English**:
- **Min NPC Per Spawn Cycle**: Minimum NPCs generated per cycle (default: 5)
- **Max NPC Per Spawn Cycle**: Maximum NPCs generated per cycle (default: 10)

**Русский**:
- **Min NPC Per Spawn Cycle**: Минимальное количество NPC, генерируемых за цикл (по умолчанию: 5)
- **Max NPC Per Spawn Cycle**: Максимальное количество NPC, генерируемых за цикл (по умолчанию: 10)

### Density Settings / Настройки плотности

**English**:
- **Average NPC Per Chunk**: Average number of NPCs per chunk (default: 2.0)
- **Max NPC Per Chunk**: Hard limit for NPCs in a single chunk (default: 5)

**Русский**:
- **Average NPC Per Chunk**: Среднее количество NPC на чанк (по умолчанию: 2.0)
- **Max NPC Per Chunk**: Жесткий лимит NPC в одном чанке (по умолчанию: 5)

### Faction Weights / Веса фракций

**English**:
Controls the probability of NPCs spawning from different factions.
- **Families Weight**: 0.0 to 1.0 (default: 0.3)
- **Police Weight**: 0.0 to 1.0 (default: 0.2)
- **Civilians Weight**: 0.0 to 1.0 (default: 0.5)

**Note**: These values don't need to sum to 1.0; they're normalized automatically.

**Русский**:
Управляет вероятностью спавна NPC из разных фракций.
- **Families Weight**: от 0.0 до 1.0 (по умолчанию: 0.3)
- **Police Weight**: от 0.0 до 1.0 (по умолчанию: 0.2)
- **Civilians Weight**: от 0.0 до 1.0 (по умолчанию: 0.5)

**Примечание**: Эти значения не должны в сумме давать 1.0; они нормализуются автоматически.

### Traits Range / Диапазон характеристик

**English**:
- **Min Aggression**: Minimum aggression value (default: 0.1)
- **Max Aggression**: Maximum aggression value (default: 0.9)
- **TODO**: Add min/max for other traits (Loyalty, Intelligence, etc.)

**Русский**:
- **Min Aggression**: Минимальное значение агрессии (по умолчанию: 0.1)
- **Max Aggression**: Максимальное значение агрессии (по умолчанию: 0.9)
- **TODO**: Добавить min/max для других характеристик (Лояльность, Интеллект и т.д.)

---

## How It Works / Как это работает

### Data Flow / Поток данных

```
ScriptableObject (NPCGenerationConfig)
    ↓
MonoBehaviour (NPCGenerationConfigAuthoring) [Baking]
    ↓
ECS Component (NPCGenerationSettings) [Singleton]
    ↓
NPCGeneratorSystem [Uses settings during generation]
```

### Baking System / Система выпекания

**English**:
Unity's Entity Component System uses a "baking" process to convert MonoBehaviour components into ECS components:
- At edit time or during scene load, the `NPCGenerationConfigAuthoring.Baker` runs
- It reads values from the ScriptableObject and creates an `NPCGenerationSettings` singleton
- The `NPCGeneratorSystem` reads this singleton each frame (with fallback to defaults)

**Русский**:
Entity Component System Unity использует процесс "выпекания" (baking) для конвертации MonoBehaviour компонентов в ECS компоненты:
- Во время редактирования или при загрузке сцены запускается `NPCGenerationConfigAuthoring.Baker`
- Он читает значения из ScriptableObject и создает синглтон `NPCGenerationSettings`
- `NPCGeneratorSystem` читает этот синглтон каждый кадр (с откатом к значениям по умолчанию)

### Runtime Behavior / Поведение во время выполнения

**English**:
- If no config is assigned, the system uses `NPCGenerationSettings.Default`
- Settings are checked at the start of each generation cycle
- Changes to the ScriptableObject require entering Play mode again to take effect

**Русский**:
- Если конфиг не назначен, система использует `NPCGenerationSettings.Default`
- Настройки проверяются в начале каждого цикла генерации
- Изменения в ScriptableObject требуют повторного входа в режим Play для вступления в силу

---

## Examples / Примеры

### High-Density Urban Area / Городская зона высокой плотности
```
Average NPC Per Chunk: 5.0
Max NPC Per Chunk: 10
Families Weight: 0.2
Police Weight: 0.4
Civilians Weight: 0.4
```

### Low-Density Suburban Area / Пригородная зона низкой плотности
```
Average NPC Per Chunk: 1.0
Max NPC Per Chunk: 3
Families Weight: 0.1
Police Weight: 0.2
Civilians Weight: 0.7
```

### Gang Territory / Территория банды
```
Average NPC Per Chunk: 3.0
Max NPC Per Chunk: 6
Families Weight: 0.7
Police Weight: 0.1
Civilians Weight: 0.2
Min Aggression: 0.5
Max Aggression: 1.0
```

---

## Troubleshooting / Решение проблем

### NPCs not spawning / NPC не спавнятся

**English**:
- Check that `NPCGenerationConfigAuthoring` is in the scene
- Verify that the config asset is assigned (or defaults are acceptable)
- Check console for errors related to chunk management

**Русский**:
- Убедитесь, что `NPCGenerationConfigAuthoring` находится в сцене
- Проверьте, что конфигурационный ассет назначен (или значения по умолчанию приемлемы)
- Проверьте консоль на наличие ошибок, связанных с управлением чанками

### NPCs spawn with wrong distribution / NPC спавнятся с неправильным распределением

**English**:
- Verify faction weights in the config
- Remember: weights are normalized, so (0.3, 0.2, 0.5) and (3, 2, 5) are equivalent

**Русский**:
- Проверьте веса фракций в конфиге
- Помните: веса нормализуются, так что (0.3, 0.2, 0.5) и (3, 2, 5) эквивалентны

### Changes not taking effect / Изменения не вступают в силу

**English**:
- ScriptableObject changes require re-entering Play mode
- Ensure you're editing the correct config asset
- Check if multiple configs exist and the wrong one is assigned

**Русский**:
- Изменения в ScriptableObject требуют повторного входа в режим Play
- Убедитесь, что вы редактируете правильный конфигурационный ассет
- Проверьте, существует ли несколько конфигов и не назначен ли неправильный

---

## Technical Notes / Технические заметки

### Performance / Производительность

**English**:
- Settings are read once per frame (not per NPC)
- Uses `TryGetSingleton` with fallback to avoid crashes
- All computations are Burst-compiled for optimal performance

**Русский**:
- Настройки читаются один раз за кадр (а не для каждого NPC)
- Использует `TryGetSingleton` с откатом для избежания крашей
- Все вычисления скомпилированы через Burst для оптимальной производительности

### Thread Safety / Потокобезопасность

**English**:
- `NPCGenerationSettings` is a value type (struct), safe for Burst
- Settings are immutable during generation (read-only access)
- Random seed ensures deterministic behavior within a frame

**Русский**:
- `NPCGenerationSettings` является типом-значением (struct), безопасным для Burst
- Настройки неизменяемы во время генерации (доступ только для чтения)
- Случайное зерно (seed) обеспечивает детерминированное поведение в пределах кадра

---

## Future Enhancements / Будущие улучшения

- [ ] Add min/max ranges for all trait types / Добавить диапазоны min/max для всех типов характеристик
- [ ] Support multiple configs per scene (zone-based) / Поддержка нескольких конфигов на сцену (по зонам)
- [ ] Add runtime config switching (hot-reload) / Добавить переключение конфига во время выполнения (горячая перезагрузка)
- [ ] Add validation (e.g., warn if faction weights sum to 0) / Добавить валидацию (например, предупреждение если веса фракций в сумме дают 0)
- [ ] Add presets for common scenarios / Добавить пресеты для типичных сценариев
