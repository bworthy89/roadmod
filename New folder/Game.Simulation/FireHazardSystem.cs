using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Areas;
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
public class FireHazardSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct FireHazardJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_FirePrefabChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<Tree> m_TreeType;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> m_DamagedType;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> m_UnderConstructionType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<EventData> m_PrefabEventType;

		[ReadOnly]
		public ComponentTypeHandle<FireData> m_PrefabFireType;

		[ReadOnly]
		public ComponentTypeHandle<Locked> m_LockedType;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public EventHelpers.FireHazardData m_FireHazardData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_NaturalDisasters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (random.NextInt(64) != 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			float riskFactor;
			if (nativeArray3.Length != 0)
			{
				NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
				NativeArray<CurrentDistrict> nativeArray5 = chunk.GetNativeArray(ref m_CurrentDistrictType);
				NativeArray<Damaged> nativeArray6 = chunk.GetNativeArray(ref m_DamagedType);
				NativeArray<UnderConstruction> nativeArray7 = chunk.GetNativeArray(ref m_UnderConstructionType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					PrefabRef prefabRef = nativeArray2[i];
					Building building = nativeArray3[i];
					CurrentDistrict currentDistrict = nativeArray5[i];
					if (!CollectionUtils.TryGet(nativeArray4, i, out var value) || (m_BuildingData.HasComponent(value.m_Owner) && m_PlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & PlacementFlags.Floating) != PlacementFlags.None))
					{
						CollectionUtils.TryGet(nativeArray6, i, out var value2);
						if (!CollectionUtils.TryGet(nativeArray7, i, out var value3))
						{
							value3 = new UnderConstruction
							{
								m_Progress = byte.MaxValue
							};
						}
						if (m_FireHazardData.GetFireHazard(prefabRef, building, currentDistrict, value2, value3, out var fireHazard, out riskFactor))
						{
							TryStartFire(unfilteredChunkIndex, ref random, entity, fireHazard, EventTargetType.Building);
						}
					}
				}
			}
			else
			{
				if (!chunk.Has(ref m_TreeType) || chunk.Has(ref m_OwnerType) || !m_NaturalDisasters)
				{
					return;
				}
				NativeArray<Damaged> nativeArray8 = chunk.GetNativeArray(ref m_DamagedType);
				NativeArray<Transform> nativeArray9 = chunk.GetNativeArray(ref m_TransformType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					PrefabRef prefabRef2 = nativeArray2[j];
					Transform transform = nativeArray9[j];
					Damaged damaged = default(Damaged);
					if (nativeArray8.Length != 0)
					{
						damaged = nativeArray8[j];
					}
					if (m_FireHazardData.GetFireHazard(prefabRef2, default(Tree), transform, damaged, out var fireHazard2, out riskFactor))
					{
						TryStartFire(unfilteredChunkIndex, ref random, entity2, fireHazard2, EventTargetType.WildTree);
					}
				}
			}
		}

		private void TryStartFire(int jobIndex, ref Random random, Entity entity, float fireHazard, EventTargetType targetType)
		{
			for (int i = 0; i < m_FirePrefabChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_FirePrefabChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<EventData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_PrefabEventType);
				NativeArray<FireData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_PrefabFireType);
				EnabledMask enabledMask = archetypeChunk.GetEnabledMask(ref m_LockedType);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					FireData fireData = nativeArray3[j];
					if (fireData.m_RandomTargetType == targetType && (!enabledMask.EnableBit.IsValid || !enabledMask[j]))
					{
						float num = fireHazard * fireData.m_StartProbability;
						if (random.NextFloat(10000f) < num)
						{
							CreateFireEvent(jobIndex, entity, nativeArray[j], nativeArray2[j], fireData);
							return;
						}
					}
				}
			}
		}

		private void CreateFireEvent(int jobIndex, Entity targetEntity, Entity eventPrefab, EventData eventData, FireData fireData)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, eventData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new PrefabRef(eventPrefab));
			m_CommandBuffer.SetBuffer<TargetElement>(jobIndex, e).Add(new TargetElement(targetEntity));
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Damaged> __Game_Objects_Damaged_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EventData> __Game_Prefabs_EventData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FireData> __Game_Prefabs_FireData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Locked> __Game_Prefabs_Locked_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Damaged>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentTypeHandle = state.GetComponentTypeHandle<UnderConstruction>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_EventData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EventData>(isReadOnly: true);
			__Game_Prefabs_FireData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FireData>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Locked>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
		}
	}

	private const int UPDATES_PER_DAY = 64;

	private LocalEffectSystem m_LocalEffectSystem;

	private PrefabSystem m_PrefabSystem;

	private ClimateSystem m_ClimateSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_FlammableQuery;

	private EntityQuery m_FirePrefabQuery;

	private EntityQuery m_FireConfigQuery;

	private EventHelpers.FireHazardData m_FireHazardData;

	private TypeHandle __TypeHandle;

	public float noRainDays { get; private set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4096;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_FlammableQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Tree>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<Game.Buildings.FireStation>(),
				ComponentType.ReadOnly<Placeholder>(),
				ComponentType.ReadOnly<OnFire>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Overridden>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_FirePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<EventData>(), ComponentType.ReadOnly<FireData>(), ComponentType.Exclude<Locked>());
		m_FireConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_FireHazardData = new EventHelpers.FireHazardData(this);
		RequireForUpdate(m_FlammableQuery);
		RequireForUpdate(m_FirePrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ClimateSystem.isRaining)
		{
			noRainDays = 0f;
		}
		else
		{
			noRainDays += 1f / 64f;
		}
		JobHandle dependencies;
		LocalEffectSystem.ReadData readData = m_LocalEffectSystem.GetReadData(out dependencies);
		FireConfigurationPrefab prefab = m_PrefabSystem.GetPrefab<FireConfigurationPrefab>(m_FireConfigQuery.GetSingletonEntity());
		m_FireHazardData.Update(this, readData, prefab, m_ClimateSystem.temperature, noRainDays);
		JobHandle outJobHandle;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new FireHazardJob
		{
			m_FirePrefabChunks = m_FirePrefabQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TreeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DamagedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnderConstructionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabEventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabFireType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_FireData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LockedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireHazardData = m_FireHazardData,
			m_RandomSeed = RandomSeed.Next(),
			m_NaturalDisasters = m_CityConfigurationSystem.naturalDisasters,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_FlammableQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
		m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(noRainDays);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out float value);
		noRainDays = value;
	}

	public void SetDefaults(Context context)
	{
		noRainDays = 0f;
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
	public FireHazardSystem()
	{
	}
}
