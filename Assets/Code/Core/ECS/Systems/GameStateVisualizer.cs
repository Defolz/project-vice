using Unity.Entities;
using UnityEngine;

public class GameStateVisualizer : MonoBehaviour
{
    private EntityManager entityManager;
    private float lastTimeSeconds = 0f;
    private GameStateType lastGameState = GameStateType.Unknown;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (entityManager != null)
        {
            // Получаем GameStateComponent напрямую
            var gameStateQuery = entityManager.CreateEntityQuery(typeof(GameStateComponent));
            if (gameStateQuery.CalculateEntityCount() > 0)
            {
                var gameState = entityManager.GetComponentData<GameStateComponent>(gameStateQuery.GetSingletonEntity());
                if (lastGameState != gameState.Value)
                {
                    Debug.Log($"GameState: {gameState.Value}, Paused: {gameState.IsTimePaused}");
                    lastGameState = gameState.Value;
                }
            }

            // Получаем GameTimeComponent напрямую
            var gameTimeQuery = entityManager.CreateEntityQuery(typeof(GameTimeComponent));
            if (gameTimeQuery.CalculateEntityCount() > 0)
            {
                var gameTime = entityManager.GetComponentData<GameTimeComponent>(gameTimeQuery.GetSingletonEntity());
                if (lastTimeSeconds != gameTime.TotalSeconds)
                {
                    //Debug.Log($"Time: {gameTime.Day} day, {gameTime.Hour}:{gameTime.Minute:D2}");
                    lastTimeSeconds = gameTime.TotalSeconds;
                }
            }
        }
    }
}