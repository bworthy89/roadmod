using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Rendering.Utilities;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class WaterToolSystem : ToolBaseSystem
{
	public enum Attribute
	{
		None,
		Location,
		Radius,
		Rate,
		Height
	}

	private enum State
	{
		Default,
		MouseDown,
		Dragging
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public ControlPoint m_StartPoint;

		[ReadOnly]
		public ControlPoint m_RaycastPoint;

		[ReadOnly]
		public float m_OriginalHeight;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public Attribute m_Attribute;

		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> m_WaterSourceData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public float3 m_PositionOffset;

		[ReadOnly]
		public bool m_useLegacy;

		public EntityCommandBuffer m_CommandBuffer;

		private WaterSourceDefinition GetWaterDefinitionLegacy(CreationDefinition definitionData, Game.Simulation.WaterSourceData sourceData)
		{
			if (m_State == State.Dragging)
			{
				definitionData.m_Flags |= CreationFlags.Dragging;
				switch (m_Attribute)
				{
				case Attribute.Location:
					if (sourceData.m_ConstantDepth == 1 || sourceData.m_ConstantDepth == 2)
					{
						sourceData.m_Height += m_RaycastPoint.m_Position.y - m_StartPoint.m_Position.y;
					}
					break;
				case Attribute.Radius:
					sourceData.m_Radius = math.clamp(math.distance(m_RaycastPoint.m_HitPosition.xz, m_StartPoint.m_Position.xz), 1f, 20000f);
					break;
				case Attribute.Rate:
					sourceData.m_Height = math.clamp(m_RaycastPoint.m_HitPosition.y - m_StartPoint.m_Position.y, 1f, 1000f);
					break;
				case Attribute.Height:
					sourceData.m_Height = m_RaycastPoint.m_HitPosition.y - m_PositionOffset.y;
					break;
				}
			}
			return new WaterSourceDefinition
			{
				m_ConstantDepth = sourceData.m_ConstantDepth,
				m_Height = sourceData.m_Height,
				m_Radius = sourceData.m_Radius,
				m_Multiplier = sourceData.m_Multiplier,
				m_Polluted = sourceData.m_Polluted
			};
		}

		private WaterSourceDefinition GetWaterDefinition(CreationDefinition definitionData, Game.Simulation.WaterSourceData sourceData, Entity original)
		{
			WaterSourceDefinition result = default(WaterSourceDefinition);
			if (m_TransformData.TryGetComponent(original, out var _))
			{
				if (m_State == State.Dragging)
				{
					definitionData.m_Flags |= CreationFlags.Dragging;
					switch (m_Attribute)
					{
					case Attribute.Radius:
						sourceData.m_Radius = math.clamp(math.distance(m_RaycastPoint.m_HitPosition.xz, m_StartPoint.m_Position.xz), 1f, 2500f);
						break;
					case Attribute.Height:
					{
						float num = m_RaycastPoint.m_HitPosition.y - m_StartPoint.m_HitPosition.y;
						sourceData.m_Height = math.clamp(m_OriginalHeight + num, 0f, 250f);
						break;
					}
					}
				}
				result.m_Radius = sourceData.m_Radius;
				result.m_Height = sourceData.m_Height;
				result.m_Polluted = sourceData.m_Polluted;
				result.m_SourceId = sourceData.m_id;
				result.m_SourceNameId = sourceData.SourceNameId;
			}
			return result;
		}

		public void Execute()
		{
			Entity entity = Entity.Null;
			float3 position = default(float3);
			switch (m_State)
			{
			case State.Default:
				entity = m_RaycastPoint.m_OriginalEntity;
				position = m_RaycastPoint.m_Position;
				break;
			case State.MouseDown:
				entity = m_StartPoint.m_OriginalEntity;
				position = m_StartPoint.m_Position;
				break;
			case State.Dragging:
				entity = m_StartPoint.m_OriginalEntity;
				position = m_RaycastPoint.m_Position;
				break;
			}
			if (m_WaterSourceData.TryGetComponent(entity, out var componentData))
			{
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition creationDefinition = new CreationDefinition
				{
					m_Original = entity
				};
				creationDefinition.m_Flags |= CreationFlags.Select;
				WaterSourceDefinition component = ((!m_useLegacy) ? GetWaterDefinition(creationDefinition, componentData, entity) : GetWaterDefinitionLegacy(creationDefinition, componentData));
				component.m_Position = position;
				m_CommandBuffer.AddComponent(e, creationDefinition);
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Game.Simulation.WaterSourceData> __Game_Simulation_WaterSourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_WaterSourceData_RO_ComponentLookup = state.GetComponentLookup<Game.Simulation.WaterSourceData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
		}
	}

	public const string kToolID = "Water Tool";

	public bool m_showSourceNames;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_DefinitionQuery;

	private ControlPoint m_RaycastPoint;

	private ControlPoint m_StartPoint;

	private State m_State;

	private float m_OriginalHeight;

	private TypeHandle __TypeHandle;

	public override string toolID => "Water Tool";

	public Attribute attribute { get; private set; }

	public event Action<int> onWaterSourceClick;

	public event Action<int> onWaterSourceDeleted;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_showSourceNames = false;
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_RaycastPoint = default(ControlPoint);
		m_State = State.Default;
		attribute = Attribute.None;
	}

	private protected override void UpdateActions()
	{
		base.applyAction.shouldBeEnabled = base.actionsEnabled;
		base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
	}

	public override PrefabBase GetPrefab()
	{
		return null;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		return false;
	}

	private void InitializeRaycastImplLegacy()
	{
		if (m_State == State.Dragging)
		{
			if (attribute != Attribute.Location)
			{
				return;
			}
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
			if (base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_StartPoint.m_OriginalEntity, out var component))
			{
				float num = component.m_Height;
				if (component.m_ConstantDepth > 0)
				{
					TerrainHeightData data = m_TerrainSystem.GetHeightData();
					num += m_TerrainSystem.positionOffset.y - TerrainUtils.SampleHeight(ref data, m_StartPoint.m_Position);
				}
				m_ToolRaycastSystem.rayOffset = new float3(0f, 0f - num, 0f);
			}
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.WaterSources;
		}
	}

	private void InitializeRaycastImpl()
	{
		if (m_State == State.Dragging)
		{
			if (attribute == Attribute.Location)
			{
				m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
				if (base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_StartPoint.m_OriginalEntity, out var component))
				{
					float height = component.m_Height;
					m_ToolRaycastSystem.rayOffset = new float3(0f, 0f - height, 0f);
				}
			}
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.WaterSources;
		}
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		if (m_WaterSystem.UseLegacyWaterSources)
		{
			InitializeRaycastImplLegacy();
		}
		else
		{
			InitializeRaycastImpl();
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		UpdateInfoview(Entity.Null);
		GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
		if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
		{
			if (m_State != State.Default)
			{
				if (base.applyAction.WasPressedThisFrame() || base.applyAction.WasReleasedThisFrame())
				{
					return Apply(inputDeps);
				}
				if (base.secondaryApplyAction.WasPressedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
				{
					return Cancel(inputDeps);
				}
				return Update(inputDeps);
			}
			if (base.secondaryApplyAction.WasPressedThisFrame())
			{
				return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
			}
			if (base.secondaryApplyAction.WasReleasedThisFrame())
			{
				return DeleteSource(inputDeps);
			}
			if (base.applyAction.WasPressedThisFrame())
			{
				return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
			}
			return Update(inputDeps);
		}
		return Clear(inputDeps);
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		base.GetAvailableSnapMask(out onMask, out offMask);
		onMask |= Snap.ContourLines;
		offMask |= Snap.ContourLines;
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Default:
			base.applyMode = ApplyMode.None;
			return inputDeps;
		case State.MouseDown:
			m_State = State.Default;
			base.applyMode = ApplyMode.Clear;
			return inputDeps;
		case State.Dragging:
			m_State = State.Default;
			base.applyMode = ApplyMode.Clear;
			return inputDeps;
		default:
			return Update(inputDeps);
		}
	}

	private JobHandle DeleteSource(JobHandle inputDeps)
	{
		if (m_RaycastPoint.m_OriginalEntity != Entity.Null)
		{
			int arg = -1;
			if (base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_RaycastPoint.m_OriginalEntity, out var component))
			{
				arg = component.m_id;
			}
			base.EntityManager.DestroyEntity(m_RaycastPoint.m_OriginalEntity);
			this.onWaterSourceDeleted.Fire(arg);
		}
		return inputDeps;
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Default:
			if (m_RaycastPoint.m_OriginalEntity != Entity.Null && !singleFrameOnly)
			{
				m_State = State.MouseDown;
				m_StartPoint = m_RaycastPoint;
				if (base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_RaycastPoint.m_OriginalEntity, out var component))
				{
					m_OriginalHeight = component.m_Height;
					this.onWaterSourceClick.Fire(m_RaycastPoint.m_OriginalEntity.Index);
				}
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		case State.MouseDown:
			m_State = State.Default;
			base.applyMode = ApplyMode.Clear;
			return inputDeps;
		case State.Dragging:
			m_State = State.Default;
			base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
			return inputDeps;
		default:
			return Update(inputDeps);
		}
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		m_TerrainSystem.GetHeightData();
		if (GetRaycastResult(out var controlPoint))
		{
			if (m_RaycastPoint.Equals(controlPoint))
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			if (m_State == State.Default)
			{
				attribute = GetAttribute(controlPoint);
			}
			base.applyMode = ApplyMode.Clear;
			m_RaycastPoint = controlPoint;
			if (m_State == State.MouseDown && math.distance(controlPoint.m_HitPosition, m_StartPoint.m_HitPosition) >= 1f)
			{
				m_State = State.Dragging;
				if (m_WaterSystem.UseLegacyWaterSources)
				{
					m_State = State.Dragging;
				}
				else if (attribute == Attribute.Height)
				{
					GetRaycastResult(out controlPoint);
					m_StartPoint = controlPoint;
				}
				inputDeps = UpdateDefinitions(inputDeps);
			}
			else
			{
				inputDeps = UpdateDefinitions(inputDeps);
			}
			return inputDeps;
		}
		if (m_RaycastPoint.Equals(default(ControlPoint)))
		{
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		base.applyMode = ApplyMode.Clear;
		m_RaycastPoint = default(ControlPoint);
		if (m_State == State.MouseDown)
		{
			inputDeps = UpdateDefinitions(inputDeps);
			m_State = State.Dragging;
		}
		else
		{
			if (m_State == State.Default)
			{
				attribute = Attribute.None;
			}
			inputDeps = UpdateDefinitions(inputDeps);
		}
		return inputDeps;
	}

	private Attribute GetAttribute(ControlPoint controlPoint)
	{
		if (base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(controlPoint.m_OriginalEntity, out var component) && m_CameraUpdateSystem.TryGetViewer(out var viewer))
		{
			float2 @float = controlPoint.m_HitPosition.xz - controlPoint.m_Position.xz;
			if (math.length(@float) < component.m_Radius * 0.9f)
			{
				return Attribute.Location;
			}
			float2 xz = viewer.right.xz;
			float2 x = MathUtils.Left(xz);
			if (math.abs(math.dot(xz, @float)) > math.abs(math.dot(x, @float)))
			{
				return Attribute.Radius;
			}
			if (m_WaterSystem.UseLegacyWaterSources)
			{
				if (component.m_ConstantDepth != 0)
				{
					return Attribute.Height;
				}
				return Attribute.Rate;
			}
			if (math.abs(math.dot(x, @float)) > math.abs(math.dot(xz, @float)))
			{
				return Attribute.Height;
			}
		}
		return Attribute.None;
	}

	private bool GetRaycastResultImplLegacy(out ControlPoint controlPoint)
	{
		if (m_State == State.Dragging && attribute != Attribute.None && attribute != Attribute.Location && base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_StartPoint.m_OriginalEntity, out var component) && m_CameraUpdateSystem.TryGetViewer(out var viewer))
		{
			TerrainHeightData data = m_TerrainSystem.GetHeightData();
			Line3.Segment line = ToolRaycastSystem.CalculateRaycastLine(viewer.camera);
			controlPoint = m_StartPoint;
			float2 t2;
			if (attribute == Attribute.Radius)
			{
				float3 position = m_StartPoint.m_Position;
				if (component.m_ConstantDepth > 0)
				{
					position.y = m_TerrainSystem.positionOffset.y + component.m_Height;
				}
				else
				{
					position.y = TerrainUtils.SampleHeight(ref data, position) + component.m_Height;
				}
				if (MathUtils.Intersect(line.y, position.y, out var t))
				{
					controlPoint.m_HitPosition = MathUtils.Position(line, t);
				}
			}
			else if (MathUtils.Intersect(new Circle2(component.m_Radius, m_StartPoint.m_Position.xz), line.xz, out t2))
			{
				float3 hitPosition = MathUtils.Position(line, t2.x);
				float3 hitPosition2 = MathUtils.Position(line, t2.y);
				if (math.distancesq(hitPosition.xz, m_StartPoint.m_HitPosition.xz) <= math.distancesq(hitPosition2.xz, m_StartPoint.m_HitPosition.xz))
				{
					controlPoint.m_HitPosition = hitPosition;
				}
				else
				{
					controlPoint.m_HitPosition = hitPosition2;
				}
			}
			return true;
		}
		return base.GetRaycastResult(out controlPoint);
	}

	private bool GetRaycastResultImpl(out ControlPoint controlPoint)
	{
		if (m_State == State.Dragging && attribute != Attribute.None && attribute != Attribute.Location && base.EntityManager.TryGetComponent<Game.Simulation.WaterSourceData>(m_StartPoint.m_OriginalEntity, out var component) && m_CameraUpdateSystem.TryGetViewer(out var viewer))
		{
			TerrainHeightData data = m_TerrainSystem.GetHeightData();
			Line3.Segment line = ToolRaycastSystem.CalculateRaycastLine(viewer.camera);
			controlPoint = m_StartPoint;
			float2 t2;
			if (attribute == Attribute.Radius)
			{
				float3 position = m_StartPoint.m_Position;
				position.y = TerrainUtils.SampleHeight(ref data, position) + component.m_Height;
				if (MathUtils.Intersect(line.y, position.y, out var t))
				{
					controlPoint.m_HitPosition = MathUtils.Position(line, t);
				}
			}
			else if (attribute == Attribute.Height)
			{
				float3 planeOrigin = m_StartPoint.m_HitPosition;
				float3 hitPosition = ToolRaycastSystem.CameraRayPlaneIntersect(viewer.camera, in planeOrigin);
				controlPoint.m_HitPosition = hitPosition;
			}
			else if (MathUtils.Intersect(new Circle2(component.m_Radius, m_StartPoint.m_Position.xz), line.xz, out t2))
			{
				float3 hitPosition2 = MathUtils.Position(line, t2.x);
				float3 hitPosition3 = MathUtils.Position(line, t2.y);
				if (math.distancesq(hitPosition2.xz, m_StartPoint.m_HitPosition.xz) <= math.distancesq(hitPosition3.xz, m_StartPoint.m_HitPosition.xz))
				{
					controlPoint.m_HitPosition = hitPosition2;
				}
				else
				{
					controlPoint.m_HitPosition = hitPosition3;
				}
			}
			return true;
		}
		return base.GetRaycastResult(out controlPoint);
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (m_WaterSystem.UseLegacyWaterSources)
		{
			return GetRaycastResultImplLegacy(out controlPoint);
		}
		return GetRaycastResultImpl(out controlPoint);
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (m_RaycastPoint.m_OriginalEntity != Entity.Null)
		{
			JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
			{
				m_StartPoint = m_StartPoint,
				m_RaycastPoint = m_RaycastPoint,
				m_OriginalHeight = m_OriginalHeight,
				m_State = m_State,
				m_Attribute = attribute,
				m_WaterSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_WaterSourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionOffset = m_TerrainSystem.positionOffset,
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer(),
				m_useLegacy = m_WaterSystem.UseLegacyWaterSources
			}, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		return jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public WaterToolSystem()
	{
	}
}
