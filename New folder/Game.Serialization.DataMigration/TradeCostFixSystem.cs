using System.Runtime.CompilerServices;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization.DataMigration;

[CompilerGenerated]
public class TradeCostFixSystem : GameSystemBase
{
	[BurstCompile]
	private struct TradeCostFixJob : IJobChunk
	{
		public BufferTypeHandle<TradeCost> m_TradeCostBufType;

		public BufferTypeHandle<Resources> m_ResourcesBufType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<TradeCost> bufferAccessor = chunk.GetBufferAccessor(ref m_TradeCostBufType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				bufferAccessor[i].Clear();
			}
			if (!chunk.Has<Game.Objects.OutsideConnection>())
			{
				return;
			}
			BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourcesBufType);
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int j = 0; j < bufferAccessor2.Length; j++)
			{
				DynamicBuffer<Resources> resources = bufferAccessor2[j];
				PrefabRef prefabRef = nativeArray[j];
				StorageCompanyData storageCompanyData = m_StorageCompanyDatas[prefabRef];
				StorageLimitData storageLimitData = m_StorageLimitDatas[prefabRef];
				ResourceIterator iterator = ResourceIterator.GetIterator();
				int num = EconomyUtils.CountResources(storageCompanyData.m_StoredResources);
				while (iterator.Next())
				{
					if ((storageCompanyData.m_StoredResources & iterator.resource) != Resource.NoResource)
					{
						if (iterator.resource == Resource.OutgoingMail)
						{
							EconomyUtils.SetResources(Resource.OutgoingMail, resources, 0);
							continue;
						}
						int num2 = storageLimitData.m_Limit / num;
						EconomyUtils.SetResources(iterator.resource, resources, num2 / 2);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RW_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_TradeCost_RW_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private TradeSystem m_TradeSystem;

	private EntityQuery m_TradeCostQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_TradeSystem = base.World.GetOrCreateSystemManaged<TradeSystem>();
		m_TradeCostQuery = GetEntityQuery(ComponentType.ReadOnly<TradeCost>(), ComponentType.Exclude<Created>(), ComponentType.Exclude<Deleted>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_LoadGameSystem.context.format.Has(FormatTags.TradeCostFix))
		{
			if (!m_TradeCostQuery.IsEmptyIgnoreFilter)
			{
				TradeCostFixJob jobData = new TradeCostFixJob
				{
					m_TradeCostBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferTypeHandle, ref base.CheckedStateRef),
					m_ResourcesBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_StorageLimitDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef)
				};
				base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TradeCostQuery, base.Dependency);
			}
			m_TradeSystem.SetDefaults();
		}
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
	public TradeCostFixSystem()
	{
	}
}
