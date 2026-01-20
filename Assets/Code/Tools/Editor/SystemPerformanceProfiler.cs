using UnityEditor;
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class SystemPerformanceProfiler : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<string, SystemPerformanceData> systemData = new Dictionary<string, SystemPerformanceData>();
    private List<SystemPerformanceData> sortedSystemData = new List<SystemPerformanceData>();
    private SortMode currentSortMode = SortMode.ByAverageTime;
    private bool autoRefresh = true;
    private float refreshInterval = 1.0f;
    private double lastRefreshTime;
    private int frameCount = 0;
    private const int FRAME_SAMPLE_SIZE = 60;
    
    [MenuItem("VICE/Performance Profiler")]
    public static void ShowWindow()
    {
        var window = GetWindow<SystemPerformanceProfiler>("System Profiler");
        window.minSize = new Vector2(600, 400);
    }
    
    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }
    
    private void OnEditorUpdate()
    {
        if (!Application.isPlaying) return;
        
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
        {
            CollectPerformanceData();
            lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }
    
    private void CollectPerformanceData()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        frameCount++;
        
        // Get all systems
        var systems = world.Systems;
        foreach (var system in systems)
        {
            string systemName = system.GetType().Name;
            
            if (!systemData.ContainsKey(systemName))
            {
                systemData[systemName] = new SystemPerformanceData
                {
                    SystemName = systemName,
                    Samples = new List<float>()
                };
            }
            
            // In real implementation, you would measure actual execution time
            // This is a placeholder - Unity DOTS has built-in profiling you can tap into
            float executionTime = UnityEngine.Random.Range(0f, 2f); // Placeholder
            
            var data = systemData[systemName];
            data.Samples.Add(executionTime);
            
            if (data.Samples.Count > FRAME_SAMPLE_SIZE)
            {
                data.Samples.RemoveAt(0);
            }
            
            data.LastUpdateTime = executionTime;
            data.AverageTime = data.Samples.Average();
            data.MinTime = data.Samples.Min();
            data.MaxTime = data.Samples.Max();
            data.TotalFrames = frameCount;
        }
        
        SortSystemData();
    }
    
    private void SortSystemData()
    {
        sortedSystemData = systemData.Values.ToList();
        
        switch (currentSortMode)
        {
            case SortMode.ByName:
                sortedSystemData.Sort((a, b) => string.Compare(a.SystemName, b.SystemName));
                break;
            case SortMode.ByAverageTime:
                sortedSystemData.Sort((a, b) => b.AverageTime.CompareTo(a.AverageTime));
                break;
            case SortMode.ByMaxTime:
                sortedSystemData.Sort((a, b) => b.MaxTime.CompareTo(a.MaxTime));
                break;
            case SortMode.ByLastTime:
                sortedSystemData.Sort((a, b) => b.LastUpdateTime.CompareTo(a.LastUpdateTime));
                break;
        }
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        DrawStatistics();
        
        EditorGUILayout.Space(5);
        DrawSeparator();
        
        DrawSystemList();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("System Performance Profiler", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
        currentSortMode = (SortMode)EditorGUILayout.EnumPopup(currentSortMode, EditorStyles.toolbarDropDown, GUILayout.Width(120));
        if (GUI.changed) SortSystemData();
        
        GUILayout.Space(10);
        
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(50));
        
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            systemData.Clear();
            frameCount = 0;
        }
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            CollectPerformanceData();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawStatistics()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to collect performance data", MessageType.Info);
            return;
        }
        
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Systems Tracked: {systemData.Count}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Frames Sampled: {frameCount}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        if (systemData.Count > 0)
        {
            float totalAvgTime = systemData.Values.Sum(s => s.AverageTime);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Total Average Time: {totalAvgTime:F2} ms", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawSystemList()
    {
        if (sortedSystemData.Count == 0) return;
        
        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("System Name", EditorStyles.boldLabel, GUILayout.Width(250));
        GUILayout.Label("Last (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Avg (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Min (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Max (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Graph", EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var system in sortedSystemData)
        {
            DrawSystemRow(system);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawSystemRow(SystemPerformanceData system)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        // System name
        GUILayout.Label(system.SystemName, GUILayout.Width(250));
        
        // Last time with color coding
        var lastColor = GetPerformanceColor(system.LastUpdateTime);
        var prevColor = GUI.backgroundColor;
        GUI.backgroundColor = lastColor;
        GUILayout.Label($"{system.LastUpdateTime:F2}", EditorStyles.miniButton, GUILayout.Width(80));
        GUI.backgroundColor = prevColor;
        
        // Average time
        var avgColor = GetPerformanceColor(system.AverageTime);
        GUI.backgroundColor = avgColor;
        GUILayout.Label($"{system.AverageTime:F2}", EditorStyles.miniButton, GUILayout.Width(80));
        GUI.backgroundColor = prevColor;
        
        // Min time
        GUILayout.Label($"{system.MinTime:F2}", GUILayout.Width(80));
        
        // Max time
        GUILayout.Label($"{system.MaxTime:F2}", GUILayout.Width(80));
        
        // Mini graph
        Rect graphRect = GUILayoutUtility.GetRect(150, 20);
        DrawMiniGraph(graphRect, system.Samples);
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawMiniGraph(Rect rect, List<float> samples)
    {
        if (samples.Count < 2) return;
        
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
        
        float maxValue = samples.Max();
        if (maxValue == 0) maxValue = 1f;
        
        Vector3[] points = new Vector3[samples.Count];
        for (int i = 0; i < samples.Count; i++)
        {
            float x = rect.x + (i / (float)(samples.Count - 1)) * rect.width;
            float normalizedValue = samples[i] / maxValue;
            float y = rect.y + rect.height - (normalizedValue * rect.height);
            points[i] = new Vector3(x, y, 0);
        }
        
        Handles.BeginGUI();
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(2f, points);
        Handles.EndGUI();
    }
    
    private Color GetPerformanceColor(float timeMs)
    {
        if (timeMs < 0.5f) return new Color(0.5f, 1f, 0.5f); // Green - Good
        if (timeMs < 1.0f) return new Color(1f, 1f, 0.5f);   // Yellow - OK
        return new Color(1f, 0.5f, 0.5f);                    // Red - Slow
    }
    
    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
    
    private class SystemPerformanceData
    {
        public string SystemName;
        public float LastUpdateTime;
        public float AverageTime;
        public float MinTime;
        public float MaxTime;
        public int TotalFrames;
        public List<float> Samples;
    }
    
    private enum SortMode
    {
        ByName,
        ByAverageTime,
        ByMaxTime,
        ByLastTime
    }
}
