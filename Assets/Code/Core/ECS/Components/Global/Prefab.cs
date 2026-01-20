using Unity.Entities;

// Компонент-маркер для обозначения Entity как префаба
// Префабы не должны участвовать в обычной логике игры
public struct Prefab : IComponentData
{
}
