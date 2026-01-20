using UnityEditor;
using UnityEngine;

/// <summary>
/// Главное окно отладки. Меню: VICE > Debug Window
/// </summary>
public class ViceDebugWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabs = { "Overview", "Chunks", "NPCs", "Navigation" };
    
    private ViceDebugDataCollector dataCollector;
    private ViceDebugUIDrawer uiDrawer;
    
    [MenuItem("VICE/Debug Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<ViceDebugWindow>("VICE Debug");
        window.minSize = new Vector2(500, 400);
    }
    
    private void OnEnable()
    {
        dataCollector = new ViceDebugDataCollector();
        uiDrawer = new ViceDebugUIDrawer();
        EditorApplication.update += OnUpdate;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;
    }
    
    private void OnUpdate()
    {
        if (dataCollector.ShouldRefresh())
        {
            dataCollector.Refresh();
            Repaint();
        }
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        try
        {
            switch (selectedTab)
            {
                case 0: uiDrawer.DrawOverview(dataCollector.OverviewData); break;
                case 1: uiDrawer.DrawChunks(dataCollector.ChunkData); break;
                case 2: uiDrawer.DrawNPCs(dataCollector.NPCData); break;
                case 3: uiDrawer.DrawNavigation(dataCollector.NavigationData); break;
            }
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
        GUILayout.FlexibleSpace();
        
        dataCollector.AutoRefresh = GUILayout.Toggle(dataCollector.AutoRefresh, "Auto", 
            EditorStyles.toolbarButton, GUILayout.Width(50));
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            dataCollector.Refresh();
        
        EditorGUILayout.EndHorizontal();
    }
}
