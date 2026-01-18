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

    // Метод для получения строкового представления (для отладки/логирования вне Burst)
    public override string ToString()
    {
        // ВНИМАНИЕ: ToString() может вызывать .ToString() на FixedString, что недопустимо в Burst.
        // Поэтому, если этот метод будет вызываться внутри Burst-системы, он тоже будет ошибкой.
        // Лучше использовать NameData как есть в системах и конвертировать в string только для Debug.Log или UI.
        return $"{FirstName} '{Nickname}' {LastName}";
    }
}