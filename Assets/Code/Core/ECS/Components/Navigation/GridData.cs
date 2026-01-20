using Unity.Entities;
using Unity.Mathematics;

// BlobAsset структура, хранящая данные навигационной сетки чанка
// Каждая ячейка: 0 = walkable, 1 = blocked
public struct GridData
{
    public BlobArray<byte> Cells; // NAV_GRID_SIZE * NAV_GRID_SIZE элементов
    public int2 ChunkId;
    public int GridSize; // Должно быть равно ChunkConstants.NAV_GRID_SIZE
    
    // Получить индекс ячейки в BlobArray по координатам (x, y)
    public int GetCellIndex(int x, int y)
    {
        return y * GridSize + x;
    }
    
    // Проверить, walkable ли ячейка
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;
        return Cells[GetCellIndex(x, y)] == 0;
    }
}
