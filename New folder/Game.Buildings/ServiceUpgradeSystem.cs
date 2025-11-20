using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Buildings;

[CompilerGenerated]
public class ServiceUpgradeSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpgradeQuery;

	private EntityQuery m_UpgradePrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_UpgradeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<ServiceUpgrade>(),
				ComponentType.ReadOnly<Object>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<InstalledUpgrade>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UpgradePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceUpgradeData>(), ComponentType.ReadOnly<PrefabData>());
		RequireForUpdate(m_UpgradeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityCommandBuffer commandBuffer = m_ModificationBarrier.CreateCommandBuffer();
		ComponentTypeHandle<Deleted> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Owner> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ServiceUpgrade> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabRef> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<InstalledUpgrade> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef);
		NativeArray<ArchetypeChunk> nativeArray = m_UpgradeQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				if (archetypeChunk.Has(ref typeHandle3))
				{
					NativeArray<Owner> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
					NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle4);
					if (archetypeChunk.Has(ref typeHandle))
					{
						for (int j = 0; j < nativeArray2.Length; j++)
						{
							UpgradeRemoved(commandBuffer, nativeArray2[j], nativeArray3[j]);
						}
					}
					else
					{
						for (int k = 0; k < nativeArray2.Length; k++)
						{
							UpgradeInstalled(commandBuffer, nativeArray2[k], nativeArray3[k]);
						}
					}
				}
				else
				{
					BufferAccessor<InstalledUpgrade> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
					for (int l = 0; l < bufferAccessor.Length; l++)
					{
						OwnerDeleted(commandBuffer, bufferAccessor[l]);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void UpgradeInstalled(EntityCommandBuffer commandBuffer, Owner owner, PrefabRef prefabRef)
	{
		List<ComponentBase> components = m_PrefabSystem.GetPrefab<PrefabBase>(prefabRef).components;
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] is IServiceUpgrade serviceUpgrade)
			{
				serviceUpgrade.GetUpgradeComponents(hashSet);
			}
		}
		foreach (ComponentType item in hashSet)
		{
			if (!base.EntityManager.HasComponent(owner.m_Owner, item))
			{
				commandBuffer.AddComponent(owner.m_Owner, item);
			}
		}
	}

	private void UpgradeRemoved(EntityCommandBuffer commandBuffer, Owner owner, PrefabRef prefabRef)
	{
		if (base.EntityManager.HasComponent<Deleted>(owner.m_Owner))
		{
			return;
		}
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		HashSet<ComponentType> hashSet2 = new HashSet<ComponentType>();
		HashSet<IServiceUpgrade> hashSet3 = new HashSet<IServiceUpgrade>();
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabRef, out var prefab))
		{
			List<ComponentBase> components = prefab.components;
			for (int i = 0; i < components.Count; i++)
			{
				if (components[i] is IServiceUpgrade serviceUpgrade)
				{
					serviceUpgrade.GetUpgradeComponents(hashSet);
					hashSet3.Add(serviceUpgrade);
				}
			}
		}
		else
		{
			NativeArray<PrefabData> nativeArray = m_UpgradePrefabQuery.ToComponentDataArray<PrefabData>(Allocator.Temp);
			foreach (PrefabData item in nativeArray)
			{
				List<ComponentBase> components2 = m_PrefabSystem.GetPrefab<PrefabBase>(item).components;
				for (int j = 0; j < components2.Count; j++)
				{
					if (components2[j] is IServiceUpgrade serviceUpgrade2)
					{
						serviceUpgrade2.GetUpgradeComponents(hashSet);
						hashSet3.Add(serviceUpgrade2);
					}
				}
			}
			nativeArray.Dispose();
		}
		PrefabRef componentData = base.EntityManager.GetComponentData<PrefabRef>(owner.m_Owner);
		DynamicBuffer<InstalledUpgrade> buffer = base.EntityManager.GetBuffer<InstalledUpgrade>(owner.m_Owner, isReadOnly: true);
		if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(componentData, out var prefab2))
		{
			return;
		}
		List<ComponentBase> components3 = prefab2.components;
		for (int k = 0; k < components3.Count; k++)
		{
			components3[k].GetArchetypeComponents(hashSet2);
		}
		foreach (InstalledUpgrade item2 in buffer)
		{
			PrefabRef componentData2 = base.EntityManager.GetComponentData<PrefabRef>(item2.m_Upgrade);
			if (!m_PrefabSystem.TryGetPrefab<PrefabBase>(componentData2, out var prefab3))
			{
				continue;
			}
			List<ComponentBase> components4 = prefab3.components;
			for (int l = 0; l < components4.Count; l++)
			{
				if (components4[l] is IServiceUpgrade serviceUpgrade3)
				{
					serviceUpgrade3.GetUpgradeComponents(hashSet2);
					hashSet3.Add(serviceUpgrade3);
				}
			}
		}
		foreach (ComponentType item3 in hashSet)
		{
			if (!hashSet2.Contains(item3) && base.EntityManager.HasComponent(owner.m_Owner, item3))
			{
				commandBuffer.RemoveComponent(owner.m_Owner, item3);
			}
		}
		foreach (IServiceUpgrade item4 in hashSet3)
		{
			item4.DoActionWithOwnerAfterRemove(base.EntityManager, owner.m_Owner);
		}
	}

	private void OwnerDeleted(EntityCommandBuffer commandBuffer, DynamicBuffer<InstalledUpgrade> installedUpgrades)
	{
		for (int i = 0; i < installedUpgrades.Length; i++)
		{
			Entity upgrade = installedUpgrades[i].m_Upgrade;
			if (!base.EntityManager.HasComponent<Object>(upgrade) && !base.EntityManager.HasComponent<Deleted>(upgrade))
			{
				commandBuffer.AddComponent(upgrade, default(Deleted));
			}
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
	public ServiceUpgradeSystem()
	{
	}
}
