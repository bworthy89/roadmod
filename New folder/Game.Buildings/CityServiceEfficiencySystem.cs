using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class CityServiceEfficiencySystem : GameSystemBase
{
	[BurstCompile]
	private struct BuildingStateEfficiencyJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectDatas;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> m_ServiceUpkeepDatas;

		[ReadOnly]
		public DynamicBuffer<ServiceBudgetData> m_ServiceBudgets;

		public AnimationCurve1 m_ServiceBudgetEfficiencyFactor;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				if (m_ServiceObjectDatas.TryGetComponent(nativeArray[i], out var componentData))
				{
					float efficiency;
					if (HasMoneyUpkeep(nativeArray[i]) || (bufferAccessor.Length != 0 && HasMoneyUpkeep(bufferAccessor[i])))
					{
						int serviceBudget = GetServiceBudget(componentData.m_Service);
						efficiency = m_ServiceBudgetEfficiencyFactor.Evaluate((float)serviceBudget / 100f);
					}
					else
					{
						efficiency = 1f;
					}
					BuildingUtils.SetEfficiencyFactor(bufferAccessor2[i], EfficiencyFactor.ServiceBudget, efficiency);
				}
			}
		}

		private bool HasMoneyUpkeep(Entity prefab)
		{
			if (m_ServiceUpkeepDatas.TryGetBuffer(prefab, out var bufferData))
			{
				foreach (ServiceUpkeepData item in bufferData)
				{
					if (item.m_Upkeep.m_Resource == Resource.Money)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool HasMoneyUpkeep(DynamicBuffer<InstalledUpgrade> installedUpgrades)
		{
			foreach (InstalledUpgrade item in installedUpgrades)
			{
				if (m_Prefabs.TryGetComponent(item, out var componentData) && HasMoneyUpkeep(componentData))
				{
					return true;
				}
			}
			return false;
		}

		private int GetServiceBudget(Entity service)
		{
			foreach (ServiceBudgetData item in m_ServiceBudgets)
			{
				if (item.m_Service == service)
				{
					return item.m_Budget;
				}
			}
			return 100;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpkeepData> __Game_Prefabs_ServiceUpkeepData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup = state.GetBufferLookup<ServiceUpkeepData>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedBudgetQuery;

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_ChangedBuildingQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_339138653_0;

	private EntityQuery __query_339138653_1;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedBudgetQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>(), ComponentType.ReadOnly<Updated>());
		m_ChangedBuildingQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<CityServiceUpkeep>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadWrite<Efficiency>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<CityServiceUpkeep>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<Efficiency>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireAnyForUpdate(m_UpdatedBudgetQuery, m_ChangedBuildingQuery);
		RequireForUpdate<ServiceBudgetData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		BuildingEfficiencyParameterData singleton = __query_339138653_0.GetSingleton<BuildingEfficiencyParameterData>();
		BuildingStateEfficiencyJob jobData = new BuildingStateEfficiencyJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpkeepDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpkeepData_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceBudgets = __query_339138653_1.GetSingletonBuffer<ServiceBudgetData>(isReadOnly: true),
			m_ServiceBudgetEfficiencyFactor = singleton.m_ServiceBudgetEfficiencyFactor
		};
		EntityQuery query = ((!m_UpdatedBudgetQuery.IsEmptyIgnoreFilter) ? m_BuildingQuery : m_ChangedBuildingQuery);
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_339138653_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAllRW<ServiceBudgetData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_339138653_1 = entityQueryBuilder2.Build(ref state);
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
	public CityServiceEfficiencySystem()
	{
	}
}
