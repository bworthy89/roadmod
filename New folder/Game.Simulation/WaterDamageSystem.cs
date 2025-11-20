using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Events;
using Game.Notifications;
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
public class WaterDamageSystem : GameSystemBase
{
	[BurstCompile]
	private struct WaterDamageJob : IJobChunk
	{
		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public EntityArchetype m_DestroyEventArchetype;

		[ReadOnly]
		public DisasterConfigurationData m_DisasterConfigurationData;

		[ReadOnly]
		public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		public ComponentTypeHandle<Flooded> m_FloodedType;

		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			float num = 1.0666667f;
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Flooded> nativeArray3 = chunk.GetNativeArray(ref m_FloodedType);
			NativeArray<Damaged> nativeArray4 = chunk.GetNativeArray(ref m_DamagedType);
			NativeArray<Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
			bool flag = chunk.Has(ref m_BuildingType);
			bool flag2 = chunk.Has(ref m_DestroyedType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Flooded value = nativeArray3[i];
				value.m_Depth = GetFloodDepth(nativeArray5[i].m_Position);
				float num2 = 0f;
				if (!flag2 && m_ObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					ObjectGeometryData objectGeometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
					num2 = math.min(m_DisasterConfigurationData.m_FloodDamageRate, value.m_Depth * m_DisasterConfigurationData.m_FloodDamageRate / math.max(0.5f, objectGeometryData.m_Size.y));
				}
				if (num2 > 0f)
				{
					float structuralIntegrity = m_StructuralIntegrityData.GetStructuralIntegrity(prefabRef.m_Prefab, flag);
					float value2 = num2 / structuralIntegrity;
					if (flag)
					{
						DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
						CityUtils.ApplyModifier(ref value2, modifiers, CityModifierType.DisasterDamageRate);
					}
					value2 = math.min(0.5f, value2 * num);
					if (value2 > 0f)
					{
						if (nativeArray4.Length != 0)
						{
							Damaged damaged = nativeArray4[i];
							damaged.m_Damage.z = math.min(1f, damaged.m_Damage.z + value2);
							if (!flag2 && ObjectUtils.GetTotalDamage(damaged) == 1f)
							{
								Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DestroyEventArchetype);
								m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Destroy(entity, value.m_Event));
								m_IconCommandBuffer.Remove(entity, m_DisasterConfigurationData.m_WaterDamageNotificationPrefab);
								m_IconCommandBuffer.Remove(entity, IconPriority.Problem);
								m_IconCommandBuffer.Remove(entity, IconPriority.FatalProblem);
								m_IconCommandBuffer.Add(entity, m_DisasterConfigurationData.m_WaterDestroyedNotificationPrefab, IconPriority.FatalProblem, IconClusterLayer.Default, IconFlags.IgnoreTarget, value.m_Event);
								num2 = 0f;
							}
							nativeArray4[i] = damaged;
						}
						else
						{
							Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_DamageEventArchetype);
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new Damage(entity, new float3(0f, 0f, value2)));
						}
					}
				}
				if (value.m_Depth > 0f)
				{
					if (num2 > 0f)
					{
						m_IconCommandBuffer.Add(entity, m_DisasterConfigurationData.m_WaterDamageNotificationPrefab, (num2 >= 30f) ? IconPriority.MajorProblem : IconPriority.Problem, IconClusterLayer.Default, IconFlags.IgnoreTarget, value.m_Event);
					}
				}
				else
				{
					m_CommandBuffer.RemoveComponent<Flooded>(unfilteredChunkIndex, entity);
					m_IconCommandBuffer.Remove(entity, m_DisasterConfigurationData.m_WaterDamageNotificationPrefab);
				}
				nativeArray3[i] = value;
			}
		}

		private float GetFloodDepth(float3 position)
		{
			float num = WaterUtils.SampleDepth(ref m_WaterSurfaceData, position);
			if (num > 0.5f)
			{
				num += TerrainUtils.SampleHeight(ref m_TerrainHeightData, position) - position.y;
				if (num > 0.5f)
				{
					return num;
				}
			}
			return 0f;
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

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Flooded> __Game_Events_Flooded_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Events_Flooded_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Flooded>();
			__Game_Objects_Damaged_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>();
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private IconCommandSystem m_IconCommandSystem;

	private CitySystem m_CitySystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_FloodedQuery;

	private EntityQuery m_FireConfigQuery;

	private EntityQuery m_DisasterConfigQuery;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_DestroyEventArchetype;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_FloodedQuery = GetEntityQuery(ComponentType.ReadWrite<Flooded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_FireConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_DisasterConfigQuery = GetEntityQuery(ComponentType.ReadOnly<DisasterConfigurationData>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Damage>());
		m_DestroyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Destroy>());
		m_StructuralIntegrityData = new EventHelpers.StructuralIntegrityData(this);
		RequireForUpdate(m_FloodedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FireConfigurationData singleton = m_FireConfigQuery.GetSingleton<FireConfigurationData>();
		DisasterConfigurationData singleton2 = m_DisasterConfigQuery.GetSingleton<DisasterConfigurationData>();
		m_StructuralIntegrityData.Update(this, singleton);
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new WaterDamageJob
		{
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_DestroyEventArchetype = m_DestroyEventArchetype,
			m_DisasterConfigurationData = singleton2,
			m_StructuralIntegrityData = m_StructuralIntegrityData,
			m_City = m_CitySystem.City,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FloodedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Flooded_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef)
		}, m_FloodedQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
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
	public WaterDamageSystem()
	{
	}
}
