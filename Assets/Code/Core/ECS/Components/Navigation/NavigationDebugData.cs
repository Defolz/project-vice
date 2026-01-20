using Unity.Entities;
using Unity.Mathematics;

// Компонент для хранения отладочной статистики навигационной сетки
public struct NavigationDebugData : IComponentData
{
    public int2 ChunkId;
    public int WalkableCells;   // Количество проходимых ячеек
    public int BlockedCells;    // Количество заблокированных ячеек
    public int ObstacleCount;   // Количество препятствий, влияющих на этот чанк
    
    public NavigationDebugData(int2 chunkId)
    {
        ChunkId = chunkId;
        WalkableCells = 0;
        BlockedCells = 0;
        ObstacleCount = 0;
    }
    
    // Вычислить статистику из GridData
    public static NavigationDebugData FromGridData(ref GridData gridData, int obstacleCount)
    {
        var data = new NavigationDebugData(gridData.ChunkId);
        data.ObstacleCount = obstacleCount;
        
        for (int i = 0; i < gridData.Cells.Length; i++)
        {
            if (gridData.Cells[i] == 0)
                data.WalkableCells++;
            else
                data.BlockedCells++;
        }
        
        return data;
    }
    
    public float WalkablePercentage => (WalkableCells + BlockedCells) > 0 
        ? (float)WalkableCells / (WalkableCells + BlockedCells) * 100f 
        : 0f;
}
