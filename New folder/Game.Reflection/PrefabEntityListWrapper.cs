using System;
using System.Collections;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace Game.Reflection;

public class PrefabEntityListWrapper<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection where T : PrefabBase
{
	public class PrefabEntityListWrapperEnumerator : IEnumerator<T>, IEnumerator, IDisposable
	{
		private int m_Index = -1;

		private PrefabSystem m_PrefabSystem;

		public NativeList<Entity> m_Entities;

		public T Current => PrefabEntityListWrapper<T>.ToPrefab(m_Entities[m_Index], m_PrefabSystem);

		object IEnumerator.Current => Current;

		public PrefabEntityListWrapperEnumerator(NativeList<Entity> entities, PrefabSystem prefabSystem)
		{
			m_Entities = entities;
			m_PrefabSystem = prefabSystem;
		}

		public bool MoveNext()
		{
			m_Index++;
			return m_Index < m_Entities.Length;
		}

		public void Reset()
		{
			m_Index = -1;
		}

		public void Dispose()
		{
		}
	}

	public NativeList<Entity> m_Entities;

	public PrefabSystem m_PrefabSystem;

	public int Count => m_Entities.Length;

	public bool IsSynchronized => true;

	public object SyncRoot { get; } = new object();

	public bool IsReadOnly => false;

	object IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			this[index] = (T)value;
		}
	}

	public bool IsFixedSize => false;

	public T this[int index]
	{
		get
		{
			return ToPrefab(m_Entities[index], m_PrefabSystem);
		}
		set
		{
			m_Entities[index] = ToEntity(value, m_PrefabSystem);
		}
	}

	public PrefabEntityListWrapper(NativeList<Entity> underlyingList, PrefabSystem prefabSystem)
	{
		m_Entities = underlyingList;
		m_PrefabSystem = prefabSystem;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new PrefabEntityListWrapperEnumerator(m_Entities, m_PrefabSystem);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Add(T item)
	{
		m_Entities.Add(ToEntity(item, m_PrefabSystem));
	}

	public int Add(object value)
	{
		Add((T)value);
		return m_Entities.Length - 1;
	}

	public void Clear()
	{
		m_Entities.Clear();
	}

	public bool Contains(object value)
	{
		return Contains((T)value);
	}

	public int IndexOf(object value)
	{
		return IndexOf((T)value);
	}

	public void Insert(int index, object value)
	{
		Insert(index, (T)value);
	}

	public void Remove(object value)
	{
		Remove((T)value);
	}

	public bool Contains(T item)
	{
		return m_Entities.Contains(ToEntity(item, m_PrefabSystem));
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		for (int i = arrayIndex; i < m_Entities.Length; i++)
		{
			array[arrayIndex] = ToPrefab(m_Entities[arrayIndex], m_PrefabSystem);
		}
	}

	public bool Remove(T item)
	{
		Entity value = ToEntity(item, m_PrefabSystem);
		int num = m_Entities.IndexOf(value);
		if (num >= 0 && num < m_Entities.Length)
		{
			m_Entities.RemoveAt(num);
			return true;
		}
		return false;
	}

	public void CopyTo(Array array, int index)
	{
		CopyTo((T[])array, index);
	}

	public int IndexOf(T item)
	{
		Entity value = ToEntity(item, m_PrefabSystem);
		return m_Entities.IndexOf(value);
	}

	public void Insert(int index, T item)
	{
		Entity value = ToEntity(item, m_PrefabSystem);
		m_Entities.InsertRange(index, 1);
		m_Entities[index] = value;
	}

	public void RemoveAt(int index)
	{
		m_Entities.RemoveAt(index);
	}

	private static Entity ToEntity(T prefab, PrefabSystem prefabSystem)
	{
		if (!(prefab != null))
		{
			return Entity.Null;
		}
		return prefabSystem.GetEntity(prefab);
	}

	private static T ToPrefab(Entity entity, PrefabSystem prefabSystem)
	{
		if (!(entity != Entity.Null))
		{
			return null;
		}
		return prefabSystem.GetPrefab<T>(entity);
	}
}
