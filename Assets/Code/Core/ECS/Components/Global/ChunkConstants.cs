using System;

// Единый источник истины для настроек чанков
public static class ChunkConstants
{
    public const float CHUNK_SIZE = 100.0f; // Размер стороны чанка в метрах
    public const int ENTITY_CAPACITY_PER_CHUNK = 256; // Пример: сколько Entity можно отслеживать в чанке
    public const int VIEW_DISTANCE_IN_CHUNKS = 5; // На сколько чанков в каждую сторону от центра загружать (пример для симуляции)
    
    // Navigation Grid Settings
    public const int NAV_GRID_SIZE = 64; // 64x64 ячеек навигационной сетки на чанк
    public const float NAV_CELL_SIZE = CHUNK_SIZE / NAV_GRID_SIZE; // ~1.56м размер ячейки
}