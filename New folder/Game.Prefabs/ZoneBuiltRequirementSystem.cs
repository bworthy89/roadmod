using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Serialization;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class ZoneBuiltRequirementSystem : GameSystemBase, IPreDeserialize
{
	private struct ZoneBuiltData
	{
		public Entity m_Theme;

		public Entity m_Zone;

		public int m_Squares;

		public int m_Count;

		public AreaType m_Type;

		public byte m_Level;
	}

	[BurstCompile]
	private struct UpdateZoneBuiltDataJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_BuildingChunks;

		public NativeParallelHashMap<ZoneBuiltDataKey, ZoneBuiltDataValue> m_ZoneBuiltData;

		public NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingData;

		public void Execute()
		{
			for (int i = 0; i < m_BuildingChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_BuildingChunks[i];
				NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				bool flag = archetypeChunk.Has(ref m_DeletedType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					PrefabRef prefabRef = nativeArray[j];
					if (m_SpawnableBuildingData.HasComponent(prefabRef.m_Prefab))
					{
						SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefabRef.m_Prefab];
						BuildingData buildingData = m_BuildingData[prefabRef.m_Prefab];
						ZoneBuiltDataKey key = new ZoneBuiltDataKey
						{
							m_Zone = spawnableBuildingData.m_ZonePrefab,
							m_Level = spawnableBuildingData.m_Level
						};
						if (!m_ZoneBuiltData.ContainsKey(key))
						{
							m_ZoneBuiltData[key] = new ZoneBuiltDataValue(0, 0);
						}
						int num2;
						int num = (num2 = ((!flag) ? 1 : (-1))) * buildingData.m_LotSize.x * buildingData.m_LotSize.y;
						m_ZoneBuiltData[key] = new ZoneBuiltDataValue
						{
							m_Count = math.max(0, m_ZoneBuiltData[key].m_Count + num2),
							m_Squares = math.max(0, m_ZoneBuiltData[key].m_Squares + num)
						};
					}
				}
			}
			ZoneBuiltLevelUpdate item;
			while (m_ZoneBuiltLevelQueue.TryDequeue(out item))
			{
				ZoneBuiltDataKey key2 = new ZoneBuiltDataKey
				{
					m_Zone = item.m_Zone,
					m_Level = item.m_FromLevel
				};
				ZoneBuiltDataKey key3 = new ZoneBuiltDataKey
				{
					m_Zone = item.m_Zone,
					m_Level = item.m_ToLevel
				};
				if (!m_ZoneBuiltData.ContainsKey(key2))
				{
					m_ZoneBuiltData[key2] = new ZoneBuiltDataValue(0, 0);
				}
				if (!m_ZoneBuiltData.ContainsKey(key3))
				{
					m_ZoneBuiltData[key3] = new ZoneBuiltDataValue(0, 0);
				}
				m_ZoneBuiltData[key2] = new ZoneBuiltDataValue
				{
					m_Count = math.max(0, m_ZoneBuiltData[key2].m_Count - 1),
					m_Squares = math.max(0, m_ZoneBuiltData[key2].m_Squares - item.m_Squares)
				};
				m_ZoneBuiltData[key3] = new ZoneBuiltDataValue
				{
					m_Count = math.max(0, m_ZoneBuiltData[key3].m_Count + 1),
					m_Squares = math.max(0, m_ZoneBuiltData[key3].m_Squares + item.m_Squares)
				};
			}
		}
	}

	[BurstCompile]
	private struct ZoneBuiltRequirementJob : IJobChunk
	{
		[ReadOnly]
		public NativeParallelHashMap<ZoneBuiltDataKey, ZoneBuiltDataValue> m_ZoneBuiltData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		[ReadOnly]
		public ComponentLookup<ThemeData> m_ThemeData;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_ObjectRequirementElements;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ZoneBuiltRequirementData> m_ZoneBuiltRequirementType;

		public ComponentTypeHandle<UnlockRequirementData> m_UnlockRequirementType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ZoneBuiltRequirementData> nativeArray2 = chunk.GetNativeArray(ref m_ZoneBuiltRequirementType);
			NativeArray<UnlockRequirementData> nativeArray3 = chunk.GetNativeArray(ref m_UnlockRequirementType);
			ChunkEntityEnumerator chunkEntityEnumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			int nextIndex;
			while (chunkEntityEnumerator.NextEntityIndex(out nextIndex))
			{
				ZoneBuiltRequirementData zoneBuiltRequirement = nativeArray2[nextIndex];
				UnlockRequirementData unlockRequirement = nativeArray3[nextIndex];
				if (ShouldUnlock(zoneBuiltRequirement, ref unlockRequirement))
				{
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_UnlockEventArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Unlock(nativeArray[nextIndex]));
				}
				nativeArray3[nextIndex] = unlockRequirement;
			}
		}

		private bool ShouldUnlock(ZoneBuiltRequirementData zoneBuiltRequirement, ref UnlockRequirementData unlockRequirement)
		{
			int num = 0;
			int num2 = 0;
			if (zoneBuiltRequirement.m_RequiredZone != Entity.Null)
			{
				foreach (KeyValue<ZoneBuiltDataKey, ZoneBuiltDataValue> item in m_ZoneBuiltData)
				{
					if (item.Key.m_Zone == zoneBuiltRequirement.m_RequiredZone && item.Key.m_Level >= zoneBuiltRequirement.m_MinimumLevel)
					{
						num += item.Value.m_Squares;
						num2 += item.Value.m_Count;
					}
				}
			}
			else if (zoneBuiltRequirement.m_RequiredTheme != Entity.Null)
			{
				foreach (KeyValue<ZoneBuiltDataKey, ZoneBuiltDataValue> item2 in m_ZoneBuiltData)
				{
					if (!m_ObjectRequirementElements.HasBuffer(item2.Key.m_Zone))
					{
						continue;
					}
					DynamicBuffer<ObjectRequirementElement> dynamicBuffer = m_ObjectRequirementElements[item2.Key.m_Zone];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						ObjectRequirementElement objectRequirementElement = dynamicBuffer[i];
						if (m_ThemeData.HasComponent(objectRequirementElement.m_Requirement) && objectRequirementElement.m_Requirement == zoneBuiltRequirement.m_RequiredTheme)
						{
							num += item2.Value.m_Squares;
							num2 += item2.Value.m_Count;
						}
					}
				}
			}
			else
			{
				foreach (KeyValue<ZoneBuiltDataKey, ZoneBuiltDataValue> item3 in m_ZoneBuiltData)
				{
					if (m_ZoneData.TryGetComponent(item3.Key.m_Zone, out var componentData) && componentData.m_AreaType == zoneBuiltRequirement.m_RequiredType && item3.Key.m_Level >= zoneBuiltRequirement.m_MinimumLevel)
					{
						num += item3.Value.m_Squares;
						num2 += item3.Value.m_Count;
					}
				}
			}
			if (num < zoneBuiltRequirement.m_MinimumSquares || zoneBuiltRequirement.m_MinimumCount == 0)
			{
				unlockRequirement.m_Progress = math.min(num, zoneBuiltRequirement.m_MinimumSquares);
			}
			else
			{
				unlockRequirement.m_Progress = math.min(num2, zoneBuiltRequirement.m_MinimumCount);
			}
			if (num >= zoneBuiltRequirement.m_MinimumSquares)
			{
				return num2 >= zoneBuiltRequirement.m_MinimumCount;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ZoneBuiltRequirementData> __Game_Prefabs_ZoneBuiltRequirementData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ThemeData> __Game_Prefabs_ThemeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_ZoneBuiltRequirementData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ZoneBuiltRequirementData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_ThemeData_RO_ComponentLookup = state.GetComponentLookup<ThemeData>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_UpdatedBuildingsQuery;

	private EntityQuery m_AllBuildingsQuery;

	private EntityQuery m_RequirementQuery;

	private EntityArchetype m_UnlockEventArchetype;

	private NativeParallelHashMap<ZoneBuiltDataKey, ZoneBuiltDataValue> m_ZoneBuiltData;

	private NativeQueue<ZoneBuiltLevelUpdate> m_ZoneBuiltLevelQueue;

	private JobHandle m_WriteDeps;

	private JobHandle m_QueueWriteDeps;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	public NativeQueue<ZoneBuiltLevelUpdate> GetZoneBuiltLevelQueue(out JobHandle deps)
	{
		deps = m_QueueWriteDeps;
		return m_ZoneBuiltLevelQueue;
	}

	public void AddWriter(JobHandle jobHandle)
	{
		m_QueueWriteDeps = JobHandle.CombineDependencies(jobHandle, m_QueueWriteDeps);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_UpdatedBuildingsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Building>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_AllBuildingsQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>());
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneBuiltRequirementData>(), ComponentType.ReadWrite<UnlockRequirementData>(), ComponentType.ReadOnly<Locked>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		m_ZoneBuiltData = new NativeParallelHashMap<ZoneBuiltDataKey, ZoneBuiltDataValue>(20, Allocator.Persistent);
		m_ZoneBuiltLevelQueue = new NativeQueue<ZoneBuiltLevelUpdate>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ZoneBuiltData.Dispose();
		m_ZoneBuiltLevelQueue.Dispose();
		base.OnDestroy();
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = (GetLoaded() ? m_AllBuildingsQuery : m_UpdatedBuildingsQuery);
		if (!entityQuery.IsEmptyIgnoreFilter || !m_ZoneBuiltLevelQueue.IsEmpty())
		{
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> buildingChunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle jobHandle = IJobExtensions.Schedule(new UpdateZoneBuiltDataJob
			{
				m_BuildingChunks = buildingChunks,
				m_ZoneBuiltData = m_ZoneBuiltData,
				m_ZoneBuiltLevelQueue = m_ZoneBuiltLevelQueue,
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef)
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, m_QueueWriteDeps));
			buildingChunks.Dispose(jobHandle);
			m_WriteDeps = jobHandle;
			if (!m_RequirementQuery.IsEmptyIgnoreFilter)
			{
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new ZoneBuiltRequirementJob
				{
					m_ZoneBuiltData = m_ZoneBuiltData,
					m_UnlockEventArchetype = m_UnlockEventArchetype,
					m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_ZoneBuiltRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ZoneBuiltRequirementData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_UnlockRequirementType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
					m_ZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ThemeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ThemeData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ObjectRequirementElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef)
				}, m_RequirementQuery, jobHandle);
				m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
				base.Dependency = jobHandle2;
			}
			else
			{
				base.Dependency = jobHandle;
			}
		}
	}

	public void PreDeserialize(Context context)
	{
		m_WriteDeps.Complete();
		m_ZoneBuiltData.Clear();
		m_Loaded = true;
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
	public ZoneBuiltRequirementSystem()
	{
	}
}
