using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Citizens;

[CompilerGenerated]
public class CompanyInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeCompanyJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ProcessingCompany> m_ProcessingCompanyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ExtractorCompany> m_ExtractionCompanyType;

		public ComponentTypeHandle<CompanyData> m_CompanyType;

		public ComponentTypeHandle<Profitability> m_ProfitabilityType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;

		[ReadOnly]
		public BufferLookup<CompanyBrandElement> m_Brands;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<RentAction>.ParallelWriter m_RentActionQueue;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CompanyData> nativeArray3 = chunk.GetNativeArray(ref m_CompanyType);
			NativeArray<Profitability> nativeArray4 = chunk.GetNativeArray(ref m_ProfitabilityType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesType);
			NativeArray<ServiceAvailable> nativeArray5 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			NativeArray<LodgingProvider> nativeArray6 = chunk.GetNativeArray(ref m_LodgingProviderType);
			bool flag = nativeArray5.Length != 0;
			bool flag2 = chunk.Has(ref m_ProcessingCompanyType);
			bool flag3 = chunk.Has(ref m_ExtractionCompanyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				Random random = m_RandomSeed.GetRandom(entity.Index);
				DynamicBuffer<CompanyBrandElement> dynamicBuffer = m_Brands[prefab];
				Entity brand = ((dynamicBuffer.Length != 0) ? dynamicBuffer[random.NextInt(dynamicBuffer.Length)].m_Brand : Entity.Null);
				nativeArray3[i] = new CompanyData
				{
					m_RandomSeed = random,
					m_Brand = brand
				};
				nativeArray4[i] = new Profitability
				{
					m_Profitability = 127
				};
				if (flag)
				{
					ServiceCompanyData serviceCompanyData = m_ServiceCompanyDatas[prefab];
					nativeArray5[i] = new ServiceAvailable
					{
						m_ServiceAvailable = serviceCompanyData.m_MaxService / 2,
						m_MeanPriority = 0f
					};
				}
				if (flag2)
				{
					IndustrialProcessData industrialProcessData = m_ProcessDatas[prefab];
					DynamicBuffer<Resources> dynamicBuffer2 = bufferAccessor[i];
					if (flag)
					{
						AddStartingResources(dynamicBuffer2, industrialProcessData.m_Input1.m_Resource, 3000);
						AddStartingResources(dynamicBuffer2, industrialProcessData.m_Input2.m_Resource, 3000);
					}
					else
					{
						AddStartingResources(dynamicBuffer2, industrialProcessData.m_Input1.m_Resource, 15000);
						AddStartingResources(dynamicBuffer2, industrialProcessData.m_Input2.m_Resource, 15000);
						bool flag4 = EconomyUtils.IsResourceHasWeight(industrialProcessData.m_Output.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
						if (!flag3)
						{
							AddStartingResources(dynamicBuffer2, industrialProcessData.m_Output.m_Resource, flag4 ? 1000 : 0);
						}
						else
						{
							float num = 1000f * EconomyUtils.GetIndustrialPrice(industrialProcessData.m_Output.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							EconomyUtils.AddResources(Resource.Money, (int)num, dynamicBuffer2);
						}
					}
				}
				if (m_PropertyRenters.HasComponent(entity) && m_PropertyRenters[entity].m_Property != Entity.Null)
				{
					m_RentActionQueue.Enqueue(new RentAction
					{
						m_Property = m_PropertyRenters[entity].m_Property,
						m_Renter = entity
					});
				}
			}
			for (int j = 0; j < nativeArray6.Length; j++)
			{
				nativeArray6[j] = new LodgingProvider
				{
					m_FreeRooms = 0,
					m_Price = -1
				};
			}
		}

		private void AddStartingResources(DynamicBuffer<Resources> buffer, Resource resource, int amount)
		{
			if (resource != Resource.NoResource)
			{
				int num = (int)math.round((float)amount * EconomyUtils.GetIndustrialPrice(resource, m_ResourcePrefabs, ref m_ResourceDatas));
				EconomyUtils.AddResources(resource, amount, buffer);
				EconomyUtils.AddResources(Resource.Money, -num, buffer);
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ProcessingCompany> __Game_Companies_ProcessingCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ExtractorCompany> __Game_Companies_ExtractorCompany_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CompanyData> __Game_Companies_CompanyData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Profitability> __Game_Companies_Profitability_RW_ComponentTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RW_ComponentTypeHandle;

		public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<CompanyBrandElement> __Game_Prefabs_CompanyBrandElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_ProcessingCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.ProcessingCompany>(isReadOnly: true);
			__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.ExtractorCompany>(isReadOnly: true);
			__Game_Companies_CompanyData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyData>();
			__Game_Companies_Profitability_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Profitability>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>();
			__Game_Companies_LodgingProvider_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>();
			__Game_Prefabs_CompanyBrandElement_RO_BufferLookup = state.GetBufferLookup<CompanyBrandElement>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		}
	}

	private ResourceSystem m_ResourceSystem;

	private PropertyProcessingSystem m_PropertyProcessingSystem;

	private EntityQuery m_CreatedGroup;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1030701298_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_PropertyProcessingSystem = base.World.GetOrCreateSystemManaged<PropertyProcessingSystem>();
		m_CreatedGroup = GetEntityQuery(ComponentType.ReadWrite<CompanyData>(), ComponentType.ReadWrite<Profitability>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatedGroup);
		RequireForUpdate<EconomyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		InitializeCompanyJob jobData = new InitializeCompanyJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ProcessingCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ProcessingCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractionCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyData_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ProfitabilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_Profitability_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LodgingProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_LodgingProvider_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Brands = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CompanyBrandElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_RandomSeed = RandomSeed.Next(),
			m_EconomyParameters = __query_1030701298_0.GetSingleton<EconomyParameterData>(),
			m_RentActionQueue = m_PropertyProcessingSystem.GetRentActionQueue(out deps).AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_PropertyProcessingSystem.AddWriter(base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1030701298_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
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
