using UnityEditor;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Детальное окно инспектирования NPC
/// Открывается при нажатии Inspect в ViceDebugWindow
/// </summary>
public class NPCInspectorWindow : EditorWindow
{
    private Entity npcEntity;
    private Vector2 scrollPosition;
    private float refreshInterval = 0.5f;
    private double lastRefreshTime;
    private bool autoRefresh = true;
    
    // Кэшированные данные
    private NPCInspectorData cachedData;
    
    public static void ShowWindow(Entity entity)
    {
        var window = GetWindow<NPCInspectorWindow>("NPC Inspector");
        window.minSize = new Vector2(600, 700);
        window.npcEntity = entity;
        window.RefreshData();
    }
    
    private void OnEnable()
    {
        EditorApplication.update += OnUpdate;
    }
    
    private void OnDisable()
    {
        EditorApplication.update -= OnUpdate;
    }
    
    private void OnUpdate()
    {
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
        {
            RefreshData();
            Repaint();
        }
    }
    
    private void OnGUI()
    {
        DrawToolbar();
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            EditorGUILayout.HelpBox("World not available. Enter Play Mode.", MessageType.Warning);
            return;
        }
        
        var em = world.EntityManager;
        if (!em.Exists(npcEntity))
        {
            EditorGUILayout.HelpBox("NPC Entity no longer exists.", MessageType.Error);
            if (GUILayout.Button("Close Window"))
            {
                Close();
            }
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (cachedData != null)
        {
            DrawNPCData(cachedData);
        }
        else
        {
            EditorGUILayout.HelpBox("Loading NPC data...", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        EditorGUILayout.LabelField($"Entity: {npcEntity.Index}:{npcEntity.Version}", GUILayout.Width(150));
        
        GUILayout.FlexibleSpace();
        
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
        
        EditorGUILayout.LabelField("Interval:", GUILayout.Width(50));
        refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.1f, 2f, GUILayout.Width(100));
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshData();
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void RefreshData()
    {
        lastRefreshTime = EditorApplication.timeSinceStartup;
        
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        
        var em = world.EntityManager;
        if (!em.Exists(npcEntity)) return;
        
        cachedData = new NPCInspectorData();
        
        // Basic info
        if (em.HasComponent<NPCId>(npcEntity))
            cachedData.npcId = em.GetComponentData<NPCId>(npcEntity);
        
        if (em.HasComponent<NameData>(npcEntity))
            cachedData.name = em.GetComponentData<NameData>(npcEntity);
        
        if (em.HasComponent<Location>(npcEntity))
            cachedData.location = em.GetComponentData<Location>(npcEntity);
        
        if (em.HasComponent<Faction>(npcEntity))
            cachedData.faction = em.GetComponentData<Faction>(npcEntity);
        
        if (em.HasComponent<Traits>(npcEntity))
            cachedData.traits = em.GetComponentData<Traits>(npcEntity);
        
        if (em.HasComponent<StateFlags>(npcEntity))
            cachedData.stateFlags = em.GetComponentData<StateFlags>(npcEntity);
        
        if (em.HasComponent<CurrentGoal>(npcEntity))
            cachedData.currentGoal = em.GetComponentData<CurrentGoal>(npcEntity);
        
        // Path following
        if (em.HasComponent<PathFollower>(npcEntity))
            cachedData.pathFollower = em.GetComponentData<PathFollower>(npcEntity);
        
        if (em.HasComponent<PathResult>(npcEntity))
            cachedData.pathResult = em.GetComponentData<PathResult>(npcEntity);
        
        // Waypoints
        if (em.HasBuffer<PathWaypoint>(npcEntity))
        {
            var buffer = em.GetBuffer<PathWaypoint>(npcEntity);
            cachedData.waypoints = new PathWaypoint[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                cachedData.waypoints[i] = buffer[i];
            }
        }
        
        cachedData.hasData = true;
    }
    
    private void DrawNPCData(NPCInspectorData data)
    {
        if (!data.hasData) return;
        
        EditorGUILayout.Space(10);
        
        // === HEADER ===
        DrawSection("NPC IDENTITY", () =>
        {
            DrawKeyValue("ID", data.npcId.Value.ToString());
            DrawKeyValue("Generation Seed", data.npcId.GenerationSeed.ToString());
            DrawKeyValue("First Name", data.name.FirstName.ToString());
            DrawKeyValue("Last Name", data.name.LastName.ToString());
            DrawKeyValue("Nickname", data.name.Nickname.ToString());
            DrawKeyValue("Faction", data.faction.ToString());
        });
        
        // === LOCATION ===
        DrawSection("LOCATION & POSITION", () =>
        {
            DrawKeyValue("Chunk ID", $"[{data.location.ChunkId.x}, {data.location.ChunkId.y}]");
            DrawKeyValue("Position In Chunk", $"({data.location.PositionInChunk.x:F2}, {data.location.PositionInChunk.y:F2}, {data.location.PositionInChunk.z:F2})");
            DrawKeyValue("Global Position 2D", $"({data.location.GlobalPosition2D.x:F2}, {data.location.GlobalPosition2D.y:F2})");
            DrawKeyValue("Global Position 3D", $"({data.location.GlobalPosition3D.x:F2}, {data.location.GlobalPosition3D.y:F2}, {data.location.GlobalPosition3D.z:F2})");
        });
        
        // === STATE FLAGS ===
        DrawSection("STATE FLAGS", () =>
        {
            DrawToggle("Is Alive", data.stateFlags.IsAlive);
            DrawToggle("Is Dead", data.stateFlags.IsDead);
            DrawToggle("Is Injured", data.stateFlags.IsInjured);
            DrawToggle("Is Wanted", data.stateFlags.IsWanted);
            DrawToggle("Is Arrested", data.stateFlags.IsArrested);
            DrawToggle("Is Busy", data.stateFlags.IsBusy);
            DrawToggle("Is Sleeping", data.stateFlags.IsSleeping);
            DrawToggle("Is In Vehicle", data.stateFlags.IsInVehicle);
        });
        
        // === TRAITS ===
        DrawSection("PERSONALITY TRAITS", () =>
        {
            DrawSlider("Aggression", data.traits.Aggression);
            DrawSlider("Loyalty", data.traits.Loyalty);
            DrawSlider("Anxiety", data.traits.Anxiety);
            DrawSlider("Intelligence", data.traits.Intelligence);
            DrawSlider("Greed", data.traits.Greed);
            DrawSlider("Bravery", data.traits.Bravery);
        });
        
        // === CURRENT GOAL ===
        DrawSection("CURRENT GOAL", () =>
        {
            var goalTypeName = GetGoalTypeName((int)data.currentGoal.Type);
            DrawKeyValue("Type", $"{goalTypeName} ({(int)data.currentGoal.Type})");
            DrawKeyValue("Priority", $"{data.currentGoal.Priority:P0}");
            
            if (data.currentGoal.TargetEntity != Entity.Null)
            {
                DrawKeyValue("Target Entity", $"{data.currentGoal.TargetEntity.Index}:{data.currentGoal.TargetEntity.Version}");
            }
            
            if (!data.currentGoal.TargetPosition.Equals(float3.zero))
            {
                DrawKeyValue("Target Position", $"({data.currentGoal.TargetPosition.x:F2}, {data.currentGoal.TargetPosition.y:F2}, {data.currentGoal.TargetPosition.z:F2})");
            }
            
            if (data.currentGoal.ExpiryTime > 0)
            {
                var currentTime = (float)EditorApplication.timeSinceStartup;
                var timeLeft = data.currentGoal.ExpiryTime - currentTime;
                var color = timeLeft > 0 ? Color.green : Color.red;
                DrawKeyValue("Expiry Time", $"{data.currentGoal.ExpiryTime:F1}s (Remaining: {timeLeft:F1}s)", color);
            }
            else
            {
                DrawKeyValue("Expiry Time", "Never");
            }
        });
        
        // === PATH FOLLOWING ===
        if (data.pathFollower.State != PathFollowerState.Idle)
        {
            DrawSection("PATH FOLLOWING", () =>
            {
                DrawKeyValue("State", data.pathFollower.State.ToString());
                DrawKeyValue("Speed", $"{data.pathFollower.Speed:F2} m/s");
                DrawKeyValue("Current Waypoint", $"{data.pathFollower.CurrentWaypointIndex} / {data.waypoints?.Length ?? 0}");
                DrawKeyValue("Arrival Threshold", $"{data.pathFollower.ArrivalThreshold:F2}m");
                
                if (data.pathFollower.StuckTimer > 0)
                {
                    DrawKeyValue("Stuck Timer", $"{data.pathFollower.StuckTimer:F2}s", Color.yellow);
                }
                
                if (data.waypoints != null && data.waypoints.Length > 0)
                {
                    var progress = data.pathFollower.GetProgress(data.waypoints.Length);
                    EditorGUILayout.Space(5);
                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(false, 20),
                        progress,
                        $"Progress: {progress:P0}"
                    );
                }
            });
        }
        
        // === PATH RESULT ===
        if (data.pathResult.IsValid)
        {
            DrawSection("PATH RESULT", () =>
            {
                DrawKeyValue("Status", data.pathResult.Status.ToString());
                DrawKeyValue("Total Distance", $"{data.pathResult.TotalDistance:F2}m");
                DrawKeyValue("Waypoint Count", data.pathResult.WaypointCount.ToString());
                DrawKeyValue("Calculation Time", $"{data.pathResult.CalculationTime * 1000:F2}ms");
            });
        }
        
        // === WAYPOINTS ===
        if (data.waypoints != null && data.waypoints.Length > 0)
        {
            DrawSection($"WAYPOINTS ({data.waypoints.Length})", () =>
            {
                for (int i = 0; i < Mathf.Min(data.waypoints.Length, 10); i++)
                {
                    var wp = data.waypoints[i];
                    var isCurrent = i == data.pathFollower.CurrentWaypointIndex;
                    var color = isCurrent ? new Color(0.5f, 1f, 0.5f) : Color.white;
                    
                    var prev = GUI.backgroundColor;
                    GUI.backgroundColor = color;
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"#{i}", GUILayout.Width(30));
                    EditorGUILayout.LabelField($"Pos: ({wp.Position.x:F1}, {wp.Position.y:F1})", GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Dist: {wp.Distance:F2}m");
                    if (isCurrent)
                        EditorGUILayout.LabelField("← CURRENT", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                    
                    GUI.backgroundColor = prev;
                }
                
                if (data.waypoints.Length > 10)
                {
                    EditorGUILayout.LabelField($"... and {data.waypoints.Length - 10} more waypoints", EditorStyles.miniLabel);
                }
            });
        }
        
        EditorGUILayout.Space(20);
    }
    
    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.Space(10);
        
        var style = new GUIStyle(EditorStyles.boldLabel);
        style.fontSize = 14;
        style.normal.textColor = new Color(0.3f, 0.6f, 1f);
        
        EditorGUILayout.LabelField(title, style);
        
        var rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.5f));
        
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        content();
        EditorGUILayout.EndVertical();
    }
    
    private void DrawKeyValue(string key, string value, Color? valueColor = null)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(200));
        
        if (valueColor.HasValue)
        {
            var prev = GUI.contentColor;
            GUI.contentColor = valueColor.Value;
            EditorGUILayout.LabelField(value);
            GUI.contentColor = prev;
        }
        else
        {
            EditorGUILayout.LabelField(value);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawToggle(string label, bool value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(200));
        
        var color = value ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        
        EditorGUILayout.Toggle(value, GUILayout.Width(20));
        
        GUI.backgroundColor = prev;
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawSlider(string label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUI.ProgressBar(
            EditorGUILayout.GetControlRect(false, 20),
            value,
            $"{value:P0}"
        );
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }
    
    private string GetGoalTypeName(int typeValue)
    {
        string[] names = {
            "None", "Idle", "MoveToLocation", "PatrolArea", "FollowTarget",
            "AttackTarget", "DefendLocation", "Work", "Sleep", "Eat",
            "Socialize", "Flee", "Investigate", "Retaliate", "VisitLocation", "EscortTarget"
        };
        
        return typeValue >= 0 && typeValue < names.Length ? names[typeValue] : $"Unknown({typeValue})";
    }
}

// Data structure для кэширования
public class NPCInspectorData
{
    public bool hasData;
    public NPCId npcId;
    public NameData name;
    public Location location;
    public Faction faction;
    public Traits traits;
    public StateFlags stateFlags;
    public CurrentGoal currentGoal;
    public PathFollower pathFollower;
    public PathResult pathResult;
    public PathWaypoint[] waypoints;
}
