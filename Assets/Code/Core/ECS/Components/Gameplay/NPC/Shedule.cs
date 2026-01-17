using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

// Представляет временные слоты в расписании NPC (например, "8-10: работа", "12-14: обед", "18-22: дом")
public struct TimeSlot : IBufferElementData
{
    public byte StartTimeHour;   // Начало слота в часах (0-23)
    public byte EndTimeHour;     // Конец слота в часах (0-23)
    public int ActivityType;     // Тип активности (например, enum, закодированный в int)

    public TimeSlot(byte start, byte end, int activityType)
    {
        // Явно приводим byte к int для устранения неоднозначности в вызове math.clamp
        StartTimeHour = (byte)math.clamp((int)start, 0, 23);
        EndTimeHour = (byte)math.clamp((int)end, 0, 23);
        ActivityType = activityType;
    }
    
    // Проверяет, попадает ли текущий час в этот слот
    public bool ContainsHour(byte hour)
    {
        // Обработка случая, когда слот пересекает полночь (например, 22:00 - 02:00)
        if (EndTimeHour < StartTimeHour)
        {
            return hour >= StartTimeHour || hour < EndTimeHour;
        }
        // Обычный случай (например, 09:00 - 17:00)
        return hour >= StartTimeHour && hour < EndTimeHour;
    }
    
    public override string ToString()
    {
        return $"[{StartTimeHour}:00-{EndTimeHour}:00] Act:{ActivityType}";
    }
}

// Компонент, содержащий расписание NPC на день
public struct Schedule : IComponentData
{
    // Ссылка на буфер с TimeSlot'ами
    public Entity TimeSlotsBufferEntity; // Entity, содержащий Buffer<TimeSlot>

    public Schedule(Entity slotsBufferEntity)
    {
        TimeSlotsBufferEntity = slotsBufferEntity;
    }
}