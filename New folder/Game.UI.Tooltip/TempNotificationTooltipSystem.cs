using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TempNotificationTooltipSystem : TooltipSystemBase
{
	private struct ItemInfo : IComparable<ItemInfo>
	{
		public Entity m_Prefab;

		public IconPriority m_Priority;

		public int CompareTo(ItemInfo other)
		{
			return -m_Priority.CompareTo(other.m_Priority);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_TempQuery;

	private NativeParallelHashMap<Entity, IconPriority> m_Priorities;

	private NativeList<ItemInfo> m_Items;

	private List<StringTooltip> m_Tooltips;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Icon>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>());
		m_Priorities = new NativeParallelHashMap<Entity, IconPriority>(10, Allocator.Persistent);
		m_Items = new NativeList<ItemInfo>(10, Allocator.Persistent);
		m_Tooltips = new List<StringTooltip>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Priorities.Dispose();
		m_Items.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CompleteDependency();
		m_Priorities.Clear();
		m_Items.Clear();
		NativeArray<ArchetypeChunk> nativeArray = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Owner> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Icon> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			foreach (ArchetypeChunk item2 in nativeArray)
			{
				NativeArray<Temp> nativeArray2 = item2.GetNativeArray(ref typeHandle);
				NativeArray<Icon> nativeArray3 = item2.GetNativeArray(ref typeHandle3);
				NativeArray<Owner> nativeArray4 = item2.GetNativeArray(ref typeHandle2);
				NativeArray<PrefabRef> nativeArray5 = item2.GetNativeArray(ref typeHandle4);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Temp temp = nativeArray2[i];
					Icon icon = nativeArray3[i];
					PrefabRef prefabRef = nativeArray5[i];
					if (icon.m_ClusterLayer == IconClusterLayer.Marker)
					{
						continue;
					}
					if (nativeArray4.Length != 0)
					{
						Owner owner = nativeArray4[i];
						if (base.EntityManager.TryGetComponent<Temp>(owner.m_Owner, out var component) && HasIcon(component.m_Original, prefabRef.m_Prefab, icon.m_Priority))
						{
							continue;
						}
					}
					if ((temp.m_Flags & (TempFlags.Dragging | TempFlags.Select)) == TempFlags.Select)
					{
						continue;
					}
					if (m_Priorities.TryGetValue(prefabRef.m_Prefab, out var item))
					{
						if ((int)icon.m_Priority > (int)item)
						{
							m_Priorities[prefabRef.m_Prefab] = icon.m_Priority;
						}
					}
					else
					{
						m_Priorities.TryAdd(prefabRef.m_Prefab, icon.m_Priority);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		foreach (KeyValue<Entity, IconPriority> item3 in m_Priorities)
		{
			m_Items.Add(new ItemInfo
			{
				m_Prefab = item3.Key,
				m_Priority = item3.Value
			});
		}
		m_Items.Sort();
		for (int j = 0; j < m_Items.Length; j++)
		{
			ItemInfo itemInfo = m_Items[j];
			NotificationIconPrefab prefab = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(itemInfo.m_Prefab);
			if (m_Tooltips.Count <= j)
			{
				m_Tooltips.Add(new StringTooltip
				{
					path = $"notification{j}"
				});
			}
			StringTooltip stringTooltip = m_Tooltips[j];
			if (prefab.TryGet<UIObject>(out var component2) && !string.IsNullOrEmpty(component2.m_Icon))
			{
				stringTooltip.icon = component2.m_Icon;
			}
			else
			{
				stringTooltip.icon = null;
			}
			stringTooltip.value = LocalizedString.Id("Notifications.TITLE[" + prefab.name + "]");
			stringTooltip.color = NotificationTooltip.GetColor(itemInfo.m_Priority);
			AddMouseTooltip(stringTooltip);
		}
	}

	private bool HasIcon(Entity entity, Entity prefab, IconPriority minPriority)
	{
		if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<IconElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity icon = buffer[i].m_Icon;
				Icon componentData = base.EntityManager.GetComponentData<Icon>(icon);
				if (base.EntityManager.GetComponentData<PrefabRef>(icon).m_Prefab == prefab && (int)componentData.m_Priority >= (int)minPriority)
				{
					return true;
				}
			}
		}
		return false;
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
	public TempNotificationTooltipSystem()
	{
	}
}
