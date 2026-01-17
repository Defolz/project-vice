using Unity.Entities;

public struct Faction : IComponentData
{
    public int Value; // Уникальный ID фракции (например, 1 = Families, 2 = Colombians, 3 = FBI)
    
    public static readonly Faction Invalid = new Faction { Value = 0 };
    public static readonly Faction Families = new Faction { Value = 1 };
    public static readonly Faction Colombians = new Faction { Value = 2 };
    public static readonly Faction FBI = new Faction { Value = 3 };
    public static readonly Faction Police = new Faction { Value = 4 };
    public static readonly Faction Civilians = new Faction { Value = 5 };

    public bool IsValid => Value != 0;
    
    public Faction(int value)
    {
        Value = value;
    }
    
    public override string ToString()
    {
        return Value switch
        {
            1 => "Families",
            2 => "Colombians", 
            3 => "FBI",
            4 => "Police",
            5 => "Civilians",
            _ => $"Unknown({Value})"
        };
    }
}