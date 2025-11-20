using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Simulation.Flow;

public struct Layer : IDisposable
{
	public UnsafeList<CutElement> m_Elements;

	public UnsafeList<CutElementRef> m_ElementRefs;

	private int m_UsedElementCount;

	private int m_UsedElementRefCount;

	private int m_FreeElementIndex;

	private int m_FreeElementRefIndex;

	public bool isEmpty => m_UsedElementCount == 0;

	public int usedElementCount => m_UsedElementCount;

	public int usedElementRefCount => m_UsedElementRefCount;

	public Layer(int initialLength, Allocator allocator)
	{
		m_Elements = new UnsafeList<CutElement>(initialLength, allocator, NativeArrayOptions.ClearMemory);
		m_Elements.Length = initialLength;
		m_ElementRefs = new UnsafeList<CutElementRef>(initialLength, allocator, NativeArrayOptions.ClearMemory);
		m_ElementRefs.Length = initialLength;
		m_UsedElementCount = 0;
		m_UsedElementRefCount = 0;
		if (initialLength > 0)
		{
			m_FreeElementIndex = 0;
			m_FreeElementRefIndex = 0;
			int num = initialLength - 1;
			for (int i = 0; i < initialLength; i++)
			{
				int nextIndex = ((i == num) ? (-1) : (i + 1));
				CutElement value = m_Elements[i];
				value.m_NextIndex = nextIndex;
				m_Elements[i] = value;
				CutElementRef value2 = m_ElementRefs[i];
				value2.m_NextIndex = nextIndex;
				m_ElementRefs[i] = value2;
			}
		}
		else
		{
			m_FreeElementIndex = -1;
			m_FreeElementRefIndex = -1;
		}
	}

	public bool ContainsCutElement(Identifier id)
	{
		if (id.m_Index != -1)
		{
			CutElement cutElement = m_Elements[id.m_Index];
			if (cutElement.isCreated && cutElement.m_Version == id.m_Version)
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsCutElementForConnection(Identifier id, Connection connection, bool admissible)
	{
		if (id.m_Index != -1 && id.m_Index < m_Elements.Length)
		{
			CutElement cutElement = m_Elements[id.m_Index];
			if (cutElement.isCreated && cutElement.m_Version == id.m_Version && cutElement.m_Edge == connection.m_Edge && cutElement.m_StartNode == connection.m_StartNode && cutElement.m_EndNode == connection.m_EndNode && cutElement.isAdmissible == admissible)
			{
				return true;
			}
		}
		return false;
	}

	public ref CutElement GetCutElement(int index)
	{
		return ref m_Elements.ElementAt(index);
	}

	public int AddCutElement(in CutElement element)
	{
		int num = m_FreeElementIndex;
		if (num != -1)
		{
			m_FreeElementIndex = m_Elements[num].m_NextIndex;
		}
		else
		{
			num = m_Elements.Length;
			m_Elements.Resize(num + 1, NativeArrayOptions.ClearMemory);
		}
		m_Elements[num] = element;
		m_UsedElementCount++;
		return num;
	}

	public void FreeCutElement(int index)
	{
		ref CutElement reference = ref m_Elements.ElementAt(index);
		reference.m_Flags = CutElementFlags.None;
		reference.m_NextIndex = m_FreeElementIndex;
		m_FreeElementIndex = index;
		m_UsedElementCount--;
	}

	public ref CutElementRef GetCutElementRef(int index)
	{
		return ref m_ElementRefs.ElementAt(index);
	}

	public int AddCutElementRef(in CutElementRef elementRef)
	{
		int num = m_FreeElementRefIndex;
		if (num != -1)
		{
			m_FreeElementRefIndex = m_ElementRefs[num].m_NextIndex;
		}
		else
		{
			num = m_ElementRefs.Length;
			m_ElementRefs.Resize(num + 1, NativeArrayOptions.ClearMemory);
		}
		m_ElementRefs[num] = elementRef;
		m_UsedElementRefCount++;
		return num;
	}

	public void FreeCutElementRef(int index)
	{
		ref CutElementRef reference = ref m_ElementRefs.ElementAt(index);
		reference.m_Layer = -1;
		reference.m_Index = -1;
		reference.m_NextIndex = m_FreeElementRefIndex;
		m_FreeElementRefIndex = index;
		m_UsedElementRefCount--;
	}

	public void MergeGroups(int elementId1, int elementId2)
	{
		ref CutElement cutElement = ref GetCutElement(elementId1);
		CutElement cutElement2 = GetCutElement(elementId2);
		int num = cutElement.m_Group;
		int num2 = cutElement2.m_Group;
		if (num == num2)
		{
			return;
		}
		int nextIndex = cutElement.m_NextIndex;
		cutElement.m_NextIndex = num2;
		int num3 = num2;
		do
		{
			ref CutElement cutElement3 = ref GetCutElement(num3);
			cutElement3.m_Group = num;
			num3 = cutElement3.m_NextIndex;
			if (num3 == -1)
			{
				cutElement3.m_NextIndex = nextIndex;
			}
		}
		while (num3 != -1);
	}

	public void RemoveElementLink(int elementIndex, int upperLayerIndex, int upperLayerElementIndex)
	{
		ref CutElement cutElement = ref GetCutElement(elementIndex);
		int num = -1;
		int num2 = cutElement.m_LinkedElements;
		while (num2 != -1)
		{
			CutElementRef cutElementRef = GetCutElementRef(num2);
			if (cutElementRef.m_Layer == upperLayerIndex && cutElementRef.m_Index == upperLayerElementIndex)
			{
				break;
			}
			num = num2;
			num2 = cutElementRef.m_NextIndex;
		}
		ref CutElementRef cutElementRef2 = ref GetCutElementRef(num2);
		if (num == -1)
		{
			cutElement.m_LinkedElements = cutElementRef2.m_NextIndex;
		}
		else
		{
			GetCutElementRef(num).m_NextIndex = cutElementRef2.m_NextIndex;
		}
		FreeCutElementRef(num2);
	}

	public static Layer Load(LayerState state, NativeArray<CutElement> layerElements, ref int elementIndex, NativeArray<CutElementRef> layerElementRefs, ref int elementRefIndex)
	{
		UnsafeList<CutElement> unsafeList = new UnsafeList<CutElement>(state.m_ElementsLength, Allocator.Temp);
		AddRange(ref unsafeList, layerElements.Slice(elementIndex, state.m_ElementsLength));
		UnsafeList<CutElementRef> unsafeList2 = new UnsafeList<CutElementRef>(state.m_ElementRefsLength, Allocator.Temp);
		AddRange(ref unsafeList2, layerElementRefs.Slice(elementRefIndex, state.m_ElementRefsLength));
		elementIndex += state.m_ElementsLength;
		elementRefIndex += state.m_ElementRefsLength;
		return new Layer
		{
			m_Elements = unsafeList,
			m_ElementRefs = unsafeList2,
			m_UsedElementCount = state.m_UsedElementCount,
			m_UsedElementRefCount = state.m_UsedElementRefCount,
			m_FreeElementIndex = state.m_FreeElementIndex,
			m_FreeElementRefIndex = state.m_FreeElementRefIndex
		};
	}

	private unsafe static void AddRange<T>(ref UnsafeList<T> unsafeList, NativeSlice<T> slice) where T : unmanaged
	{
		unsafeList.AddRange(slice.GetUnsafeReadOnlyPtr(), slice.Length);
	}

	public void Save(NativeList<LayerState> layerStates, NativeList<CutElement> layerElements, NativeList<CutElementRef> layerElementRefs)
	{
		LayerState value = new LayerState
		{
			m_ElementsLength = m_Elements.Length,
			m_ElementRefsLength = m_ElementRefs.Length,
			m_UsedElementCount = m_UsedElementCount,
			m_UsedElementRefCount = m_UsedElementRefCount,
			m_FreeElementIndex = m_FreeElementIndex,
			m_FreeElementRefIndex = m_FreeElementRefIndex
		};
		layerStates.Add(in value);
		AddRange(layerElements, m_Elements);
		AddRange(layerElementRefs, m_ElementRefs);
	}

	private unsafe static void AddRange<T>(NativeList<T> list, UnsafeList<T> unsafeList) where T : unmanaged
	{
		list.AddRange(unsafeList.Ptr, unsafeList.Length);
	}

	public void Dispose()
	{
		m_Elements.Dispose();
		m_ElementRefs.Dispose();
	}
}
