using System;
using Colossal.Entities;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI;

public readonly struct UIObjectInfo : IComparable<UIObjectInfo>
{
	public Entity entity { get; }

	public PrefabData prefabData { get; }

	public int priority { get; }

	public UIObjectInfo(Entity entity, int priority)
	{
		this.entity = entity;
		prefabData = default(PrefabData);
		this.priority = priority;
	}

	public UIObjectInfo(Entity entity, PrefabData prefabData, int priority)
	{
		this.entity = entity;
		this.prefabData = prefabData;
		this.priority = priority;
	}

	public int CompareTo(UIObjectInfo other)
	{
		return priority.CompareTo(other.priority);
	}

	public static NativeList<UIObjectInfo> GetSortedObjects(EntityQuery query, Allocator allocator)
	{
		using NativeArray<Entity> nativeArray = query.ToEntityArray(Allocator.Temp);
		using NativeArray<PrefabData> nativeArray2 = query.ToComponentDataArray<PrefabData>(Allocator.Temp);
		using NativeArray<UIObjectData> nativeArray3 = query.ToComponentDataArray<UIObjectData>(Allocator.Temp);
		int length = nativeArray.Length;
		NativeList<UIObjectInfo> nativeList = new NativeList<UIObjectInfo>(length, allocator);
		for (int i = 0; i < length; i++)
		{
			nativeList.Add(new UIObjectInfo(nativeArray[i], nativeArray2[i], nativeArray3[i].m_Priority));
		}
		nativeList.Sort();
		return nativeList;
	}

	public static NativeList<UIObjectInfo> GetSortedObjects(EntityManager entityManager, EntityQuery query, Allocator allocator)
	{
		using NativeArray<Entity> nativeArray = query.ToEntityArray(Allocator.Temp);
		using NativeArray<PrefabData> nativeArray2 = query.ToComponentDataArray<PrefabData>(Allocator.Temp);
		int length = nativeArray.Length;
		NativeList<UIObjectInfo> nativeList = new NativeList<UIObjectInfo>(length, allocator);
		for (int i = 0; i < length; i++)
		{
			UIObjectData component;
			int num = (entityManager.TryGetComponent<UIObjectData>(nativeArray[i], out component) ? component.m_Priority : 0);
			nativeList.Add(new UIObjectInfo(nativeArray[i], nativeArray2[i], num));
		}
		nativeList.Sort();
		return nativeList;
	}

	public static NativeList<UIObjectInfo> GetSortedObjects(EntityManager entityManager, NativeList<Entity> entities, Allocator allocator)
	{
		int length = entities.Length;
		NativeList<UIObjectInfo> nativeList = new NativeList<UIObjectInfo>(length, allocator);
		for (int i = 0; i < length; i++)
		{
			Entity entity = entities[i];
			UIObjectData component;
			int num = (entityManager.TryGetComponent<UIObjectData>(entity, out component) ? component.m_Priority : 0);
			nativeList.Add(new UIObjectInfo(entity, entityManager.GetComponentData<PrefabData>(entity), num));
		}
		nativeList.Sort();
		return nativeList;
	}

	public static NativeList<UIObjectInfo> GetObjects(EntityManager entityManager, DynamicBuffer<UIGroupElement> elements, Allocator allocator)
	{
		NativeList<UIObjectInfo> result = new NativeList<UIObjectInfo>(elements.Length, allocator);
		for (int i = 0; i < elements.Length; i++)
		{
			Entity prefab = elements[i].m_Prefab;
			UIObjectData component;
			int num = (entityManager.TryGetComponent<UIObjectData>(prefab, out component) ? component.m_Priority : 0);
			result.Add(new UIObjectInfo(prefab, entityManager.GetComponentData<PrefabData>(prefab), num));
		}
		return result;
	}

	public static NativeList<UIObjectInfo> GetSortedObjects(EntityManager entityManager, DynamicBuffer<UIGroupElement> elements, Allocator allocator)
	{
		NativeList<UIObjectInfo> nativeList = new NativeList<UIObjectInfo>(elements.Length, allocator);
		for (int i = 0; i < elements.Length; i++)
		{
			Entity prefab = elements[i].m_Prefab;
			UIObjectData component;
			int num = (entityManager.TryGetComponent<UIObjectData>(prefab, out component) ? component.m_Priority : 0);
			nativeList.Add(new UIObjectInfo(prefab, entityManager.GetComponentData<PrefabData>(prefab), num));
		}
		nativeList.Sort();
		return nativeList;
	}
}
