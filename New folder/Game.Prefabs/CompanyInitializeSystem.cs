using System.Runtime.CompilerServices;
using Game.Common;
using Game.Economy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class CompanyInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeAffiliatedBrandsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentTypeHandle<CommercialCompanyData> m_CommercialCompanyDataType;

		[ReadOnly]
		public ComponentTypeHandle<StorageCompanyData> m_StorageCompanyDataType;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProcessData> m_IndustrialProcessDataType;

		public BufferTypeHandle<CompanyBrandElement> m_CompanyBrandElementType;

		public BufferTypeHandle<AffiliatedBrandElement> m_AffiliatedBrandElementType;

		public void Execute()
		{
			NativeParallelMultiHashMap<sbyte, Entity> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<sbyte, Entity>(100, Allocator.Temp);
			NativeParallelMultiHashMap<sbyte, Entity> nativeParallelMultiHashMap2 = new NativeParallelMultiHashMap<sbyte, Entity>(100, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				bool flag = archetypeChunk.Has(ref m_CommercialCompanyDataType);
				bool flag2 = archetypeChunk.Has(ref m_StorageCompanyDataType);
				NativeArray<IndustrialProcessData> nativeArray = archetypeChunk.GetNativeArray(ref m_IndustrialProcessDataType);
				BufferAccessor<CompanyBrandElement> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_CompanyBrandElementType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<CompanyBrandElement> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						if (m_DeletedData.HasComponent(dynamicBuffer[k].m_Brand))
						{
							dynamicBuffer.RemoveAtSwapBack(k--);
						}
					}
				}
				for (int l = 0; l < nativeArray.Length; l++)
				{
					IndustrialProcessData industrialProcessData = nativeArray[l];
					if (!flag2 && industrialProcessData.m_Input1.m_Resource != Resource.NoResource)
					{
						int resourceIndex = EconomyUtils.GetResourceIndex(industrialProcessData.m_Input1.m_Resource);
						DynamicBuffer<CompanyBrandElement> dynamicBuffer2 = bufferAccessor[l];
						for (int m = 0; m < dynamicBuffer2.Length; m++)
						{
							nativeParallelMultiHashMap2.Add((sbyte)resourceIndex, dynamicBuffer2[m].m_Brand);
						}
					}
					if (!flag2 && industrialProcessData.m_Input2.m_Resource != Resource.NoResource)
					{
						int resourceIndex2 = EconomyUtils.GetResourceIndex(industrialProcessData.m_Input2.m_Resource);
						DynamicBuffer<CompanyBrandElement> dynamicBuffer3 = bufferAccessor[l];
						for (int n = 0; n < dynamicBuffer3.Length; n++)
						{
							nativeParallelMultiHashMap2.Add((sbyte)resourceIndex2, dynamicBuffer3[n].m_Brand);
						}
					}
					if (!flag && !flag2 && industrialProcessData.m_Output.m_Resource != Resource.NoResource)
					{
						int resourceIndex3 = EconomyUtils.GetResourceIndex(industrialProcessData.m_Output.m_Resource);
						DynamicBuffer<CompanyBrandElement> dynamicBuffer4 = bufferAccessor[l];
						for (int num = 0; num < dynamicBuffer4.Length; num++)
						{
							nativeParallelMultiHashMap.Add((sbyte)resourceIndex3, dynamicBuffer4[num].m_Brand);
						}
					}
				}
			}
			for (int num2 = 0; num2 < m_Chunks.Length; num2++)
			{
				ArchetypeChunk archetypeChunk2 = m_Chunks[num2];
				bool flag3 = archetypeChunk2.Has(ref m_CommercialCompanyDataType);
				bool flag4 = archetypeChunk2.Has(ref m_StorageCompanyDataType);
				NativeArray<IndustrialProcessData> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_IndustrialProcessDataType);
				BufferAccessor<AffiliatedBrandElement> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_AffiliatedBrandElementType);
				for (int num3 = 0; num3 < bufferAccessor2.Length; num3++)
				{
					IndustrialProcessData industrialProcessData2 = nativeArray2[num3];
					DynamicBuffer<AffiliatedBrandElement> dynamicBuffer5 = bufferAccessor2[num3];
					dynamicBuffer5.Clear();
					if (!flag4 && industrialProcessData2.m_Input1.m_Resource != Resource.NoResource)
					{
						int resourceIndex4 = EconomyUtils.GetResourceIndex(industrialProcessData2.m_Input1.m_Resource);
						if (nativeParallelMultiHashMap.TryGetFirstValue((sbyte)resourceIndex4, out var item, out var it))
						{
							do
							{
								dynamicBuffer5.Add(new AffiliatedBrandElement
								{
									m_Brand = item
								});
							}
							while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
						}
					}
					if (!flag4 && industrialProcessData2.m_Input2.m_Resource != Resource.NoResource)
					{
						int resourceIndex5 = EconomyUtils.GetResourceIndex(industrialProcessData2.m_Input2.m_Resource);
						if (nativeParallelMultiHashMap.TryGetFirstValue((sbyte)resourceIndex5, out var item2, out var it2))
						{
							do
							{
								dynamicBuffer5.Add(new AffiliatedBrandElement
								{
									m_Brand = item2
								});
							}
							while (nativeParallelMultiHashMap.TryGetNextValue(out item2, ref it2));
						}
					}
					if (!flag3 && industrialProcessData2.m_Output.m_Resource != Resource.NoResource)
					{
						int resourceIndex6 = EconomyUtils.GetResourceIndex(industrialProcessData2.m_Output.m_Resource);
						if (nativeParallelMultiHashMap2.TryGetFirstValue((sbyte)resourceIndex6, out var item3, out var it3))
						{
							do
							{
								dynamicBuffer5.Add(new AffiliatedBrandElement
								{
									m_Brand = item3
								});
							}
							while (nativeParallelMultiHashMap2.TryGetNextValue(out item3, ref it3));
						}
						if (flag4 && nativeParallelMultiHashMap.TryGetFirstValue((sbyte)resourceIndex6, out item3, out it3))
						{
							do
							{
								dynamicBuffer5.Add(new AffiliatedBrandElement
								{
									m_Brand = item3
								});
							}
							while (nativeParallelMultiHashMap.TryGetNextValue(out item3, ref it3));
						}
					}
					if (dynamicBuffer5.Length >= 3)
					{
						dynamicBuffer5.AsNativeArray().Sort();
					}
					int num4 = 0;
					AffiliatedBrandElement value = default(AffiliatedBrandElement);
					for (int num5 = 0; num5 < dynamicBuffer5.Length; num5++)
					{
						AffiliatedBrandElement affiliatedBrandElement = dynamicBuffer5[num5];
						if (affiliatedBrandElement.m_Brand != value.m_Brand)
						{
							if (value.m_Brand != Entity.Null)
							{
								dynamicBuffer5[num4++] = value;
							}
							value = affiliatedBrandElement;
						}
					}
					if (value.m_Brand != Entity.Null)
					{
						dynamicBuffer5[num4++] = value;
					}
					if (num4 < dynamicBuffer5.Length)
					{
						dynamicBuffer5.RemoveRange(num4, dynamicBuffer5.Length - num4);
					}
					dynamicBuffer5.TrimExcess();
				}
			}
			nativeParallelMultiHashMap.Dispose();
			nativeParallelMultiHashMap2.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<BrandData> __Game_Prefabs_BrandData_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CommercialCompanyData> __Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle;

		public BufferTypeHandle<CompanyBrandElement> __Game_Prefabs_CompanyBrandElement_RW_BufferTypeHandle;

		public BufferTypeHandle<AffiliatedBrandElement> __Game_Prefabs_AffiliatedBrandElement_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_BrandData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<BrandData>();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialCompanyData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_CompanyBrandElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<CompanyBrandElement>();
			__Game_Prefabs_AffiliatedBrandElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<AffiliatedBrandElement>();
		}
	}

	private EntityQuery m_PrefabQuery;

	private EntityQuery m_CompanyQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<BrandData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_CompanyQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<CompanyBrandElement>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<BrandData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BrandData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		CompleteDependency();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ArchetypeChunk archetypeChunk = nativeArray[i];
			if (archetypeChunk.Has(ref typeHandle))
			{
				continue;
			}
			NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
			if (!archetypeChunk.Has(ref typeHandle3))
			{
				continue;
			}
			NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(entityTypeHandle);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity brand = nativeArray3[j];
				BrandPrefab prefab = m_PrefabSystem.GetPrefab<BrandPrefab>(nativeArray2[j]);
				for (int k = 0; k < prefab.m_Companies.Length; k++)
				{
					CompanyPrefab prefab2 = prefab.m_Companies[k];
					m_PrefabSystem.GetBuffer<CompanyBrandElement>(prefab2, isReadOnly: false).Add(new CompanyBrandElement(brand));
				}
			}
		}
		nativeArray.Dispose();
		JobHandle outJobHandle;
		InitializeAffiliatedBrandsJob jobData = new InitializeAffiliatedBrandsJob
		{
			m_Chunks = m_CompanyQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CommercialCompanyDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CommercialCompanyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StorageCompanyDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialProcessDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyBrandElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_CompanyBrandElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_AffiliatedBrandElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_AffiliatedBrandElement_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, outJobHandle);
		jobData.m_Chunks.Dispose(base.Dependency);
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
	public CompanyInitializeSystem()
	{
	}
}
