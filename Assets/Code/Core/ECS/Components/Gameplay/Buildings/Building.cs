using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Компонент, представляющий здание в мире
/// Используется для навигации NPC и планирования целей
/// </summary>
public struct Building : IComponentData
{
    /// <summary>
    /// Тип здания, определяющий какие NPC могут его посещать
    /// </summary>
    public BuildingType Type;
    
    /// <summary>
    /// Позиция центра здания в мировых координатах (2D X-Y)
    /// </summary>
    public float2 Position;
    
    /// <summary>
    /// Размер здания (ширина и длина в метрах)
    /// </summary>
    public float2 Size;
    
    /// <summary>
    /// Высота здания в метрах
    /// </summary>
    public float Height;
    
    /// <summary>
    /// ID чанка, к которому принадлежит здание
    /// </summary>
    public int2 ChunkId;
    
    /// <summary>
    /// Текущее количество NPC внутри здания
    /// </summary>
    public int CurrentOccupancy;
    
    /// <summary>
    /// Максимальное количество NPC, которые могут находиться в здании одновременно
    /// </summary>
    public int MaxOccupancy;
    
    /// <summary>
    /// Флаг доступности здания (может ли NPC войти)
    /// </summary>
    public bool IsAccessible;
    
    public Building(BuildingType type, float2 position, float2 size, float height, 
                   int2 chunkId, int maxOccupancy = 10)
    {
        Type = type;
        Position = position;
        Size = size;
        Height = height;
        ChunkId = chunkId;
        CurrentOccupancy = 0;
        MaxOccupancy = maxOccupancy;
        IsAccessible = true;
    }
    
    /// <summary>
    /// Проверяет, может ли здание принять еще одного посетителя
    /// </summary>
    public bool CanAcceptVisitor => IsAccessible && CurrentOccupancy < MaxOccupancy;
    
    /// <summary>
    /// Получить позицию входа в здание (нижняя центральная точка)
    /// </summary>
    public float2 GetEntrancePosition()
    {
        return new float2(Position.x, Position.y - Size.y * 0.5f);
    }
    
    /// <summary>
    /// Проверяет, находится ли точка внутри здания (2D AABB)
    /// </summary>
    public bool ContainsPoint(float2 point)
    {
        var halfSize = Size * 0.5f;
        var min = Position - halfSize;
        var max = Position + halfSize;
        
        return point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y;
    }
    
    /// <summary>
    /// Получить расстояние от точки до ближайшей границы здания
    /// </summary>
    public float GetDistanceToPoint(float2 point)
    {
        var halfSize = Size * 0.5f;
        var min = Position - halfSize;
        var max = Position + halfSize;
        
        // Clamp точку к границам здания
        var closest = math.clamp(point, min, max);
        
        return math.distance(point, closest);
    }
    
    public override string ToString()
    {
        return $"Building({Type}, Pos:{Position}, Size:{Size}, Height:{Height}m, Occupancy:{CurrentOccupancy}/{MaxOccupancy})";
    }
}

/// <summary>
/// Типы зданий для различных активностей NPC
/// </summary>
public enum BuildingType : byte
{
    /// <summary>
    /// Жилое здание - дом, квартира, спальное место
    /// Активности: Sleep, Socialize
    /// </summary>
    Residential = 0,
    
    /// <summary>
    /// Коммерческое здание - магазин, ресторан, развлечения
    /// Активности: Eat, Socialize, VisitLocation
    /// </summary>
    Commercial = 1,
    
    /// <summary>
    /// Промышленное здание - склад, фабрика, производство
    /// Активности: Work
    /// </summary>
    Industrial = 2,
    
    /// <summary>
    /// Общественное здание - полицейский участок, больница, школа
    /// Активности: Work (для полиции/медиков), VisitLocation
    /// </summary>
    Public = 3,
    
    /// <summary>
    /// Специальное здание - банк, казино, гангстерская база
    /// Активности: Special missions, PatrolArea (для банд)
    /// </summary>
    Special = 4
}

/// <summary>
/// Вспомогательный класс для работы с типами зданий
/// </summary>
public static class BuildingTypeExtensions
{
    /// <summary>
    /// Проверяет, подходит ли здание для указанного типа цели
    /// </summary>
    public static bool SupportsGoal(this BuildingType buildingType, GoalType goalType)
    {
        return (buildingType, goalType) switch
        {
            (BuildingType.Residential, GoalType.Sleep) => true,
            (BuildingType.Residential, GoalType.Socialize) => true,
            
            (BuildingType.Commercial, GoalType.Eat) => true,
            (BuildingType.Commercial, GoalType.Socialize) => true,
            (BuildingType.Commercial, GoalType.VisitLocation) => true,
            
            (BuildingType.Industrial, GoalType.Work) => true,
            
            (BuildingType.Public, GoalType.Work) => true,
            (BuildingType.Public, GoalType.VisitLocation) => true,
            
            (BuildingType.Special, GoalType.PatrolArea) => true,
            (BuildingType.Special, GoalType.VisitLocation) => true,
            
            _ => false
        };
    }
    
    /// <summary>
    /// Проверяет, может ли фракция использовать здание для работы
    /// </summary>
    public static bool CanWorkIn(this BuildingType buildingType, FactionType faction)
    {
        return (buildingType, faction) switch
        {
            (BuildingType.Public, FactionType.Police) => true,
            (BuildingType.Public, FactionType.FBI) => true,
            (BuildingType.Industrial, FactionType.Civilians) => true,
            (BuildingType.Commercial, FactionType.Civilians) => true,
            (BuildingType.Special, FactionType.Families) => true,
            (BuildingType.Special, FactionType.Colombians) => true,
            _ => false
        };
    }
}
