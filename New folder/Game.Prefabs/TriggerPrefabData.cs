using System;
using Game.Triggers;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

public struct TriggerPrefabData : IDisposable
{
	public struct PrefabKey : IEquatable<PrefabKey>
	{
		public TriggerType m_TriggerType;

		public Entity m_TriggerEntity;

		public bool Equals(PrefabKey other)
		{
			if (m_TriggerType == other.m_TriggerType)
			{
				return m_TriggerEntity == other.m_TriggerEntity;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)m_TriggerType * 31 + m_TriggerEntity.GetHashCode();
		}
	}

	public struct PrefabValue
	{
		public TargetType m_TargetTypes;

		public Entity m_Prefab;
	}

	public struct Iterator
	{
		public NativeParallelMultiHashMapIterator<PrefabKey> m_Iterator;
	}

	private NativeParallelMultiHashMap<PrefabKey, PrefabValue> m_PrefabMap;

	public TriggerPrefabData(Allocator allocator)
	{
		m_PrefabMap = new NativeParallelMultiHashMap<PrefabKey, PrefabValue>(100, allocator);
	}

	public void Dispose()
	{
		m_PrefabMap.Dispose();
	}

	public void AddPrefab(Entity prefab, TriggerData triggerData)
	{
		PrefabKey key = new PrefabKey
		{
			m_TriggerType = triggerData.m_TriggerType,
			m_TriggerEntity = triggerData.m_TriggerPrefab
		};
		PrefabValue item = new PrefabValue
		{
			m_TargetTypes = triggerData.m_TargetTypes,
			m_Prefab = prefab
		};
		m_PrefabMap.Add(key, item);
	}

	public void RemovePrefab(Entity prefab, TriggerData triggerData)
	{
		PrefabKey key = new PrefabKey
		{
			m_TriggerType = triggerData.m_TriggerType,
			m_TriggerEntity = triggerData.m_TriggerPrefab
		};
		if (!m_PrefabMap.TryGetFirstValue(key, out var item, out var it))
		{
			return;
		}
		do
		{
			if (item.m_TargetTypes == triggerData.m_TargetTypes && item.m_Prefab == prefab)
			{
				m_PrefabMap.Remove(it);
				break;
			}
		}
		while (m_PrefabMap.TryGetNextValue(out item, ref it));
	}

	public bool HasAnyPrefabs(TriggerType triggerType, Entity triggerPrefab)
	{
		PrefabKey key = new PrefabKey
		{
			m_TriggerType = triggerType,
			m_TriggerEntity = triggerPrefab
		};
		PrefabValue item;
		NativeParallelMultiHashMapIterator<PrefabKey> it;
		return m_PrefabMap.TryGetFirstValue(key, out item, out it);
	}

	public bool TryGetFirstPrefab(TriggerType triggerType, TargetType targetType, Entity triggerPrefab, out Entity prefab, out Iterator iterator)
	{
		PrefabKey key = new PrefabKey
		{
			m_TriggerType = triggerType,
			m_TriggerEntity = triggerPrefab
		};
		if (m_PrefabMap.TryGetFirstValue(key, out var item, out iterator.m_Iterator))
		{
			do
			{
				if (targetType == TargetType.Nothing || (item.m_TargetTypes & targetType) != TargetType.Nothing)
				{
					prefab = item.m_Prefab;
					return true;
				}
			}
			while (m_PrefabMap.TryGetNextValue(out item, ref iterator.m_Iterator));
		}
		prefab = Entity.Null;
		return false;
	}

	public bool TryGetNextPrefab(TriggerType triggerType, TargetType targetType, Entity triggerPrefab, out Entity prefab, ref Iterator iterator)
	{
		PrefabKey prefabKey = new PrefabKey
		{
			m_TriggerType = triggerType,
			m_TriggerEntity = triggerPrefab
		};
		PrefabValue item;
		while (m_PrefabMap.TryGetNextValue(out item, ref iterator.m_Iterator))
		{
			if (targetType == TargetType.Nothing || (item.m_TargetTypes & targetType) != TargetType.Nothing)
			{
				prefab = item.m_Prefab;
				return true;
			}
		}
		prefab = Entity.Null;
		return false;
	}
}
