using Unity.Entities;

// Enum для типов фракций
public enum FactionType : byte
{
    Invalid = 0,
    Families = 1,
    Colombians = 2,
    FBI = 3,
    Police = 4,
    Civilians = 5
}

public struct Faction : IComponentData
{
    public FactionType Type;
    
    public static readonly Faction Invalid = new Faction { Type = FactionType.Invalid };
    public static readonly Faction Families = new Faction { Type = FactionType.Families };
    public static readonly Faction Colombians = new Faction { Type = FactionType.Colombians };
    public static readonly Faction FBI = new Faction { Type = FactionType.FBI };
    public static readonly Faction Police = new Faction { Type = FactionType.Police };
    public static readonly Faction Civilians = new Faction { Type = FactionType.Civilians };

    public bool IsValid => Type != FactionType.Invalid;
    
    // Конструктор из enum
    public Faction(FactionType type)
    {
        Type = type;
    }
    
    // Конструктор из int (для обратной совместимости)
    public Faction(int value)
    {
        Type = (FactionType)value;
    }
    
    // Для обратной совместимости
    public int Value => (int)Type;
    
    public override string ToString()
    {
        return Type switch
        {
            FactionType.Families => "Families",
            FactionType.Colombians => "Colombians",
            FactionType.FBI => "FBI",
            FactionType.Police => "Police",
            FactionType.Civilians => "Civilians",
            _ => $"Unknown({(int)Type})"
        };
    }
}