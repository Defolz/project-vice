using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Отрисовывает UI для окна отладки
/// </summary>
public class DebugUIDrawer
{
    public void DrawOverview(OverviewData data)
    {
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
        DrawBox($"Game Day: {data.GameDay}, Hour: {data.GameHour:D2}:00");
        DrawBox($"FPS: {data.FPS:F0}", GetFPSColor(data.FPS));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
        DrawBox($"Total: {data.TotalEntities}");
        DrawBox($"Chunks: {data.TotalChunks}");
        DrawBox($"NPCs: {data.TotalNPCs}");
    }
    
    public void DrawChunks(ChunkData data)
    {
        EditorGUILayout.Space(5);
        
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Chunks: {data.Chunks.Count}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        data.SearchFilter = EditorGUILayout.TextField(data.SearchFilter, GUILayout.Width(200));
        if (GUILayout.Button("×", GUILayout.Width(25))) data.SearchFilter = "";
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();
        
        // Filter chunks
        var filtered = data.Chunks.Where(c => 
            string.IsNullOrEmpty(data.SearchFilter) || 
            c.Id.ToString().Contains(data.SearchFilter) ||
            c.State.ToString().ToLower().Contains(data.SearchFilter.ToLower())
        ).ToList();
        
        // Draw chunks
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
                Debug.Log($"Chunk [{chunk.Id.x},{chunk.Id.y}] - State: {chunk.State}, NPCs: {chunk.NPCCount}, Pos: {chunk.Position}");
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        if (filtered.Count == 0)
            EditorGUILayout.HelpBox("No chunks found", MessageType.Info);
    }
    
    public void DrawNPCs(NPCData data)
    {
        EditorGUILayout.Space(5);
        
        // Header & filters
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"NPCs: {data.NPCs.Count}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        data.SearchFilter = EditorGUILayout.TextField(data.SearchFilter, GUILayout.Width(150));
        if (GUILayout.Button("×", GUILayout.Width(25))) data.SearchFilter = "";
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        data.ShowOnlyAlive = EditorGUILayout.Toggle("Alive Only", data.ShowOnlyAlive, GUILayout.Width(120));
        EditorGUILayout.LabelField("Faction:", GUILayout.Width(55));
        data.FactionFilter = EditorGUILayout.IntField(data.FactionFilter, GUILayout.Width(50));
        if (GUILayout.Button("All", GUILayout.Width(40))) data.FactionFilter = -1;
        EditorGUILayout.EndHorizontal();
        
        DrawSeparator();
        
        // Filter NPCs
        var filtered = data.NPCs.Where(n => 
            (!data.ShowOnlyAlive || n.IsAlive) &&
            (data.FactionFilter == -1 || n.Faction == data.FactionFilter) &&
            (string.IsNullOrEmpty(data.SearchFilter) || 
             n.Name.ToLower().Contains(data.SearchFilter.ToLower()) ||
             n.Id.ToString().Contains(data.SearchFilter))
        ).ToList();
        
        // Draw NPCs
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
                NPCInspector.Inspect(npc.Entity);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Status line
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
            default: return new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
