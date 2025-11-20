using Colossal.Logging;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using OrganicNeighborhood.Data;
using OrganicNeighborhood.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace OrganicNeighborhood.Systems
{
    /// <summary>
    /// Main tool system for organic neighborhood layout generation
    /// Handles 3-point area definition (like grid tool) and creates organic road networks
    /// </summary>
    public partial class OrganicNeighborhoodToolSystem : ToolBaseSystem
    {
        /// <summary>
        /// Tool state for tracking user input progression
        /// </summary>
        private enum State
        {
            /// <summary>Waiting for first point</summary>
            WaitingForFirstPoint,
            /// <summary>First point placed, waiting for second</summary>
            WaitingForSecondPoint,
            /// <summary>Two points placed, waiting for third</summary>
            WaitingForThirdPoint,
            /// <summary>All three points placed, ready to generate</summary>
            ReadyToGenerate,
            /// <summary>Applying the layout</summary>
            Applying
        }

        // System dependencies
        private ToolOutputBarrier m_ToolOutputBarrier;
        private ToolRaycastSystem m_ToolRaycastSystem;
        private PrefabSystem m_PrefabSystem;
        private TerrainSystem m_TerrainSystem;
        private WaterSystem m_WaterSystem;
        private EntityQuery m_DefinitionQuery;
        private EntityQuery m_TempQuery;

        // Tool state
        private State m_State;
        private ControlPoint m_Point1;
        private ControlPoint m_Point2;
        private ControlPoint m_Point3;
        private bool m_ForceCancel;

        // Layout configuration
        private LayoutParameters m_LayoutParameters;
        private TerrainAwareParameters m_TerrainParameters;

        // Road prefabs for different road types
        private Entity m_ArterialPrefab;
        private Entity m_CollectorPrefab;
        private Entity m_LocalPrefab;

        // Input actions (inherited from ToolBaseSystem)
        private ProxyAction m_ApplyAction;
        private ProxyAction m_CancelAction;

        /// <summary>
        /// Tool identifier for UI and system registration
        /// </summary>
        public override string toolID => "Organic Neighborhood Tool";

        /// <summary>
        /// Initialize logging reference from main mod
        /// </summary>
        private static ILog Log => Mod.Log;

        /// <summary>
        /// Called when the system is created
        /// Initialize queries and dependencies
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            Log?.Info($"[{Mod.ModId}] OrganicNeighborhoodToolSystem.OnCreate()");

            // Get system dependencies
            m_ToolOutputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_ToolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();

            // Create queries for temporary entities
            m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CreationDefinition>(),
                    ComponentType.ReadOnly<Updated>()
                }
            });

            m_TempQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Temp>()
                }
            });

            // Initialize state
            m_State = State.WaitingForFirstPoint;
            m_LayoutParameters = LayoutParameters.Default;
            m_TerrainParameters = TerrainAwareParameters.Default;

            // Initialize road prefabs to null - will be looked up when needed
            m_ArterialPrefab = Entity.Null;
            m_CollectorPrefab = Entity.Null;
            m_LocalPrefab = Entity.Null;

            Log?.Info($"[{Mod.ModId}] OrganicNeighborhoodToolSystem created successfully");
        }

        /// <summary>
        /// Called when the tool starts (becomes active)
        /// Reset state and set up input actions
        /// </summary>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            Log?.Info($"[{Mod.ModId}] OrganicNeighborhoodToolSystem activated");

            // Reset state
            m_State = State.WaitingForFirstPoint;
            m_ForceCancel = false;

            // Set up raycast requirements
            // We need to raycast against terrain for control points
            m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements;
        }

        /// <summary>
        /// Main update loop - called every frame when tool is active
        /// </summary>
        protected override void OnUpdate()
        {
            // Handle cancellation
            if (m_ForceCancel || GetCancelInput())
            {
                HandleCancel();
                return;
            }

            // Get raycast result
            if (!GetRaycastResult(out Entity hitEntity, out RaycastHit hit))
            {
                // No valid raycast, just return
                return;
            }

            // Create control point from raycast
            ControlPoint currentPoint = new ControlPoint(hitEntity, hit);

            // Handle apply input (left click)
            if (GetApplyInput())
            {
                HandleApplyInput(currentPoint);
            }

            // Update preview visualization
            UpdatePreview(currentPoint);
        }

        /// <summary>
        /// Handle user clicking to place a point
        /// </summary>
        private void HandleApplyInput(ControlPoint point)
        {
            switch (m_State)
            {
                case State.WaitingForFirstPoint:
                    m_Point1 = point;
                    m_State = State.WaitingForSecondPoint;
                    Log?.Info($"[{Mod.ModId}] Point 1 placed at {point.m_Position}");
                    break;

                case State.WaitingForSecondPoint:
                    m_Point2 = point;
                    m_State = State.WaitingForThirdPoint;
                    Log?.Info($"[{Mod.ModId}] Point 2 placed at {point.m_Position}");
                    break;

                case State.WaitingForThirdPoint:
                    m_Point3 = point;
                    m_State = State.ReadyToGenerate;
                    Log?.Info($"[{Mod.ModId}] Point 3 placed at {point.m_Position}");
                    Log?.Info($"[{Mod.ModId}] Ready to generate layout!");

                    // Apply the layout
                    ApplyLayout();
                    break;
            }
        }

        /// <summary>
        /// Cancel current operation and reset to initial state
        /// </summary>
        private void HandleCancel()
        {
            Log?.Info($"[{Mod.ModId}] Tool cancelled, resetting state");

            // Clear temporary entities
            ClearTemporaryEntities();

            // Reset state
            m_State = State.WaitingForFirstPoint;
            m_ForceCancel = false;
        }

        /// <summary>
        /// Update preview visualization based on current state
        /// Shows markers at placed points and preview lines
        /// </summary>
        private void UpdatePreview(ControlPoint currentPoint)
        {
            // Clear previous preview entities
            ClearTemporaryEntities();

            // TODO Phase 5: Create visual markers for placed control points
            // For now, just log the state
            switch (m_State)
            {
                case State.WaitingForFirstPoint:
                    // Show cursor at current position
                    break;

                case State.WaitingForSecondPoint:
                    // Show marker at point 1, preview line to current cursor
                    break;

                case State.WaitingForThirdPoint:
                    // Show markers at points 1 & 2, preview parallelogram
                    break;

                case State.ReadyToGenerate:
                    // Show full preview of generated roads
                    break;
            }
        }

        /// <summary>
        /// Apply the organic neighborhood layout based on 3 control points
        /// Creates NetCourse entities for the road network
        /// </summary>
        private void ApplyLayout()
        {
            Log?.Info($"[{Mod.ModId}] Generating organic neighborhood layout...");
            Log?.Info($"[{Mod.ModId}]   Point 1: {m_Point1.m_Position}");
            Log?.Info($"[{Mod.ModId}]   Point 2: {m_Point2.m_Position}");
            Log?.Info($"[{Mod.ModId}]   Point 3: {m_Point3.m_Position}");
            Log?.Info($"[{Mod.ModId}]   Style: {m_LayoutParameters.m_Style}");

            // Calculate area dimensions for logging
            float width = math.distance(m_Point1.m_Position.xz, m_Point2.m_Position.xz);
            float height = math.distance(m_Point1.m_Position.xz, m_Point3.m_Position.xz);
            Log?.Info($"[{Mod.ModId}] Area dimensions: {width:F1}m × {height:F1}m");

            // Allocate output list for generated roads
            NativeList<RoadDefinition> generatedRoads = new NativeList<RoadDefinition>(
                Allocator.TempJob);

            // Create and schedule the grid generation job
            GenerateOrganicGridJob job = new GenerateOrganicGridJob
            {
                m_PointA = m_Point1.m_Position,
                m_PointB = m_Point2.m_Position,
                m_PointC = m_Point3.m_Position,
                m_Parameters = m_LayoutParameters,
                m_GeneratedRoads = generatedRoads
            };

            // Schedule the job (runs immediately since it's IJob)
            JobHandle gridJobHandle = job.Schedule();
            gridJobHandle.Complete();

            // Log initial generation results
            Log?.Info($"[{Mod.ModId}] Generated {generatedRoads.Length} road segments (before terrain processing)");

            // ===== PHASE 4: TERRAIN AWARENESS =====

            // Get terrain and water data from game systems
            TerrainHeightData terrainHeightData = m_TerrainSystem.GetHeightData();
            WaterSurfaceData waterSurfaceData = m_WaterSystem.GetSurfaceData(out JobHandle waterDeps);

            // Wait for water data dependencies
            waterDeps.Complete();

            // Convert NativeList to NativeArray for the terrain job
            NativeArray<RoadDefinition> inputRoadsArray = new NativeArray<RoadDefinition>(
                generatedRoads.Length,
                Allocator.TempJob);

            for (int i = 0; i < generatedRoads.Length; i++)
            {
                inputRoadsArray[i] = generatedRoads[i];
            }

            // Create output list for terrain-aware roads
            NativeList<RoadDefinition> terrainAwareRoads = new NativeList<RoadDefinition>(
                generatedRoads.Length,
                Allocator.TempJob);

            // Create statistics reference
            NativeReference<TerrainStats> terrainStats = new NativeReference<TerrainStats>(
                TerrainStats.Create(),
                Allocator.TempJob);

            // Create and schedule terrain awareness job
            ApplyTerrainAwarenessJob terrainJob = new ApplyTerrainAwarenessJob
            {
                m_InputRoads = inputRoadsArray,
                m_TerrainParams = m_TerrainParameters,
                m_TerrainHeightData = terrainHeightData,
                m_WaterSurfaceData = waterSurfaceData,
                m_OutputRoads = terrainAwareRoads,
                m_Stats = terrainStats
            };

            JobHandle terrainJobHandle = terrainJob.Schedule();
            terrainJobHandle.Complete();

            // Get terrain statistics
            TerrainStats stats = terrainStats.Value;

            Log?.Info($"[{Mod.ModId}] Terrain processing complete:");
            Log?.Info($"[{Mod.ModId}]   Processed: {stats.m_TotalRoads} roads");
            Log?.Info($"[{Mod.ModId}]   Valid: {stats.m_ValidRoads} roads");
            if (stats.m_RejectedBySlope > 0)
                Log?.Info($"[{Mod.ModId}]   Rejected (slope): {stats.m_RejectedBySlope} roads");
            if (stats.m_RejectedByWater > 0)
                Log?.Info($"[{Mod.ModId}]   Rejected (water): {stats.m_RejectedByWater} roads");
            if (stats.m_ValidRoads > 0)
            {
                Log?.Info($"[{Mod.ModId}]   Elevation range: {stats.m_MinElevation:F1}m to {stats.m_MaxElevation:F1}m");
            }

            // Cleanup temporary arrays
            inputRoadsArray.Dispose();
            terrainStats.Dispose();

            // Use terrain-aware roads for final output
            // Swap references: terrainAwareRoads becomes the main list
            generatedRoads.Dispose();
            generatedRoads = terrainAwareRoads;

            // ===== END TERRAIN AWARENESS =====

            // Log road type breakdown
            int arterialCount = 0;
            int collectorCount = 0;
            int localCount = 0;
            int culDeSacCount = 0;

            for (int i = 0; i < generatedRoads.Length; i++)
            {
                RoadDefinition road = generatedRoads[i];

                switch (road.m_Type)
                {
                    case RoadType.Arterial: arterialCount++; break;
                    case RoadType.Collector: collectorCount++; break;
                    case RoadType.Local: localCount++; break;
                    case RoadType.CulDeSac: culDeSacCount++; break;
                }

                // Log first few roads for debugging
                if (i < 5)
                {
                    Log?.Info($"[{Mod.ModId}]   Road {i}: {road.m_Type}, " +
                             $"{road.GetLength():F1}m, curve={road.m_CurveAmount:F2}");
                    Log?.Info($"[{Mod.ModId}]     Start: ({road.m_Start.x:F1}, {road.m_Start.y:F1}, {road.m_Start.z:F1})");
                    Log?.Info($"[{Mod.ModId}]     End: ({road.m_End.x:F1}, {road.m_End.y:F1}, {road.m_End.z:F1})");
                }
            }

            Log?.Info($"[{Mod.ModId}] Road type breakdown:");
            Log?.Info($"[{Mod.ModId}]   Arterial: {arterialCount}");
            Log?.Info($"[{Mod.ModId}]   Collector: {collectorCount}");
            Log?.Info($"[{Mod.ModId}]   Local: {localCount}");
            if (culDeSacCount > 0)
                Log?.Info($"[{Mod.ModId}]   Cul-de-sac: {culDeSacCount}");

            // Phase 4 Complete: Terrain awareness applied!
            // ✅ Roads snapped to terrain height
            // ✅ Slopes validated (rejected if too steep)
            // ✅ Water bodies detected and avoided

            // ===== PHASE 5: NETCOURSE ENTITY CREATION =====

            // Get entity command buffer for creating entities
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();

            int createdCount = 0;
            int skippedCount = 0;

            // Convert each terrain-aware road into a NetCourse entity
            for (int i = 0; i < generatedRoads.Length; i++)
            {
                RoadDefinition road = generatedRoads[i];

                // Check if we have a prefab for this road type
                Entity prefab = GetRoadPrefab(road.m_Type);

                if (prefab == Entity.Null)
                {
                    skippedCount++;
                    continue;  // Skip roads without prefabs
                }

                // Create the NetCourse entity!
                CreateNetCourseEntity(commandBuffer, road);
                createdCount++;
            }

            Log?.Info($"[{Mod.ModId}] NetCourse entity creation complete:");
            Log?.Info($"[{Mod.ModId}]   Created: {createdCount} entities");
            if (skippedCount > 0)
                Log?.Info($"[{Mod.ModId}]   Skipped: {skippedCount} (no prefab configured)");

            if (createdCount == 0)
            {
                Log?.Warn($"[{Mod.ModId}] No roads created! Road prefabs not configured.");
                Log?.Warn($"[{Mod.ModId}] Use SetRoadPrefabs() to configure road prefabs.");
                Log?.Warn($"[{Mod.ModId}] For now, roads are calculated but not visible in-game.");
            }

            // ===== END NETCOURSE CREATION =====

            Log?.Info($"[{Mod.ModId}] Phase 5 complete! Road entities created.");
            if (createdCount > 0)
            {
                Log?.Info($"[{Mod.ModId}] Roads should now appear in-game as preview entities!");
                Log?.Info($"[{Mod.ModId}] Press Enter/Apply to make them permanent, Esc to cancel.");
            }

            // Cleanup
            generatedRoads.Dispose();

            // Reset state after applying
            m_State = State.WaitingForFirstPoint;
        }

        /// <summary>
        /// Clear all temporary preview entities
        /// </summary>
        private void ClearTemporaryEntities()
        {
            if (!m_TempQuery.IsEmpty)
            {
                EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
                EntityManager.DestroyEntity(m_TempQuery);
            }
        }

        /// <summary>
        /// Get raycast result from the raycast system
        /// </summary>
        private bool GetRaycastResult(out Entity entity, out RaycastHit hit)
        {
            entity = Entity.Null;
            hit = default;

            if (m_ToolRaycastSystem.GetRaycastResult(out entity, out hit))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the apply action (left click) was triggered this frame
        /// </summary>
        private bool GetApplyInput()
        {
            // Check for left mouse button or apply action
            return applyAction.WasPressedThisFrame();
        }

        /// <summary>
        /// Check if the cancel action (right click or Esc) was triggered
        /// </summary>
        private bool GetCancelInput()
        {
            // Check for right mouse button or cancel action
            return cancelAction.WasPressedThisFrame();
        }

        /// <summary>
        /// Get road prefab entity based on road type
        /// Uses a simple fallback system for now - Phase 6 will add proper prefab lookup
        /// </summary>
        private Entity GetRoadPrefab(RoadType roadType)
        {
            // For now, use a default prefab for all road types
            // In Phase 6, we'll add proper prefab lookup from PrefabSystem
            // based on road names like "Small Road", "Medium Road", "Large Road"

            switch (roadType)
            {
                case RoadType.Arterial:
                    return m_ArterialPrefab;
                case RoadType.Collector:
                    return m_CollectorPrefab;
                case RoadType.Local:
                case RoadType.CulDeSac:
                    return m_LocalPrefab;
                default:
                    return m_LocalPrefab;
            }
        }

        /// <summary>
        /// Create a NetCourse entity from a RoadDefinition
        /// This is the critical method that makes roads appear in-game!
        /// </summary>
        private void CreateNetCourseEntity(
            EntityCommandBuffer commandBuffer,
            RoadDefinition road)
        {
            // Get appropriate road prefab for this road type
            Entity roadPrefab = GetRoadPrefab(road.m_Type);

            // If no prefab found, log warning and skip
            if (roadPrefab == Entity.Null)
            {
                Log?.Warn($"[{Mod.ModId}] No road prefab found for {road.m_Type}, skipping road");
                return;
            }

            // Create the entity
            Entity entity = commandBuffer.CreateEntity();

            // Add CreationDefinition (tells game what prefab to use)
            CreationDefinition creationDef = new CreationDefinition
            {
                m_Prefab = roadPrefab,
                m_Original = Entity.Null,  // Not replacing existing road
                m_Flags = CreationFlags.None
            };
            commandBuffer.AddComponent(entity, creationDef);

            // Add Updated component (marks entity as needing processing)
            commandBuffer.AddComponent<Updated>(entity);

            // Add Temp component (makes this a preview entity)
            Temp temp = new Temp
            {
                m_Flags = TempFlags.Create,  // This will create permanent road on apply
                m_Original = Entity.Null
            };
            commandBuffer.AddComponent(entity, temp);

            // Generate Bezier curve from road definition
            Bezier4x3 curve = road.m_CurveAmount > 0.01f
                ? CurveUtils.CreateOrganicCurve(road.m_Start, road.m_End, road.m_CurveAmount, road.m_Seed)
                : CurveUtils.CreateStraightCurve(road.m_Start, road.m_End);

            // Calculate curve length
            float length = MathUtils.Length(curve);

            // Create NetCourse component
            NetCourse netCourse = new NetCourse
            {
                m_Curve = curve,
                m_Length = length,
                m_FixedIndex = -1,  // Not fixed to specific index
                m_Elevation = new float2(0f, 0f),  // Default elevation

                // Start position
                m_StartPosition = new CoursePos
                {
                    m_Position = curve.a,
                    m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(curve), quaternion.identity),
                    m_Elevation = new float2(road.m_Start.y, road.m_Start.y),
                    m_CourseDelta = 0f,  // Start of course
                    m_Flags = CoursePosFlags.IsFirst,
                    m_Entity = Entity.Null,
                    m_ParentMesh = -1,
                    m_SplitPosition = 0f
                },

                // End position
                m_EndPosition = new CoursePos
                {
                    m_Position = curve.d,
                    m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(curve), quaternion.identity),
                    m_Elevation = new float2(road.m_End.y, road.m_End.y),
                    m_CourseDelta = 1f,  // End of course
                    m_Flags = CoursePosFlags.IsLast,
                    m_Entity = Entity.Null,
                    m_ParentMesh = -1,
                    m_SplitPosition = 1f
                }
            };

            // Add NetCourse component
            commandBuffer.AddComponent(entity, netCourse);

            Log?.Info($"[{Mod.ModId}] Created NetCourse entity for {road.m_Type} road ({length:F1}m)");
        }

        /// <summary>
        /// Public method to set layout parameters from UI (Phase 6)
        /// </summary>
        public void SetLayoutParameters(LayoutParameters parameters)
        {
            m_LayoutParameters = parameters;
            Log?.Info($"[{Mod.ModId}] Layout parameters updated:");
            Log?.Info($"[{Mod.ModId}]   Style: {parameters.m_Style}");
            Log?.Info($"[{Mod.ModId}]   Road spacing: {parameters.m_RoadSpacing}m");
            Log?.Info($"[{Mod.ModId}]   Position variation: {parameters.m_PositionVariation}m");
        }

        /// <summary>
        /// Public method to set terrain awareness parameters from UI (Phase 6)
        /// </summary>
        public void SetTerrainParameters(TerrainAwareParameters parameters)
        {
            m_TerrainParameters = parameters;
            Log?.Info($"[{Mod.ModId}] Terrain parameters updated:");
            Log?.Info($"[{Mod.ModId}]   Snap to terrain: {parameters.m_SnapToTerrain}");
            Log?.Info($"[{Mod.ModId}]   Validate slope: {parameters.m_ValidateSlope}");
            if (parameters.m_ValidateSlope)
                Log?.Info($"[{Mod.ModId}]   Max slope: {parameters.m_MaxSlope}°");
            Log?.Info($"[{Mod.ModId}]   Avoid water: {parameters.m_AvoidWater}");
            if (parameters.m_AvoidWater)
                Log?.Info($"[{Mod.ModId}]   Max water depth: {parameters.m_MaxWaterDepth}m");
        }

        /// <summary>
        /// Public method to set road prefabs (Phase 5/6)
        /// Allows UI or configuration to set specific road prefabs for each road type
        /// </summary>
        public void SetRoadPrefabs(Entity arterial, Entity collector, Entity local)
        {
            m_ArterialPrefab = arterial;
            m_CollectorPrefab = collector;
            m_LocalPrefab = local;

            Log?.Info($"[{Mod.ModId}] Road prefabs configured:");
            Log?.Info($"[{Mod.ModId}]   Arterial: {arterial}");
            Log?.Info($"[{Mod.ModId}]   Collector: {collector}");
            Log?.Info($"[{Mod.ModId}]   Local: {local}");
        }

        /// <summary>
        /// Called when the system is disposed
        /// </summary>
        protected override void OnDestroy()
        {
            Log?.Info($"[{Mod.ModId}] OrganicNeighborhoodToolSystem.OnDestroy()");
            base.OnDestroy();
        }
    }
}
