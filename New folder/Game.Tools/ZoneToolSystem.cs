using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Audio;
using Game.Common;
using Game.Input;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ZoneToolSystem : ToolBaseSystem
{
	public enum Mode
	{
		FloodFill,
		Marquee,
		Paint
	}

	private enum State
	{
		Default,
		Zoning,
		Dezoning
	}

	[BurstCompile]
	private struct SetZoneTypeJob : IJobChunk
	{
		[ReadOnly]
		public ZoneType m_Type;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		public BufferTypeHandle<Cell> m_CellType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Block> nativeArray = chunk.GetNativeArray(ref m_BlockType);
			BufferAccessor<Cell> bufferAccessor = chunk.GetBufferAccessor(ref m_CellType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Block block = nativeArray[i];
				DynamicBuffer<Cell> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < block.m_Size.y; j++)
				{
					for (int k = 0; k < block.m_Size.x; k++)
					{
						int index = j * block.m_Size.x + k;
						Cell value = dynamicBuffer[index];
						value.m_Zone = m_Type;
						dynamicBuffer[index] = value;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public ControlPoint m_StartPoint;

		[ReadOnly]
		public ControlPoint m_RaycastPoint;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_TempChunks;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		[ReadOnly]
		public BufferTypeHandle<Cell> m_CellType;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		public NativeValue<ControlPoint> m_SnapPoint;

		public void Execute()
		{
			switch (m_Mode)
			{
			case Mode.FloodFill:
			case Mode.Paint:
				CheckSelectedCell();
				break;
			case Mode.Marquee:
				CheckMarqueeCell();
				break;
			}
		}

		private void CheckSelectedCell()
		{
			for (int i = 0; i < m_TempChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_TempChunks[i];
				NativeArray<Block> nativeArray = archetypeChunk.GetNativeArray(ref m_BlockType);
				BufferAccessor<Cell> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_CellType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Block block = nativeArray[j];
					DynamicBuffer<Cell> dynamicBuffer = bufferAccessor[j];
					int2 cellIndex = ZoneUtils.GetCellIndex(block, m_RaycastPoint.m_HitPosition.xz);
					if (math.all((cellIndex >= 0) & (cellIndex < block.m_Size)) && (dynamicBuffer[cellIndex.y * block.m_Size.x + cellIndex.x].m_State & CellFlags.Selected) != CellFlags.None)
					{
						return;
					}
				}
			}
			m_SnapPoint.value = m_RaycastPoint;
		}

		private void CheckMarqueeCell()
		{
			ControlPoint value = m_RaycastPoint;
			if ((m_Snap & Snap.ExistingGeometry) == 0)
			{
				value.m_OriginalEntity = Entity.Null;
			}
			if (m_BlockData.HasComponent(value.m_OriginalEntity))
			{
				Block block = m_BlockData[value.m_OriginalEntity];
				value.m_Position = ZoneUtils.GetCellPosition(block, value.m_ElementIndex);
				value.m_HitPosition = value.m_Position;
				m_SnapPoint.value = value;
			}
			else if ((m_Snap & Snap.CellLength) != Snap.None && m_State != State.Default)
			{
				float2 @float = ((!m_BlockData.HasComponent(m_StartPoint.m_OriginalEntity)) ? math.normalizesafe(m_CameraRight.xz) : m_BlockData[m_StartPoint.m_OriginalEntity].m_Direction);
				float2 float2 = MathUtils.Right(@float);
				float2 xz = m_StartPoint.m_HitPosition.xz;
				float2 x = value.m_HitPosition.xz - xz;
				float num = MathUtils.Snap(math.dot(x, @float), 8f);
				float num2 = MathUtils.Snap(math.dot(x, float2), 8f);
				value.m_HitPosition.y = m_StartPoint.m_HitPosition.y;
				value.m_HitPosition.xz = xz + @float * num + float2 * num2;
				m_SnapPoint.value = value;
			}
			else
			{
				m_SnapPoint.value = value;
			}
		}
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Mode m_Mode;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public bool m_Overwrite;

		[ReadOnly]
		public ControlPoint m_StartPoint;

		[ReadOnly]
		public NativeValue<ControlPoint> m_SnapPoint;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			ControlPoint value = m_SnapPoint.value;
			if (value.Equals(default(ControlPoint)))
			{
				return;
			}
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = m_Prefab,
				m_Original = value.m_OriginalEntity
			};
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			Zoning component2 = default(Zoning);
			if (m_State == State.Dezoning)
			{
				component2.m_Flags |= ZoningFlags.Dezone | ZoningFlags.Overwrite;
			}
			else if (m_Overwrite)
			{
				component2.m_Flags |= ZoningFlags.Zone | ZoningFlags.Overwrite;
			}
			else
			{
				component2.m_Flags |= ZoningFlags.Zone;
			}
			switch (m_Mode)
			{
			case Mode.FloodFill:
				component2.m_Flags |= ZoningFlags.FloodFill;
				if (m_State != State.Default)
				{
					float3 hitPosition3 = m_StartPoint.m_HitPosition;
					float3 hitPosition4 = value.m_HitPosition;
					component2.m_Position = new Quad3(hitPosition3, hitPosition3, hitPosition4, hitPosition4);
				}
				else
				{
					float3 hitPosition5 = value.m_HitPosition;
					component2.m_Position = new Quad3(hitPosition5, hitPosition5, hitPosition5, hitPosition5);
				}
				break;
			case Mode.Paint:
				component2.m_Flags |= ZoningFlags.Paint;
				if (m_State != State.Default)
				{
					float3 hitPosition6 = m_StartPoint.m_HitPosition;
					float3 hitPosition7 = value.m_HitPosition;
					component2.m_Position = new Quad3(hitPosition6, hitPosition6, hitPosition7, hitPosition7);
				}
				else
				{
					float3 hitPosition8 = value.m_HitPosition;
					component2.m_Position = new Quad3(hitPosition8, hitPosition8, hitPosition8, hitPosition8);
				}
				break;
			case Mode.Marquee:
			{
				component2.m_Flags |= ZoningFlags.Marquee;
				float3 @float = 0f;
				if (m_State != State.Default && m_BlockData.HasComponent(m_StartPoint.m_OriginalEntity))
				{
					@float.xz = m_BlockData[m_StartPoint.m_OriginalEntity].m_Direction;
				}
				else if (m_State == State.Default && m_BlockData.HasComponent(value.m_OriginalEntity))
				{
					@float.xz = m_BlockData[value.m_OriginalEntity].m_Direction;
				}
				else
				{
					@float.xz = math.normalizesafe(m_CameraRight.xz);
				}
				float3 float2 = 0f;
				float2.xz = MathUtils.Right(@float.xz);
				if (m_State != State.Default)
				{
					float3 hitPosition = m_StartPoint.m_HitPosition;
					float3 x = value.m_HitPosition - hitPosition;
					float num = math.dot(x, @float);
					float num2 = math.dot(x, float2);
					if (num < 0f)
					{
						@float = -@float;
						num = 0f - num;
					}
					if (num2 < 0f)
					{
						float2 = -float2;
						num2 = 0f - num2;
					}
					component2.m_Position.a = hitPosition - (@float + float2) * 4f;
					component2.m_Position.b = hitPosition - @float * 4f + float2 * (num2 + 4f);
					component2.m_Position.c = hitPosition + @float * (num + 4f) + float2 * (num2 + 4f);
					component2.m_Position.d = hitPosition + @float * (num + 4f) - float2 * 4f;
				}
				else
				{
					@float *= 4f;
					float2 *= 4f;
					float3 hitPosition2 = value.m_HitPosition;
					component2.m_Position.a = hitPosition2 - @float - float2;
					component2.m_Position.b = hitPosition2 - @float + float2;
					component2.m_Position.c = hitPosition2 + @float + float2;
					component2.m_Position.d = hitPosition2 + @float - float2;
				}
				break;
			}
			}
			component2.m_Position.a.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, component2.m_Position.a);
			component2.m_Position.b.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, component2.m_Position.b);
			component2.m_Position.c.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, component2.m_Position.c);
			component2.m_Position.d.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, component2.m_Position.d);
			m_CommandBuffer.AddComponent(e, component2);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

		public BufferTypeHandle<Cell> __Game_Zones_Cell_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Cell> __Game_Zones_Cell_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
			__Game_Zones_Cell_RW_BufferTypeHandle = state.GetBufferTypeHandle<Cell>();
			__Game_Zones_Cell_RO_BufferTypeHandle = state.GetBufferTypeHandle<Cell>(isReadOnly: true);
			__Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(isReadOnly: true);
		}
	}

	public const string kToolID = "Zone Tool";

	private ZonePrefab m_Prefab;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private AudioManager m_AudioManager;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_DefinitionGroup;

	private EntityQuery m_TempBlockQuery;

	private EntityQuery m_SoundQuery;

	private IProxyAction m_ApplyZone;

	private IProxyAction m_RemoveZone;

	private IProxyAction m_DiscardZoning;

	private IProxyAction m_DiscardDezoning;

	private IProxyAction m_DefaultDiscardApply;

	private IProxyAction m_DefaultDiscardRemove;

	private bool m_ApplyBlocked;

	private ControlPoint m_RaycastPoint;

	private ControlPoint m_StartPoint;

	private NativeValue<ControlPoint> m_SnapPoint;

	private State m_State;

	private TypeHandle __TypeHandle;

	public override string toolID => "Zone Tool";

	public override int uiModeIndex => (int)mode;

	public Mode mode { get; set; }

	public ZonePrefab prefab
	{
		get
		{
			return m_Prefab;
		}
		set
		{
			if (m_Prefab != value)
			{
				m_ForceUpdate = true;
				m_Prefab = value;
			}
		}
	}

	public bool overwrite { get; set; }

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_ApplyZone;
			yield return m_RemoveZone;
			yield return m_DiscardZoning;
			yield return m_DiscardDezoning;
		}
	}

	public override void GetUIModes(List<ToolMode> modes)
	{
		modes.Add(new ToolMode(Mode.FloodFill.ToString(), 0));
		modes.Add(new ToolMode(Mode.Marquee.ToString(), 1));
		modes.Add(new ToolMode(Mode.Paint.ToString(), 2));
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_DefinitionGroup = GetDefinitionQuery();
		m_TempBlockQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Block>(), ComponentType.ReadWrite<Cell>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_ApplyZone = InputManager.instance.toolActionCollection.GetActionState("Apply Zone", "ZoneToolSystem");
		m_RemoveZone = InputManager.instance.toolActionCollection.GetActionState("Remove Zone", "ZoneToolSystem");
		m_DiscardZoning = InputManager.instance.toolActionCollection.GetActionState("Discard Zoning", "ZoneToolSystem");
		m_DiscardDezoning = InputManager.instance.toolActionCollection.GetActionState("Discard Dezoning", "ZoneToolSystem");
		m_DefaultDiscardApply = InputManager.instance.toolActionCollection.GetActionState("Discard Primary", "ZoneToolSystem");
		m_DefaultDiscardRemove = InputManager.instance.toolActionCollection.GetActionState("Discard Secondary", "ZoneToolSystem");
		m_SnapPoint = new NativeValue<ControlPoint>(Allocator.Persistent);
		overwrite = true;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_SnapPoint.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		base.requireZones = true;
		base.requireAreas = AreaTypeMask.Lots;
		m_RaycastPoint = default(ControlPoint);
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		m_ApplyBlocked = false;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			switch (m_State)
			{
			case State.Zoning:
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyAction.shouldBeEnabled = false;
				base.cancelAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_ApplyZone;
				base.secondaryApplyActionOverride = null;
				base.cancelActionOverride = ((mode == Mode.Marquee) ? m_DiscardZoning : m_DefaultDiscardApply);
				break;
			case State.Dezoning:
				base.applyAction.shouldBeEnabled = false;
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.cancelAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = null;
				base.secondaryApplyActionOverride = m_RemoveZone;
				base.cancelActionOverride = ((mode == Mode.Marquee) ? m_DiscardDezoning : m_DefaultDiscardRemove);
				break;
			default:
				base.applyAction.shouldBeEnabled = base.actionsEnabled && GetAllowApplyZone();
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled && GetAllowRemoveZone();
				base.cancelAction.shouldBeEnabled = false;
				base.applyActionOverride = m_ApplyZone;
				base.secondaryApplyActionOverride = m_RemoveZone;
				base.cancelActionOverride = null;
				break;
			}
		}
	}

	protected bool GetAllowApplyZone()
	{
		Mode mode = this.mode;
		if ((uint)(mode - 1) <= 1u)
		{
			return !m_SnapPoint.value.Equals(default(ControlPoint));
		}
		GetRaycastResult(out Entity entity, out RaycastHit hit);
		if (entity == Entity.Null || hit.m_CellIndex.Equals(new int2(-1, -1)))
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<Block>(entity, out var component))
		{
			return false;
		}
		Entity entity2 = m_PrefabSystem.GetEntity(prefab);
		if (entity2 == Entity.Null)
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<ZoneData>(entity2, out var component2))
		{
			return false;
		}
		if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Cell> buffer))
		{
			return false;
		}
		if (buffer[hit.m_CellIndex.y * component.m_Size.x + hit.m_CellIndex.x].m_Zone.m_Index == component2.m_ZoneType.m_Index)
		{
			return false;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_Zone.m_Index != component2.m_ZoneType.m_Index)
			{
				return true;
			}
		}
		return false;
	}

	protected bool GetAllowRemoveZone()
	{
		Mode mode = this.mode;
		if ((uint)(mode - 1) <= 1u)
		{
			return !m_SnapPoint.value.Equals(default(ControlPoint));
		}
		GetRaycastResult(out Entity entity, out RaycastHit _);
		if (entity == Entity.Null)
		{
			return false;
		}
		if (!base.EntityManager.TryGetComponent<Block>(entity, out var _))
		{
			return false;
		}
		if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Cell> buffer))
		{
			return false;
		}
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_Zone.m_Index != 0)
			{
				return true;
			}
		}
		return false;
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is ZonePrefab zonePrefab)
		{
			this.prefab = zonePrefab;
			return true;
		}
		return false;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		if (prefab != null)
		{
			GetAvailableSnapMask(out var onMask, out var offMask);
			switch (mode)
			{
			case Mode.FloodFill:
			case Mode.Paint:
				m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Zones;
				break;
			case Mode.Marquee:
				if ((ToolBaseSystem.GetActualSnap(selectedSnap, onMask, offMask) & Snap.ExistingGeometry) != Snap.None)
				{
					m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.Zones;
				}
				else
				{
					m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
				}
				break;
			default:
				m_ToolRaycastSystem.typeMask = TypeMask.None;
				break;
			}
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.None;
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		UpdateActions();
		if (m_FocusChanged)
		{
			return inputDeps;
		}
		if (m_State != State.Default && (!base.applyAction.enabled || !base.cancelAction.enabled) && (!base.secondaryApplyAction.enabled || !base.cancelAction.enabled))
		{
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			return Clear(inputDeps);
		}
		if (prefab != null)
		{
			UpdateInfoview(m_PrefabSystem.GetEntity(prefab));
			GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				switch (m_State)
				{
				case State.Default:
					if (m_ApplyBlocked)
					{
						if (mode != Mode.Marquee || base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
						{
							m_ApplyBlocked = false;
						}
						return Update(inputDeps);
					}
					if (base.secondaryApplyAction.WasPressedThisFrame())
					{
						return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
					}
					if (base.applyAction.WasPressedThisFrame())
					{
						return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
					}
					return Update(inputDeps);
				case State.Zoning:
					if (base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = mode == Mode.Marquee;
						return Cancel(inputDeps);
					}
					if (base.applyAction.WasPressedThisFrame() || base.applyAction.WasReleasedThisFrame())
					{
						return Apply(inputDeps);
					}
					return Update(inputDeps);
				case State.Dezoning:
					if (base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = mode == Mode.Marquee;
						return Apply(inputDeps);
					}
					if (base.secondaryApplyAction.WasPressedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
					{
						return Cancel(inputDeps);
					}
					return Update(inputDeps);
				}
			}
		}
		else
		{
			UpdateInfoview(Entity.Null);
		}
		if (m_State != State.Default && (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame()))
		{
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
		}
		return Clear(inputDeps);
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		switch (mode)
		{
		case Mode.FloodFill:
		case Mode.Paint:
			onMask = Snap.ExistingGeometry;
			offMask = Snap.None;
			break;
		case Mode.Marquee:
			onMask = Snap.ExistingGeometry | Snap.CellLength;
			offMask = Snap.ExistingGeometry | Snap.CellLength;
			break;
		default:
			base.GetAvailableSnapMask(out onMask, out offMask);
			break;
		}
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
		if (m_State == State.Default)
		{
			switch (mode)
			{
			case Mode.FloodFill:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningRemoveFillSound);
				base.applyMode = ApplyMode.Apply;
				break;
			case Mode.Paint:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningStartRemovePaintSound);
				base.applyMode = ApplyMode.Apply;
				break;
			case Mode.Marquee:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningMarqueeClearStartSound);
				base.applyMode = ApplyMode.Clear;
				break;
			}
			if (!singleFrameOnly)
			{
				m_StartPoint = m_SnapPoint.value;
				m_State = State.Dezoning;
			}
			GetRaycastResult(out m_RaycastPoint);
			JobHandle jobHandle = SnapPoint(inputDeps);
			JobHandle job = SetZoneType(jobHandle);
			JobHandle job2 = UpdateDefinitions(jobHandle);
			return JobHandle.CombineDependencies(job, job2);
		}
		if (m_State == State.Dezoning)
		{
			base.applyMode = ApplyMode.Apply;
			if (math.distance(m_StartPoint.m_Position, m_RaycastPoint.m_Position) > 5f && mode == Mode.Marquee)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningMarqueeClearEndSound);
			}
			if (mode == Mode.Paint)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningEndRemovePaintSound);
			}
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			inputDeps = SnapPoint(inputDeps);
			return UpdateDefinitions(inputDeps);
		}
		base.applyMode = ApplyMode.Clear;
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		GetRaycastResult(out m_RaycastPoint);
		inputDeps = SnapPoint(inputDeps);
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (m_State == State.Default)
		{
			switch (mode)
			{
			case Mode.FloodFill:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningFillSound);
				base.applyMode = ApplyMode.Apply;
				break;
			case Mode.Paint:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningStartPaintSound);
				base.applyMode = ApplyMode.Apply;
				break;
			case Mode.Marquee:
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningMarqueeStartSound);
				base.applyMode = ApplyMode.Clear;
				break;
			}
			if (!singleFrameOnly)
			{
				m_StartPoint = m_SnapPoint.value;
				m_State = State.Zoning;
			}
			GetRaycastResult(out m_RaycastPoint);
			inputDeps = SnapPoint(inputDeps);
			return UpdateDefinitions(inputDeps);
		}
		if (m_State == State.Zoning)
		{
			base.applyMode = ApplyMode.Apply;
			if (math.distance(m_StartPoint.m_Position, m_RaycastPoint.m_Position) > 5f && mode == Mode.Marquee)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningMarqueeEndSound);
			}
			if (mode == Mode.Paint)
			{
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_ZoningEndPaintSound);
			}
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			inputDeps = SnapPoint(inputDeps);
			return UpdateDefinitions(inputDeps);
		}
		base.applyMode = ApplyMode.Clear;
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		GetRaycastResult(out m_RaycastPoint);
		inputDeps = SnapPoint(inputDeps);
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
		{
			ControlPoint value = m_SnapPoint.value;
			if (m_RaycastPoint.Equals(controlPoint) && !forceUpdate)
			{
				switch (this.mode)
				{
				case Mode.FloodFill:
				case Mode.Paint:
					if (m_State == State.Default || m_StartPoint.Equals(value))
					{
						base.applyMode = ApplyMode.None;
						return inputDeps;
					}
					break;
				case Mode.Marquee:
					base.applyMode = ApplyMode.None;
					return inputDeps;
				}
			}
			else
			{
				m_RaycastPoint = controlPoint;
				inputDeps = SnapPoint(inputDeps);
				JobHandle.ScheduleBatchedJobs();
				inputDeps.Complete();
			}
			if (value.Equals(m_SnapPoint.value) && !forceUpdate)
			{
				switch (this.mode)
				{
				case Mode.FloodFill:
				case Mode.Paint:
					if (m_State == State.Default || m_StartPoint.Equals(value))
					{
						base.applyMode = ApplyMode.None;
						return inputDeps;
					}
					break;
				case Mode.Marquee:
					base.applyMode = ApplyMode.None;
					return inputDeps;
				}
			}
			switch (this.mode)
			{
			case Mode.FloodFill:
			case Mode.Paint:
				if (m_State != State.Default)
				{
					base.applyMode = ApplyMode.Apply;
					m_StartPoint = value;
				}
				else
				{
					base.applyMode = ApplyMode.Clear;
				}
				return UpdateDefinitions(inputDeps);
			case Mode.Marquee:
				base.applyMode = ApplyMode.Clear;
				return UpdateDefinitions(inputDeps);
			}
		}
		else
		{
			if (m_RaycastPoint.Equals(default(ControlPoint)))
			{
				base.applyMode = (forceUpdate ? ApplyMode.Clear : ApplyMode.None);
				return inputDeps;
			}
			m_RaycastPoint = default(ControlPoint);
			Mode mode = this.mode;
			if ((mode == Mode.FloodFill || mode == Mode.Paint) && m_State != State.Default)
			{
				m_StartPoint = m_SnapPoint.value;
				base.applyMode = ApplyMode.Apply;
				inputDeps = SnapPoint(inputDeps);
				return UpdateDefinitions(inputDeps);
			}
		}
		base.applyMode = ApplyMode.Clear;
		inputDeps = SnapPoint(inputDeps);
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle SetZoneType(JobHandle inputDeps)
	{
		if (m_TempBlockQuery.IsEmptyIgnoreFilter)
		{
			return inputDeps;
		}
		return JobChunkExtensions.ScheduleParallel(new SetZoneTypeJob
		{
			m_Type = default(ZoneType),
			m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RW_BufferTypeHandle, ref base.CheckedStateRef)
		}, m_TempBlockQuery, inputDeps);
	}

	private JobHandle SnapPoint(JobHandle inputDeps)
	{
		if (m_RaycastPoint.Equals(default(ControlPoint)))
		{
			m_SnapPoint.value = default(ControlPoint);
			return inputDeps;
		}
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> tempChunks = m_TempBlockQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		Transform transform = Camera.main.transform;
		JobHandle jobHandle = IJobExtensions.Schedule(new SnapJob
		{
			m_Snap = GetActualSnap(),
			m_Mode = mode,
			m_State = m_State,
			m_CameraRight = transform.right,
			m_StartPoint = m_StartPoint,
			m_RaycastPoint = m_RaycastPoint,
			m_TempChunks = tempChunks,
			m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CellType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_Cell_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SnapPoint = m_SnapPoint
		}, JobHandle.CombineDependencies(inputDeps, outJobHandle));
		tempChunks.Dispose(jobHandle);
		return jobHandle;
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionGroup, m_ToolOutputBarrier, inputDeps);
		if (!m_RaycastPoint.Equals(default(ControlPoint)))
		{
			Transform transform = Camera.main.transform;
			JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
			{
				m_Prefab = m_PrefabSystem.GetEntity(prefab),
				m_Mode = mode,
				m_State = m_State,
				m_CameraRight = transform.right,
				m_Overwrite = overwrite,
				m_StartPoint = m_StartPoint,
				m_SnapPoint = m_SnapPoint,
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_BlockData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Zones_Block_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			}, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			m_TerrainSystem.AddCPUHeightReader(jobHandle2);
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
	public ZoneToolSystem()
	{
	}
}
