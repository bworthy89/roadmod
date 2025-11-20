using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Common;

public struct GroupBuilder<T> where T : unmanaged, IEquatable<T>
{
	public struct Result : IComparable<Result>
	{
		public T m_Item;

		public int m_Group;

		public Result(T item, int group)
		{
			m_Item = item;
			m_Group = group;
		}

		public int CompareTo(Result other)
		{
			return m_Group - other.m_Group;
		}
	}

	public struct Iterator
	{
		public int m_StartIndex;

		public int m_GroupIndex;

		public Iterator(int groupIndex)
		{
			m_StartIndex = 0;
			m_GroupIndex = groupIndex;
		}
	}

	private NativeList<int> m_Groups;

	private NativeParallelHashMap<T, int> m_GroupIndex;

	private NativeList<Result> m_Results;

	public GroupBuilder(Allocator allocator)
	{
		m_Groups = new NativeList<int>(32, allocator);
		m_GroupIndex = new NativeParallelHashMap<T, int>(32, allocator);
		m_Results = new NativeList<Result>(32, allocator);
	}

	public void Dispose()
	{
		m_Groups.Dispose();
		m_GroupIndex.Dispose();
		m_Results.Dispose();
	}

	public void AddSingle(T item)
	{
		if (!m_GroupIndex.TryGetValue(item, out var item2))
		{
			item2 = CreateGroup();
			AddToGroup(item, item2);
		}
	}

	public void AddPair(T item1, T item2)
	{
		int item4;
		if (m_GroupIndex.TryGetValue(item1, out var item3))
		{
			item3 = m_Groups[item3];
			if (m_GroupIndex.TryGetValue(item2, out item4))
			{
				item4 = m_Groups[item4];
				if (item3 != item4)
				{
					MergeGroups(item3, item4);
				}
			}
			else
			{
				AddToGroup(item2, item3);
			}
		}
		else if (m_GroupIndex.TryGetValue(item2, out item4))
		{
			item4 = m_Groups[item4];
			AddToGroup(item1, item4);
		}
		else
		{
			int index = CreateGroup();
			AddToGroup(item1, index);
			AddToGroup(item2, index);
		}
	}

	public bool TryGetFirstGroup(out NativeArray<Result> group, out Iterator iterator)
	{
		NativeArray<int> nativeArray = m_Groups.AsArray();
		NativeArray<Result> array = m_Results.AsArray();
		if (array.Length == 0)
		{
			group = default(NativeArray<Result>);
			iterator = default(Iterator);
			return false;
		}
		for (int i = 0; i < nativeArray.Length; i++)
		{
			nativeArray[i] = nativeArray[nativeArray[i]];
		}
		for (int j = 0; j < array.Length; j++)
		{
			Result value = array[j];
			value.m_Group = nativeArray[value.m_Group];
			array[j] = value;
		}
		array.Sort();
		iterator = new Iterator(array[0].m_Group);
		return TryGetNextGroup(out group, ref iterator);
	}

	public bool TryGetNextGroup(out NativeArray<Result> group, ref Iterator iterator)
	{
		NativeArray<Result> nativeArray = m_Results.AsArray();
		for (int i = iterator.m_StartIndex + 1; i < nativeArray.Length; i++)
		{
			Result result = nativeArray[i];
			if (result.m_Group != iterator.m_GroupIndex)
			{
				group = nativeArray.GetSubArray(iterator.m_StartIndex, i - iterator.m_StartIndex);
				iterator.m_StartIndex = i;
				iterator.m_GroupIndex = result.m_Group;
				return true;
			}
		}
		if (nativeArray.Length > iterator.m_StartIndex)
		{
			group = nativeArray.GetSubArray(iterator.m_StartIndex, nativeArray.Length - iterator.m_StartIndex);
			iterator.m_StartIndex = nativeArray.Length;
			return true;
		}
		group = default(NativeArray<Result>);
		return false;
	}

	private int CreateGroup()
	{
		int value = m_Groups.Length;
		m_Groups.Add(in value);
		return value;
	}

	private int MergeGroups(int index1, int index2)
	{
		int num = math.min(index1, index2);
		m_Groups[math.max(index1, index2)] = num;
		return num;
	}

	private void AddToGroup(T item, int index)
	{
		m_GroupIndex.TryAdd(item, index);
		m_Results.Add(new Result(item, index));
	}
}
