using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

public struct NameData : IComponentData
{
    public FixedString32Bytes FirstName;
    public FixedString32Bytes LastName;
    public FixedString32Bytes Nickname; // Прозвище, может быть пустым
    
    public NameData(string firstName, string lastName, string nickname = "")
    {
        FirstName = new FixedString32Bytes(firstName);
        LastName = new FixedString32Bytes(lastName);
        Nickname = new FixedString32Bytes(nickname);
    }
    
    public FixedString64Bytes GetFullName()
    {
        if (Nickname.IsEmpty)
            return $"{FirstName} {LastName}";
        
        return $"{FirstName} \"{Nickname}\" {LastName}";
    }
    
    public override string ToString()
    {
        return GetFullName().ToString();
    }
}