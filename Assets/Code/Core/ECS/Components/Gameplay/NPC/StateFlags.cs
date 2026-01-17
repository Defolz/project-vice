using Unity.Entities;
using System;

// Отдельный enum с FlagsAttribute
[Flags]
public enum EntityStateFlagEnum : uint
{
    None        = 0,
    Alive       = 1 << 0,  // 00000001
    Injured     = 1 << 1,  // 00000010
    Arrested    = 1 << 2,  // 00000100
    Dead        = 1 << 3,  // 00001000
    Wanted      = 1 << 4,  // 00010000
    InVehicle   = 1 << 5,  // 00100000
    Sleeping    = 1 << 6,  // 01000000
    Busy        = 1 << 7,  // 10000000
    // Можно добавить до 32 флагов (до 1 << 31)
}

// Компонент, хранящий состояние как значение enum
public struct StateFlags : IComponentData
{
    public EntityStateFlagEnum Value;

    // Свойства для удобного доступа к флагам
    public bool IsAlive
    {
        get => (Value & EntityStateFlagEnum.Alive) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Alive) : (Value & ~EntityStateFlagEnum.Alive);
    }

    public bool IsInjured
    {
        get => (Value & EntityStateFlagEnum.Injured) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Injured) : (Value & ~EntityStateFlagEnum.Injured);
    }

    public bool IsArrested
    {
        get => (Value & EntityStateFlagEnum.Arrested) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Arrested) : (Value & ~EntityStateFlagEnum.Arrested);
    }

    public bool IsDead
    {
        get => (Value & EntityStateFlagEnum.Dead) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Dead) : (Value & ~EntityStateFlagEnum.Dead);
    }

    public bool IsWanted
    {
        get => (Value & EntityStateFlagEnum.Wanted) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Wanted) : (Value & ~EntityStateFlagEnum.Wanted);
    }

    public bool IsInVehicle
    {
        get => (Value & EntityStateFlagEnum.InVehicle) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.InVehicle) : (Value & ~EntityStateFlagEnum.InVehicle);
    }

    public bool IsSleeping
    {
        get => (Value & EntityStateFlagEnum.Sleeping) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Sleeping) : (Value & ~EntityStateFlagEnum.Sleeping);
    }

    public bool IsBusy
    {
        get => (Value & EntityStateFlagEnum.Busy) != EntityStateFlagEnum.None;
        set => Value = value ? (Value | EntityStateFlagEnum.Busy) : (Value & ~EntityStateFlagEnum.Busy);
    }

    // Конструктор для установки начальных флагов
    public StateFlags(bool alive = true, bool injured = false, bool arrested = false, bool dead = false, 
                     bool wanted = false, bool inVehicle = false, bool sleeping = false, bool busy = false)
    {
        Value = EntityStateFlagEnum.None;
        IsAlive = alive;
        IsInjured = injured;
        IsArrested = arrested;
        IsDead = dead;
        IsWanted = wanted;
        IsInVehicle = inVehicle;
        IsSleeping = sleeping;
        IsBusy = busy;
    }

    // Метод для проверки, установлен ли хотя бы один из перечисленных флагов
    public bool HasAnyFlag(StateFlags other)
    {
        return (Value & other.Value) != EntityStateFlagEnum.None;
    }

    // Метод для проверки, установлены ли все переданные флаги
    public bool HasAllFlags(StateFlags other)
    {
        return (Value & other.Value) == other.Value;
    }

    public override string ToString()
    {
        var activeStates = "";
        if (IsAlive) activeStates += "Alive, ";
        if (IsInjured) activeStates += "Injured, ";
        if (IsArrested) activeStates += "Arrested, ";
        if (IsDead) activeStates += "Dead, ";
        if (IsWanted) activeStates += "Wanted, ";
        if (IsInVehicle) activeStates += "InVehicle, ";
        if (IsSleeping) activeStates += "Sleeping, ";
        if (IsBusy) activeStates += "Busy, ";

        if (activeStates.Length > 2)
            activeStates = activeStates.Substring(0, activeStates.Length - 2); // Удалить последнюю запятую и пробел

        return string.IsNullOrEmpty(activeStates) ? "NoFlags" : activeStates;
    }
}