using System.Runtime.CompilerServices;
using Game.Common;
using Game.Events;
using Game.Notifications;
using Game.Policies;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class BuildingPoliciesSystem : GameSystemBase
{
	[BurstCompile]
	private struct CheckBuildingsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Modify> m_ModifyType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> m_OnFireType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Extension> m_ExtensionData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<BuildingOptionData> m_BuildingOptionData;

		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Modify> nativeArray = chunk.GetNativeArray(ref m_ModifyType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Modify modify = nativeArray[i];
					Extension componentData3;
					if (m_BuildingData.TryGetComponent(modify.m_Entity, out var componentData))
					{
						if (m_BuildingOptionData.TryGetComponent(modify.m_Policy, out var componentData2) && BuildingUtils.HasOption(componentData2, BuildingOption.Inactive))
						{
							if ((modify.m_Flags & PolicyFlags.Active) != 0)
							{
								m_IconCommandBuffer.Add(modify.m_Entity, m_BuildingConfigurationData.m_TurnedOffNotification);
							}
							else
							{
								m_IconCommandBuffer.Remove(modify.m_Entity, m_BuildingConfigurationData.m_TurnedOffNotification);
							}
						}
						if (m_DestroyedData.HasComponent(modify.m_Entity) || m_OnFireData.HasComponent(modify.m_Entity))
						{
							componentData.m_OptionMask = 2u;
						}
					}
					else if (m_ExtensionData.TryGetComponent(modify.m_Entity, out componentData3) && (componentData3.m_Flags & ExtensionFlags.Disabled) != ExtensionFlags.None)
					{
						componentData.m_OptionMask = 2u;
					}
					if (!m_ServiceUpgradeData.HasComponent(modify.m_Entity) || !m_OwnerData.TryGetComponent(modify.m_Entity, out var componentData4) || !m_InstalledUpgrades.TryGetBuffer(componentData4.m_Owner, out var bufferData))
					{
						continue;
					}
					for (int j = 0; j < bufferData.Length; j++)
					{
						ref InstalledUpgrade reference = ref bufferData.ElementAt(j);
						if (reference.m_Upgrade == modify.m_Entity)
						{
							reference.m_OptionMask = componentData.m_OptionMask;
							break;
						}
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<Owner> nativeArray4 = chunk.GetNativeArray(ref m_OwnerType);
			bool flag = chunk.Has(ref m_DestroyedType);
			bool flag2 = chunk.Has(ref m_OnFireType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity entity = nativeArray2[k];
				Building building = nativeArray3[k];
				Owner owner = nativeArray4[k];
				if (flag || flag2)
				{
					building.m_OptionMask = 2u;
				}
				if (!m_InstalledUpgrades.TryGetBuffer(owner.m_Owner, out var bufferData2))
				{
					continue;
				}
				for (int l = 0; l < bufferData2.Length; l++)
				{
					ref InstalledUpgrade reference2 = ref bufferData2.ElementAt(l);
					if (reference2.m_Upgrade == entity)
					{
						reference2.m_OptionMask = building.m_OptionMask;
						break;
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
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Modify> __Game_Policies_Modify_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OnFire> __Game_Events_OnFire_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingOptionData> __Game_Prefabs_BuildingOptionData_RO_ComponentLookup;

		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Policies_Modify_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Modify>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OnFire>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgrade>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Prefabs_BuildingOptionData_RO_ComponentLookup = state.GetComponentLookup<BuildingOptionData>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RW_BufferLookup = state.GetBufferLookup<InstalledUpgrade>();
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_PolicyModifyQuery;

	private EntityQuery m_BuildingSettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_PolicyModifyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Modify>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<ServiceUpgrade>(),
				ComponentType.ReadOnly<Building>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_BuildingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		RequireForUpdate(m_PolicyModifyQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.Schedule(new CheckBuildingsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ModifyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Policies_Modify_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OnFireType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingOptionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingOptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RW_BufferLookup, ref base.CheckedStateRef),
			m_BuildingConfigurationData = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		}, m_PolicyModifyQuery, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public BuildingPoliciesSystem()
	{
	}
}
