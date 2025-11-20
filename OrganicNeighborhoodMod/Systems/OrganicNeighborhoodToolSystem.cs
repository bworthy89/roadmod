using Colossal.Logging;
using Game;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
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
        private Entity m_RoadPrefab;

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

            // TODO: In Phase 5, get actual road prefab from game
            // For now, set to Entity.Null - will be populated later
            m_RoadPrefab = Entity.Null;

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

            // Get entity command buffer
            EntityCommandBuffer commandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();

            // TODO Phase 3: Schedule GenerateOrganicGridJob
            // This will calculate all road positions with Perlin variation

            // TODO Phase 4: Apply terrain awareness
            // Snap roads to terrain, validate slopes, avoid water

            // TODO Phase 5: Create NetCourse entities
            // Convert road definitions into actual NetCourse entities
            // that the game's road generation systems will process

            // For now, just log placeholder
            Log?.Info($"[{Mod.ModId}] Layout generation not yet implemented (Phase 3+)");
            Log?.Info($"[{Mod.ModId}] Area dimensions:");
            float width = math.distance(m_Point1.m_Position.xz, m_Point2.m_Position.xz);
            float height = math.distance(m_Point1.m_Position.xz, m_Point3.m_Position.xz);
            Log?.Info($"[{Mod.ModId}]   Width: {width:F1}m");
            Log?.Info($"[{Mod.ModId}]   Height: {height:F1}m");

            int roadCountX = (int)(width / m_LayoutParameters.m_RoadSpacing);
            int roadCountY = (int)(height / m_LayoutParameters.m_RoadSpacing);
            Log?.Info($"[{Mod.ModId}]   Estimated roads: {roadCountX}x{roadCountY} = {roadCountX + roadCountY} total");

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
        /// Public method to set road prefab (Phase 5)
        /// </summary>
        public void SetRoadPrefab(Entity prefab)
        {
            m_RoadPrefab = prefab;
            Log?.Info($"[{Mod.ModId}] Road prefab set: {prefab}");
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
