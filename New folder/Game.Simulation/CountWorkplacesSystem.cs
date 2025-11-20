using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CountWorkplacesSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct CountWorkplacesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<FreeWorkplaces> m_FreeWorkplacesType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDatas;

		public NativeAccumulator<Workplaces>.ParallelWriter m_FreeWorkplaces;

		public NativeAccumulator<Workplaces>.ParallelWriter m_TotalWorkplaces;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Workplaces value = default(Workplaces);
			if (chunk.Has<FreeWorkplaces>())
			{
				NativeArray<FreeWorkplaces> nativeArray = chunk.GetNativeArray(ref m_FreeWorkplacesType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					for (int j = 0; j < 5; j++)
					{
						value[j] += nativeArray[i].GetFree(j);
					}
				}
				m_FreeWorkplaces.Accumulate(value);
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray3 = chunk.GetNativeArray(ref m_WorkProviderType);
			for (int k = 0; k < nativeArray2.Length; k++)
			{
				Entity entity = nativeArray2[k];
				Entity prefab = m_Prefabs[entity].m_Prefab;
				int buildingLevel = 1;
				if (m_PropertyRenters.HasComponent(entity))
				{
					Entity property = m_PropertyRenters[entity].m_Property;
					if (!m_Prefabs.HasComponent(property))
					{
						continue;
					}
					Entity prefab2 = m_Prefabs[property].m_Prefab;
					if (m_SpawnableBuildings.HasComponent(prefab2))
					{
						buildingLevel = m_SpawnableBuildings[prefab2].m_Level;
					}
				}
				else if (m_Buildings.HasComponent(entity) && m_Buildings[entity].m_RoadEdge == Entity.Null)
				{
					continue;
				}
				WorkplaceData workplaceData = m_WorkplaceDatas[prefab];
				value = EconomyUtils.CalculateNumberOfWorkplaces(nativeArray3[k].m_MaxWorkers, workplaceData.m_Complexity, buildingLevel);
				m_TotalWorkplaces.Accumulate(value);
			}
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
		public ComponentTypeHandle<FreeWorkplaces> __Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FreeWorkplaces>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
		}
	}

	private EntityQuery m_WorkplaceQuery;

	private NativeAccumulator<Workplaces> m_FreeWorkplaces;

	private NativeAccumulator<Workplaces> m_TotalWorkplaces;

	public Workplaces m_LastFreeWorkplaces;

	public Workplaces m_LastTotalWorkplaces;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public Workplaces GetFreeWorkplaces()
	{
		return m_LastFreeWorkplaces;
	}

	public Workplaces GetUnemployedWorkspaceByLevel()
	{
		Workplaces result = default(Workplaces);
		int num = 0;
		for (int i = 0; i < 5; i++)
		{
			num = (result[i] = num + m_LastFreeWorkplaces[i]);
		}
		return result;
	}

	public Workplaces GetTotalWorkplaces()
	{
		return m_LastTotalWorkplaces;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WorkplaceQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<WorkProvider>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<Building>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_FreeWorkplaces = new NativeAccumulator<Workplaces>(Allocator.Persistent);
		m_TotalWorkplaces = new NativeAccumulator<Workplaces>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_FreeWorkplaces.Dispose();
		m_TotalWorkplaces.Dispose();
		base.OnDestroy();
	}

	private void Reset()
	{
		if (!m_WasReset)
		{
			m_LastFreeWorkplaces = default(Workplaces);
			m_LastTotalWorkplaces = default(Workplaces);
			m_WasReset = true;
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_FreeWorkplaces.Clear();
		m_TotalWorkplaces.Clear();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Workplaces value = m_LastFreeWorkplaces;
		writer.Write(value);
		Workplaces value2 = m_LastTotalWorkplaces;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.economyFix)
		{
			ref Workplaces value = ref m_LastFreeWorkplaces;
			reader.Read(out value);
			ref Workplaces value2 = ref m_LastTotalWorkplaces;
			reader.Read(out value2);
		}
		else
		{
			NativeArray<int> nativeArray = new NativeArray<int>(5, Allocator.Temp);
			NativeArray<int> value3 = nativeArray;
			reader.Read(value3);
			nativeArray.Dispose();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_WorkplaceQuery.IsEmptyIgnoreFilter)
		{
			Reset();
			return;
		}
		m_WasReset = false;
		m_LastFreeWorkplaces = m_FreeWorkplaces.GetResult();
		m_LastTotalWorkplaces = m_TotalWorkplaces.GetResult();
		m_FreeWorkplaces.Clear();
		m_TotalWorkplaces.Clear();
		CountWorkplacesJob jobData = new CountWorkplacesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_FreeWorkplacesType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_FreeWorkplaces_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableBuildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkplaceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FreeWorkplaces = m_FreeWorkplaces.AsParallelWriter(),
			m_TotalWorkplaces = m_TotalWorkplaces.AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_WorkplaceQuery, base.Dependency);
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
	public CountWorkplacesSystem()
	{
	}
}
