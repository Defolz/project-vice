using Unity.Entities;
using Unity.Collections;

public struct NameData : IComponentData
{
    public FixedString128Bytes FirstName; // Изменено на FixedString128Bytes
    public FixedString128Bytes LastName;  // Изменено на FixedString128Bytes
    public FixedString128Bytes Nickname;  // Изменено на FixedString128Bytes

    public NameData(FixedString128Bytes firstName, FixedString128Bytes lastName, FixedString128Bytes nickname)
    {
        FirstName = firstName;
        LastName = lastName;
        Nickname = nickname;
    }

    // Метод для получения строкового представления (ТОЛЬКО для отладки/логирования вне Burst)
    // НИКОГДА не вызывайте этот метод внутри Burst-систем!
    public string ToDebugString()
    {
        // Используем отдельный метод вместо ToString() для явного указания,
        // что это НЕ должно использоваться в Burst-коде
        return $"{FirstName} '{Nickname}' {LastName}";
    }
}