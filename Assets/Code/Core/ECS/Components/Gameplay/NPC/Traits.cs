using Unity.Entities;
using Unity.Mathematics;

// Структура для хранения основных черт характера NPC, влияющих на поведение AI
public struct Traits : IComponentData
{
    // Агрессия: 0.0f - пассивный, 1.0f - крайне агрессивный
    public float Aggression;
    
    // Лояльность: 0.0f - легко предаст, 1.0f - готов умереть за семью/босса
    public float Loyalty;
    
    // Уровень тревожности: 0.0f - всегда спокоен, 1.0f - параноик
    public float Anxiety;
    
    // Интеллект: 0.0f - туповат, 1.0f - стратегический ум
    public float Intelligence;
    
    // Жадность: 0.0f - щедрый, 1.0f - жадный до денег/власти
    public float Greed;
    
    // Смелость: 0.0f - трус, 1.0f - герой/безрассудный
    public float Bravery;

    // Конструктор для удобного создания с заданными значениями
    public Traits(float aggression = 0.5f, float loyalty = 0.5f, float anxiety = 0.3f, 
                  float intelligence = 0.5f, float greed = 0.5f, float bravery = 0.5f)
    {
        Aggression = math.clamp(aggression, 0.0f, 1.0f);
        Loyalty = math.clamp(loyalty, 0.0f, 1.0f);
        Anxiety = math.clamp(anxiety, 0.0f, 1.0f);
        Intelligence = math.clamp(intelligence, 0.0f, 1.0f);
        Greed = math.clamp(greed, 0.0f, 1.0f);
        Bravery = math.clamp(bravery, 0.0f, 1.0f);
    }

    // Метод для "смешивания" характеристик двух NPC, например, для генерации потомства или рекрутов
    public static Traits Blend(Traits trait1, Traits trait2, float blendFactor)
    {
        blendFactor = math.clamp(blendFactor, 0.0f, 1.0f);
        return new Traits
        {
            Aggression = math.lerp(trait1.Aggression, trait2.Aggression, blendFactor),
            Loyalty = math.lerp(trait1.Loyalty, trait2.Loyalty, blendFactor),
            Anxiety = math.lerp(trait1.Anxiety, trait2.Anxiety, blendFactor),
            Intelligence = math.lerp(trait1.Intelligence, trait2.Intelligence, blendFactor),
            Greed = math.lerp(trait1.Greed, trait2.Greed, blendFactor),
            Bravery = math.lerp(trait1.Bravery, trait2.Bravery, blendFactor)
        };
    }
    
    // Перегрузка ToString для удобства отладки
    public override string ToString()
    {
        return $"Traits(A:{Aggression:F2}, L:{Loyalty:F2}, X:{Anxiety:F2}, I:{Intelligence:F2}, G:{Greed:F2}, B:{Bravery:F2})";
    }
}