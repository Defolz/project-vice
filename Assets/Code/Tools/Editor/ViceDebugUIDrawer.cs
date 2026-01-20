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
                    var world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated)
                    {
                        Debug.LogWarning("World not available");
                        return;
                    }
                    
                    var em = world.EntityManager;
                    if (!em.Exists(capturedEntity))
                    {
                        Debug.LogWarning("NPC Entity no longer exists");
                        return;
                    }
                    
                    if (!em.HasComponent<NPCId>(capturedEntity))
                    {
                        Debug.LogWarning("Entity is not an NPC");
                        return;
                    }
                    
                    var npcId = em.GetComponentData<NPCId>(capturedEntity);
                    var location = em.GetComponentData<Location>(capturedEntity);
                    var name = em.GetComponentData<NameData>(capturedEntity);
                    var faction = em.GetComponentData<Faction>(capturedEntity);
                    var traits = em.GetComponentData<Traits>(capturedEntity);
                    var state = em.GetComponentData<StateFlags>(capturedEntity);
                    
                    Debug.Log($"=== NPC INSPECTION ===\n" +
                             $"ID: {npcId.Value} (Seed: {npcId.GenerationSeed})\n" +
                             $"Name: {name.FirstName} {name.LastName} ({name.Nickname})\n" +
                             $"Position: {location.GlobalPosition2D} | Chunk [{location.ChunkId.x},{location.ChunkId.y}]\n" +
                             $"Faction: {faction.Value}\n" +
                             $"Traits: Aggression: {traits.Aggression:F2}, Loyalty: {traits.Loyalty:F2}, Intelligence: {traits.Intelligence:F2}\n" +
                             $"State: Alive: {state.IsAlive}, Injured: {state.IsInjured}, Wanted: {state.IsWanted}\n" +
                             $"Entity: {capturedEntity.Index}:{capturedEntity.Version}");
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
