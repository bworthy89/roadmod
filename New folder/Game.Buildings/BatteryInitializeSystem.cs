using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class BatteryInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeBatteryJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Battery> m_BatteryType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BatteryData> m_BatteryDatas;

		[ReadOnly]
		public ComponentLookup<Created> m_CreatedData;

		public IconCommandBuffer m_IconCommandBuffer;

		public ElectricityParameterData m_ElectricityParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Battery> nativeArray3 = chunk.GetNativeArray(ref m_BatteryType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			bool flag = chunk.Has(ref m_CreatedType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref Battery reference = ref nativeArray3.ElementAt(i);
				if (flag)
				{
					ProcessAddition(nativeArray2[i].m_Prefab, ref reference);
				}
				if (bufferAccessor.Length != 0)
				{
					foreach (InstalledUpgrade item in bufferAccessor[i])
					{
						if (m_CreatedData.HasComponent(item.m_Upgrade) && m_Prefabs.TryGetComponent(item.m_Upgrade, out var componentData))
						{
							ProcessAddition(componentData.m_Prefab, ref reference);
						}
					}
				}
				if (reference.m_StoredEnergy == 0L)
				{
					m_IconCommandBuffer.Add(nativeArray[i], m_ElectricityParameterData.m_BatteryEmptyNotificationPrefab, IconPriority.Problem);
				}
			}
		}

		private void ProcessAddition(Entity prefab, ref Battery battery)
		{
			if (m_BatteryDatas.TryGetComponent(prefab, out var componentData))
			{
				battery.m_StoredEnergy += (long)(m_ElectricityParameterData.m_InitialBatteryCharge * (float)componentData.capacityTicks);
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

		public ComponentTypeHandle<Battery> __Game_Buildings_Battery_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BatteryData> __Game_Prefabs_BatteryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Battery_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Battery>();
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BatteryData_RO_ComponentLookup = state.GetComponentLookup<BatteryData>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_Additions;

	private EntityQuery m_SettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_Additions = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadWrite<Battery>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<ServiceUpgrade>()
			}
		});
		m_SettingsQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>());
		RequireForUpdate(m_Additions);
		RequireForUpdate(m_SettingsQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitializeBatteryJob jobData = new InitializeBatteryJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Battery_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BatteryDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BatteryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_ElectricityParameterData = m_SettingsQuery.GetSingleton<ElectricityParameterData>()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_Additions, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public BatteryInitializeSystem()
	{
	}
}
