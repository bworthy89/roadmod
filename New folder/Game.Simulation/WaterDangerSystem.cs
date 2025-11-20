using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class WaterDangerSystem : GameSystemBase
{
	[BurstCompile]
	private struct WaterDangerJob : IJobChunk
	{
		private struct EndangeredStaticObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public int m_JobIndex;

			public uint m_SimulationFrame;

			public float m_DangerSpeed;

			public float m_DangerHeight;

			public Bounds1 m_PredictionDistance;

			public Entity m_Event;

			public Line2 m_StartLine;

			public WaterLevelChangeData m_WaterLevelChangeData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;

			public ComponentLookup<InDanger> m_InDangerData;

			public EntityArchetype m_EndangerArchetype;

			public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (bounds.m_Bounds.min.y < m_DangerHeight)
				{
					return MathUtils.Intersect(m_PredictionDistance, GetDistanceBounds(bounds.m_Bounds.xz, m_StartLine));
				}
				return false;
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (bounds.m_Bounds.min.y >= m_DangerHeight)
				{
					return;
				}
				Bounds1 distanceBounds = GetDistanceBounds(bounds.m_Bounds.xz, m_StartLine);
				if (!MathUtils.Intersect(m_PredictionDistance, distanceBounds) || !m_BuildingData.HasComponent(item))
				{
					return;
				}
				DangerFlags dangerFlags = m_WaterLevelChangeData.m_DangerFlags;
				if ((dangerFlags & DangerFlags.Evacuate) != 0 && m_EmergencyShelterData.HasComponent(item))
				{
					dangerFlags = (DangerFlags)((uint)dangerFlags & 0xFFFFFFFDu);
					dangerFlags |= DangerFlags.StayIndoors;
				}
				if (m_InDangerData.HasComponent(item))
				{
					InDanger inDanger = m_InDangerData[item];
					if (inDanger.m_EndFrame >= m_SimulationFrame + 64 && (inDanger.m_Event == m_Event || !EventUtils.IsWorse(dangerFlags, inDanger.m_Flags)))
					{
						return;
					}
				}
				float num = 30f + (distanceBounds.max - m_PredictionDistance.min) / m_DangerSpeed;
				Entity e = m_CommandBuffer.CreateEntity(m_JobIndex, m_EndangerArchetype);
				m_CommandBuffer.SetComponent(m_JobIndex, e, new Endanger
				{
					m_Event = m_Event,
					m_Target = item,
					m_Flags = dangerFlags,
					m_EndFrame = m_SimulationFrame + 64 + (uint)(num * 60f)
				});
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<WaterLevelChange> m_WaterLevelChangeType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		public ComponentTypeHandle<Game.Events.DangerLevel> m_DangerLevelType;

		[ReadOnly]
		public ComponentLookup<InDanger> m_InDangerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> m_EmergencyShelterData;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> m_PrefabWaterLevelChangeData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public EntityArchetype m_EndangerArchetype;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<WaterLevelChange> nativeArray3 = chunk.GetNativeArray(ref m_WaterLevelChangeType);
			NativeArray<Duration> nativeArray4 = chunk.GetNativeArray(ref m_DurationType);
			NativeArray<Game.Events.DangerLevel> nativeArray5 = chunk.GetNativeArray(ref m_DangerLevelType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity eventEntity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				WaterLevelChange waterLevelChange = nativeArray3[i];
				Duration duration = nativeArray4[i];
				WaterLevelChangeData waterLevelChangeData = m_PrefabWaterLevelChangeData[prefabRef.m_Prefab];
				if (m_SimulationFrame < duration.m_EndFrame && waterLevelChangeData.m_DangerFlags != 0)
				{
					FindEndangeredObjects(unfilteredChunkIndex, eventEntity, duration, waterLevelChange, waterLevelChangeData);
				}
				bool flag = m_SimulationFrame > duration.m_StartFrame && m_SimulationFrame < duration.m_EndFrame;
				nativeArray5[i] = new Game.Events.DangerLevel(flag ? waterLevelChangeData.m_DangerLevel : 0f);
			}
		}

		private void FindEndangeredObjects(int jobIndex, Entity eventEntity, Duration duration, WaterLevelChange waterLevelChange, WaterLevelChangeData waterLevelChangeData)
		{
			float value = 10f;
			float num = 0f;
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.DisasterWarningTime);
			if (duration.m_StartFrame > m_SimulationFrame)
			{
				value -= (float)(duration.m_StartFrame - m_SimulationFrame) / 60f;
			}
			else
			{
				num = (float)(m_SimulationFrame - duration.m_StartFrame) / 60f;
			}
			value = math.max(0f, value);
			float num2 = (float)(duration.m_EndFrame - WaterLevelChangeSystem.TsunamiEndDelay - duration.m_StartFrame) / 60f;
			float num3 = WaterSystem.WaveSpeed * 60f;
			float num4 = num * num3;
			float min = num4 - num2 * num3;
			float2 @float = WaterSystem.kMapSize / 2 * -waterLevelChange.m_Direction;
			Line2 startLine = new Line2(@float, @float + MathUtils.Right(waterLevelChange.m_Direction));
			Bounds1 predictionDistance = new Bounds1(min, num4);
			predictionDistance.max += value * num3;
			EndangeredStaticObjectIterator iterator = new EndangeredStaticObjectIterator
			{
				m_JobIndex = jobIndex,
				m_SimulationFrame = m_SimulationFrame,
				m_DangerSpeed = num3,
				m_DangerHeight = waterLevelChange.m_DangerHeight,
				m_PredictionDistance = predictionDistance,
				m_Event = eventEntity,
				m_StartLine = startLine,
				m_WaterLevelChangeData = waterLevelChangeData,
				m_BuildingData = m_BuildingData,
				m_EmergencyShelterData = m_EmergencyShelterData,
				m_InDangerData = m_InDangerData,
				m_EndangerArchetype = m_EndangerArchetype,
				m_CommandBuffer = m_CommandBuffer
			};
			m_StaticObjectSearchTree.Iterate(ref iterator);
		}

		private static Bounds1 GetDistanceBounds(Bounds2 bounds, Line2 line)
		{
			float t;
			float4 x = new float4(MathUtils.Distance(line, bounds.min, out t), MathUtils.Distance(line, new float2(bounds.min.x, bounds.max.y), out t), MathUtils.Distance(line, bounds.max, out t), MathUtils.Distance(line, new float2(bounds.max.x, bounds.min.y), out t));
			Bounds1 bounds2 = new Bounds1(math.cmin(x), math.cmax(x));
			if (MathUtils.Intersect(bounds, line, out var _))
			{
				return bounds2 | 0f;
			}
			return bounds2;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<WaterLevelChange> __Game_Events_WaterLevelChange_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Events.DangerLevel> __Game_Events_DangerLevel_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterLevelChangeData> __Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Events_WaterLevelChange_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterLevelChange>();
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Events_DangerLevel_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.DangerLevel>();
			__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.EmergencyShelter>(isReadOnly: true);
			__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup = state.GetComponentLookup<WaterLevelChangeData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private CitySystem m_CitySystem;

	private SearchSystem m_ObjectSearchSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_WaterLevelChangeQuery;

	private EntityArchetype m_EndangerArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_WaterLevelChangeQuery = GetEntityQuery(ComponentType.ReadOnly<WaterLevelChange>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_EndangerArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Endanger>());
		RequireForUpdate(m_WaterLevelChangeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WaterDangerJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterLevelChangeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DangerLevelType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_DangerLevel_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EmergencyShelterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWaterLevelChangeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterLevelChangeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_City = m_CitySystem.City,
			m_EndangerArchetype = m_EndangerArchetype,
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_WaterLevelChangeQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public WaterDangerSystem()
	{
	}
}
