using System;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Serialization;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BrandPopularitySystem : GameSystemBase, IPreDeserialize
{
	public struct BrandPopularity : IComparable<BrandPopularity>
	{
		public Entity m_BrandPrefab;

		public int m_Popularity;

		public int CompareTo(BrandPopularity other)
		{
			return other.m_Popularity - m_Popularity;
		}
	}

	[BurstCompile]
	private struct UpdateBrandPopularityJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CompanyChunks;

		[ReadOnly]
		public ComponentTypeHandle<CompanyData> m_CompanyDataType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_CompanyRentPropertyType;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructions;

		public NativeList<BrandPopularity> m_BrandPopularity;

		public void Execute()
		{
			m_BrandPopularity.Clear();
			if (m_CompanyChunks.Length == 0)
			{
				return;
			}
			NativeParallelHashMap<Entity, int> nativeParallelHashMap = new NativeParallelHashMap<Entity, int>(100, Allocator.Temp);
			for (int i = 0; i < m_CompanyChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_CompanyChunks[i];
				NativeArray<CompanyData> nativeArray = archetypeChunk.GetNativeArray(ref m_CompanyDataType);
				NativeArray<PropertyRenter> nativeArray2 = archetypeChunk.GetNativeArray(ref m_CompanyRentPropertyType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					CompanyData companyData = nativeArray[j];
					PropertyRenter propertyRenter = nativeArray2[j];
					if (companyData.m_Brand != Entity.Null && propertyRenter.m_Property != Entity.Null && !m_UnderConstructions.HasComponent(propertyRenter.m_Property))
					{
						if (nativeParallelHashMap.TryGetValue(companyData.m_Brand, out var item))
						{
							nativeParallelHashMap[companyData.m_Brand] = item + 1;
							continue;
						}
						nativeParallelHashMap.Add(companyData.m_Brand, 1);
						ref NativeList<BrandPopularity> reference = ref m_BrandPopularity;
						BrandPopularity value = new BrandPopularity
						{
							m_BrandPrefab = companyData.m_Brand
						};
						reference.Add(in value);
					}
				}
			}
			for (int k = 0; k < m_BrandPopularity.Length; k++)
			{
				BrandPopularity value2 = m_BrandPopularity[k];
				value2.m_Popularity = nativeParallelHashMap[value2.m_BrandPrefab];
				m_BrandPopularity[k] = value2;
			}
			nativeParallelHashMap.Dispose();
			m_BrandPopularity.Sort();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<CompanyData> __Game_Companies_CompanyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_CompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
		}
	}

	private EntityQuery m_ModifiedQuery;

	private NativeList<BrandPopularity> m_BrandPopularity;

	private JobHandle m_Readers;

	public const int kUpdatesPerDay = 128;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 2048;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CompanyData>(),
				ComponentType.ReadOnly<PropertyRenter>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_BrandPopularity = new NativeList<BrandPopularity>(Allocator.Persistent);
		RequireForUpdate(m_ModifiedQuery);
	}

	public void PreDeserialize(Context context)
	{
		m_BrandPopularity.Clear();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_BrandPopularity.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> companyChunks = m_ModifiedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new UpdateBrandPopularityJob
		{
			m_CompanyChunks = companyChunks,
			m_CompanyDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyRentPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnderConstructions = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BrandPopularity = m_BrandPopularity
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, m_Readers));
		companyChunks.Dispose(jobHandle);
		base.Dependency = jobHandle;
		m_Readers = default(JobHandle);
	}

	public NativeList<BrandPopularity> ReadBrandPopularity(out JobHandle dependency)
	{
		dependency = base.Dependency;
		return m_BrandPopularity;
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
	public BrandPopularitySystem()
	{
	}
}
