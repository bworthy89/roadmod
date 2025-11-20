using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class XPBuiltSystem : GameSystemBase
{
	[BurstCompile]
	public struct XPBuiltJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectDatas;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_SignatureBuildingDatas;

		[ReadOnly]
		public ComponentLookup<PlacedSignatureBuildingData> m_PlacedSignatureBuildingDatas;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_ServiceUpgradeDatas;

		public NativeQueue<XPGain> m_XPQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity prefab = nativeArray2[i].m_Prefab;
				if (m_PlaceableObjectDatas.HasComponent(prefab) && !m_PlacedSignatureBuildingDatas.HasComponent(prefab))
				{
					PlaceableObjectData placeableObjectData = m_PlaceableObjectDatas[prefab];
					if (placeableObjectData.m_XPReward > 0)
					{
						m_XPQueue.Enqueue(new XPGain
						{
							amount = placeableObjectData.m_XPReward,
							entity = nativeArray[i],
							reason = XPReason.ServiceBuilding
						});
					}
					if (m_SignatureBuildingDatas.HasComponent(prefab))
					{
						m_CommandBuffer.AddComponent<PlacedSignatureBuildingData>(unfilteredChunkIndex, prefab);
					}
				}
				if (m_ServiceUpgradeDatas.HasComponent(prefab))
				{
					ServiceUpgradeData serviceUpgradeData = m_ServiceUpgradeDatas[prefab];
					if (serviceUpgradeData.m_XPReward > 0)
					{
						m_XPQueue.Enqueue(new XPGain
						{
							amount = serviceUpgradeData.m_XPReward,
							entity = nativeArray[i],
							reason = XPReason.ServiceUpgrade
						});
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	public struct XPElectricityJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ElectricityConsumer> m_ElectricityConsumers;

		public NativeQueue<XPGain> m_XPQueue;

		[ReadOnly]
		public Entity m_City;

		public ComponentLookup<XP> m_CityXPs;

		public void Execute()
		{
			for (int i = 0; i < m_ElectricityConsumers.Length; i++)
			{
				if (m_ElectricityConsumers[i].m_FulfilledConsumption > 0)
				{
					m_XPQueue.Enqueue(new XPGain
					{
						amount = kElectricityGridXPBonus,
						entity = Entity.Null,
						reason = XPReason.ElectricityNetwork
					});
					XP value = m_CityXPs[m_City];
					value.m_XPRewardRecord |= XPRewardFlags.ElectricityGridBuilt;
					m_CityXPs[m_City] = value;
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlacedSignatureBuildingData> __Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlacedSignatureBuildingData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_City_XP_RW_ComponentLookup = state.GetComponentLookup<XP>();
		}
	}

	private EntityQuery m_BuiltGroup;

	private EntityQuery m_ElectricityGroup;

	private XPSystem m_XPSystem;

	private ToolSystem m_ToolSystem;

	private CitySystem m_CitySystem;

	private ModificationEndBarrier m_ModificationEndBarrier;

	private static readonly int kElectricityGridXPBonus = 25;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_XPSystem = base.World.GetOrCreateSystemManaged<XPSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ModificationEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_BuiltGroup = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		m_ElectricityGroup = GetEntityQuery(ComponentType.ReadOnly<ElectricityConsumer>(), ComponentType.Exclude<Temp>());
		RequireAnyForUpdate(m_BuiltGroup, m_ElectricityGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.actionMode.IsGame())
		{
			JobHandle deps;
			NativeQueue<XPGain> queue = m_XPSystem.GetQueue(out deps);
			if (!m_BuiltGroup.IsEmptyIgnoreFilter)
			{
				XPBuiltJob jobData = new XPBuiltJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RW_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PlaceableObjectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SignatureBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_PlacedSignatureBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlacedSignatureBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ServiceUpgradeDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_XPQueue = queue,
					m_CommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter()
				};
				base.Dependency = JobChunkExtensions.Schedule(jobData, m_BuiltGroup, JobHandle.CombineDependencies(deps, base.Dependency));
			}
			if ((base.EntityManager.GetComponentData<XP>(m_CitySystem.City).m_XPRewardRecord & XPRewardFlags.ElectricityGridBuilt) == 0 && !m_ElectricityGroup.IsEmptyIgnoreFilter)
			{
				XPElectricityJob jobData2 = new XPElectricityJob
				{
					m_ElectricityConsumers = m_ElectricityGroup.ToComponentDataArray<ElectricityConsumer>(Allocator.TempJob),
					m_City = m_CitySystem.City,
					m_CityXPs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref base.CheckedStateRef),
					m_XPQueue = queue
				};
				base.Dependency = IJobExtensions.Schedule(jobData2, base.Dependency);
			}
			m_XPSystem.AddQueueWriter(base.Dependency);
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
	public XPBuiltSystem()
	{
	}
}
