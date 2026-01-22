using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Entities;

/// <summary>
/// Отрисовывает UI для окна отладки
/// </summary>
public class ViceDebugUIDrawer
{
    public void DrawOverview(ViceOverviewData data)
    {
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
        DrawBox($"Game Time: Day {data.GameDay}, {data.GameHour:D2}:{data.GameMinute:D2}");
        DrawBox($"FPS: {data.FPS:F0}", GetFPSColor(data.FPS));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
        DrawBox($"Total: {data.TotalEntities}");
        DrawBox($"Chunks: {data.TotalChunks}");
        DrawBox($"NPCs: {data.TotalNPCs}");
    }
    
    public void DrawTimeControl()
    {
        EditorGUILayout.Space(10);
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            EditorGUILayout.HelpBox("World not available. Enter Play Mode to control time.", MessageType.Warning);
            return;
        }
        
        var em = world.EntityManager;
        
        // Получаем GameTimeComponent
        var query = em.CreateEntityQuery(typeof(GameTimeComponent));
        if (query.CalculateEntityCount() == 0)
        {
            EditorGUILayout.HelpBox("GameTimeComponent not found. Make sure the game has started.", MessageType.Warning);
            query.Dispose();
            return;
        }
        
        var gameTimeEntity = query.GetSingletonEntity();
        var gameTime = em.GetComponentData<GameTimeComponent>(gameTimeEntity);
        
        // === Текущее время ===
        EditorGUILayout.LabelField("Current Game Time", EditorStyles.boldLabel);
        DrawBox($"Day {gameTime.Day}, {gameTime.Hour:D2}:{gameTime.Minute:D2}", new Color(0.3f, 0.6f, 1f));
        DrawBox($"Total Seconds: {gameTime.TotalSeconds:F1}");
        DrawBox($"Time Scale: {gameTime.TimeScale:F2}x");
        
        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);
        
        // === Контроль времени ===
        EditorGUILayout.LabelField("Time Control", EditorStyles.boldLabel);
        
        // Time Scale
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(100));
        
        if (GUILayout.Button("0.5x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 0.5f);
        if (GUILayout.Button("1x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 1f);
        if (GUILayout.Button("2x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 2f);
        if (GUILayout.Button("5x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 5f);
        if (GUILayout.Button("10x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 10f);
        if (GUILayout.Button("50x", GUILayout.Width(60)))
            SetTimeScale(em, gameTimeEntity, 50f);
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Custom Time Scale Slider
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Custom:", GUILayout.Width(100));
        var newScale = EditorGUILayout.Slider(gameTime.TimeScale, 0.1f, 100f);
        if (!Mathf.Approximately(newScale, gameTime.TimeScale))
        {
            SetTimeScale(em, gameTimeEntity, newScale);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);
        
        // === Быстрые переходы ===
        EditorGUILayout.LabelField("Quick Time Set", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("☀️ Morning (6:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 6, 0);
        if (GUILayout.Button("🌇 Work (8:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 8, 0);
        if (GUILayout.Button("🍔 Lunch (12:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 12, 0);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌆 Evening (18:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 18, 0);
        if (GUILayout.Button("🌃 Night (22:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 22, 0);
        if (GUILayout.Button("🌙 Midnight (00:00)", GUILayout.Height(30)))
            SetGameTime(em, gameTimeEntity, gameTime.Day, 0, 0);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);
        
        // === Ручное управление ===
        EditorGUILayout.LabelField("Manual Time Set", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Day:", GUILayout.Width(50));
        var newDay = EditorGUILayout.IntField(gameTime.Day, GUILayout.Width(60));
        if (newDay != gameTime.Day)
        {
            SetGameTime(em, gameTimeEntity, Mathf.Max(0, newDay), gameTime.Hour, gameTime.Minute);
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Hour:", GUILayout.Width(50));
        var newHour = EditorGUILayout.IntSlider(gameTime.Hour, 0, 23, GUILayout.Width(200));
        if (newHour != gameTime.Hour)
        {
            SetGameTime(em, gameTimeEntity, gameTime.Day, newHour, gameTime.Minute);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Minute:", GUILayout.Width(50));
        var newMinute = EditorGUILayout.IntSlider(gameTime.Minute, 0, 59, GUILayout.Width(260));
        if (newMinute != gameTime.Minute)
        {
            SetGameTime(em, gameTimeEntity, gameTime.Day, gameTime.Hour, newMinute);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);
        
        // === Пауза времени ===
        var stateQuery = em.CreateEntityQuery(typeof(GameStateComponent));
        if (stateQuery.CalculateEntityCount() > 0)
        {
            var stateEntity = stateQuery.GetSingletonEntity();
            var gameState = em.GetComponentData<GameStateComponent>(stateEntity);
            
            EditorGUILayout.LabelField("Time Pause", EditorStyles.boldLabel);
            
            var isPaused = gameState.IsTimePaused;
            var pausedColor = isPaused ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
            
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = pausedColor;
            
            var pauseText = isPaused ? "▶️ Resume Time" : "⏸️ Pause Time";
            if (GUILayout.Button(pauseText, GUILayout.Height(40)))
            {
                var newState = new GameStateComponent { IsTimePaused = !isPaused };
                em.SetComponentData(stateEntity, newState);
            }
            
            GUI.backgroundColor = prev;
            
            if (isPaused)
            {
                EditorGUILayout.HelpBox("⏸️ Time is PAUSED. Click Resume to continue.", MessageType.Warning);
            }
        }
        stateQuery.Dispose();
        
        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);
        
        // === Информация ===
        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "• Time Scale affects NPC behavior cycle\n" +
            "• 6:00 - Wake up\n" +
            "• 8:00-18:00 - Work hours (Civilians, Police)\n" +
            "• 22:00-6:00 - Sleep time\n" +
            "• Use high Time Scale (50x+) to see daily cycles quickly",
            MessageType.Info
        );
        
        query.Dispose();
    }
    
    private void SetTimeScale(EntityManager em, Entity gameTimeEntity, float scale)
    {
        var gameTime = em.GetComponentData<GameTimeComponent>(gameTimeEntity);
        gameTime.TimeScale = scale;
        em.SetComponentData(gameTimeEntity, gameTime);
        Debug.Log($"<color=cyan>Time Scale set to {scale:F2}x</color>");
    }
    
    private void SetGameTime(EntityManager em, Entity gameTimeEntity, int day, int hour, int minute)
    {
        var totalMinutes = (day * 24 * 60) + (hour * 60) + minute;
        var totalSeconds = totalMinutes * 60f;
        
        var gameTime = em.GetComponentData<GameTimeComponent>(gameTimeEntity);
        var newTime = new GameTimeComponent
        {
            TotalSeconds = totalSeconds,
            Day = day,
            Hour = hour,
            Minute = minute,
            TimeScale = gameTime.TimeScale
        };
        
        em.SetComponentData(gameTimeEntity, newTime);
        Debug.Log($"<color=cyan>Game time set to Day {day}, {hour:D2}:{minute:D2}</color>");
    }
    
    public void DrawChunks(ViceChunkData data)
    {
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Chunks: {data.Chunks.Count}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        data.SearchFilter = EditorGUILayout.TextField(data.SearchFilter, GUILayout.Width(200));
        if (GUILayout.Button("×", GUILayout.Width(25))) 
            data.SearchFilter = "";
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();
        
        var filtered = data.Chunks.Where(c => 
            string.IsNullOrEmpty(data.SearchFilter) || 
            c.Id.ToString().Contains(data.SearchFilter) ||
            c.State.ToString().ToLower().Contains(data.SearchFilter.ToLower())
        ).ToList();
        
        foreach (var chunk in filtered)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            var color = GetChunkColor(chunk.State);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.LabelField($"[{chunk.Id.x},{chunk.Id.y}]", EditorStyles.boldLabel, GUILayout.Width(70));
            GUI.backgroundColor = prev;
            
            EditorGUILayout.LabelField($"{chunk.State}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"NPCs: {chunk.NPCCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Pos: ({chunk.Position.x:F0},{chunk.Position.y:F0})", GUILayout.Width(120));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Info", GUILayout.Width(50)))
            {
                var c = chunk;
                EditorApplication.delayCall += () => LogChunkInfo(c);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        if (filtered.Count == 0)
            EditorGUILayout.HelpBox("No chunks found", MessageType.Info);
    }
    
    public void DrawNPCs(ViceNPCData data)
    {
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"NPCs: {data.NPCs.Count}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        data.SearchFilter = EditorGUILayout.TextField(data.SearchFilter, GUILayout.Width(150));
        if (GUILayout.Button("×", GUILayout.Width(25))) 
            data.SearchFilter = "";
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        data.ShowOnlyAlive = EditorGUILayout.Toggle("Alive Only", data.ShowOnlyAlive, GUILayout.Width(120));
        EditorGUILayout.LabelField("Faction:", GUILayout.Width(55));
        data.FactionFilter = EditorGUILayout.IntField(data.FactionFilter, GUILayout.Width(50));
        if (GUILayout.Button("All", GUILayout.Width(40))) 
            data.FactionFilter = -1;
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();
        
        var filtered = data.NPCs.Where(n => 
            (!data.ShowOnlyAlive || n.IsAlive) &&
            (data.FactionFilter == -1 || n.Faction == data.FactionFilter) &&
            (string.IsNullOrEmpty(data.SearchFilter) || 
             n.Name.ToLower().Contains(data.SearchFilter.ToLower()) ||
             n.Id.ToString().Contains(data.SearchFilter))
        ).ToList();
        
        foreach (var npc in filtered)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            var color = npc.IsAlive ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.LabelField(npc.Name, EditorStyles.boldLabel, GUILayout.Width(150));
            GUI.backgroundColor = prev;
            
            EditorGUILayout.LabelField($"ID: {npc.Id}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"F:{npc.Faction}", GUILayout.Width(40));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Inspect", GUILayout.Width(60)))
            {
                var capturedEntity = npc.Entity;
                EditorApplication.delayCall += () =>
                {
                    NPCInspectorWindow.ShowWindow(capturedEntity);
                };
            }
            
            EditorGUILayout.EndHorizontal();
            
            string status = "";
            if (!npc.IsAlive) status += "Dead ";
            if (npc.IsWanted) status += "Wanted ";
            if (npc.IsInjured) status += "Injured ";
            if (string.IsNullOrEmpty(status)) status = "Normal";
            
            EditorGUILayout.LabelField($"Chunk: [{npc.ChunkId.x},{npc.ChunkId.y}] | Status: {status.Trim()}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        if (filtered.Count == 0)
            EditorGUILayout.HelpBox("No NPCs found", MessageType.Info);
    }
    
    private void DrawBox(string text, Color? bgColor = null)
    {
        var prev = GUI.backgroundColor;
        if (bgColor.HasValue) GUI.backgroundColor = bgColor.Value;
        
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField(text);
        EditorGUILayout.EndHorizontal();
        
        GUI.backgroundColor = prev;
    }
    
    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(5);
    }
    
    private void LogChunkInfo(ViceChunkInfo chunk)
    {
        Debug.Log($"=== CHUNK INFO ===\n" +
                  $"ID: [{chunk.Id.x},{chunk.Id.y}]\n" +
                  $"State: {chunk.State}\n" +
                  $"Position: {chunk.Position}\n" +
                  $"NPCs: {chunk.NPCCount}\n" +
                  $"Entity: {chunk.Entity.Index}:{chunk.Entity.Version}");
    }
    
    private Color GetFPSColor(float fps)
    {
        if (fps >= 50) return new Color(0.5f, 1f, 0.5f);
        if (fps >= 30) return new Color(1f, 1f, 0.5f);
        return new Color(1f, 0.5f, 0.5f);
    }
    
    private Color GetChunkColor(ChunkState state)
    {
        switch (state)
        {
            case ChunkState.Loaded: return new Color(0.5f, 1f, 0.5f);
            case ChunkState.Generating: return new Color(1f, 1f, 0.5f);
            case ChunkState.Dirty: return new Color(1f, 0.5f, 0.5f);
            case ChunkState.Unloading: return new Color(1f, 0.5f, 1f);
            case ChunkState.Unloaded: return new Color(0.5f, 0.5f, 0.5f);
            default: return new Color(0.7f, 0.7f, 0.7f);
        }
    }
    
    public void DrawNavigation(ViceNavigationData data)
    {
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Navigation Grid Overview", EditorStyles.boldLabel);
        DrawBox($"Total Chunks: {data.Chunks.Count}");
        DrawBox($"Total Walkable Cells: {data.TotalWalkableCells:N0}");
        DrawBox($"Total Blocked Cells: {data.TotalBlockedCells:N0}");
        DrawBox($"Total Obstacles: {data.TotalObstacles}");
        DrawBox($"Memory Usage: {data.TotalMemoryKB:F2} KB");
        
        if (data.Chunks.Count > 0)
        {
            var avgWalkable = data.Chunks.Average(c => c.WalkablePercentage);
            DrawBox($"Average Walkable: {avgWalkable:F1}%", GetWalkableColor(avgWalkable));
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Chunks: {data.Chunks.Count}", EditorStyles.boldLabel);
        DrawSeparator();
        
        foreach (var chunk in data.Chunks)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            var color = GetWalkableColor(chunk.WalkablePercentage);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = color;
            EditorGUILayout.LabelField($"[{chunk.ChunkId.x},{chunk.ChunkId.y}]", EditorStyles.boldLabel, GUILayout.Width(70));
            GUI.backgroundColor = prev;
            
            EditorGUILayout.LabelField($"{chunk.WalkablePercentage:F1}% walkable", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Obstacles: {chunk.ObstacleCount}", GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Details", GUILayout.Width(60)))
            {
                var c = chunk;
                EditorApplication.delayCall += () => LogNavigationInfo(c);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField(
                $"Walkable: {chunk.WalkableCells} | Blocked: {chunk.BlockedCells}", 
                EditorStyles.miniLabel
            );
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        if (data.Chunks.Count == 0)
            EditorGUILayout.HelpBox("No navigation grids found. Make sure chunks are loaded.", MessageType.Info);
    }
    
    private Color GetWalkableColor(float percentage)
    {
        if (percentage >= 80) return new Color(0.5f, 1f, 0.5f);
        if (percentage >= 50) return new Color(1f, 1f, 0.5f);
        if (percentage >= 20) return new Color(1f, 0.7f, 0.3f);
        return new Color(1f, 0.5f, 0.5f);
    }
    
    private void LogNavigationInfo(ViceNavigationChunkInfo chunk)
    {
        Debug.Log($"=== NAVIGATION CHUNK INFO ===\n" +
                  $"Chunk ID: [{chunk.ChunkId.x},{chunk.ChunkId.y}]\n" +
                  $"Walkable Cells: {chunk.WalkableCells}\n" +
                  $"Blocked Cells: {chunk.BlockedCells}\n" +
                  $"Walkable %: {chunk.WalkablePercentage:F2}%\n" +
                  $"Obstacles: {chunk.ObstacleCount}");
    }
}
