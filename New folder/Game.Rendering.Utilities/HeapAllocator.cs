#define UNITY_ASSERTIONS
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game.Rendering.Utilities;

public struct HeapAllocator : IDisposable
{
	[DebuggerDisplay("Size = {Size}, Alignment = {Alignment}")]
	private struct SizeBin : IComparable<SizeBin>, IEquatable<SizeBin>
	{
		public ulong sizeClass;

		public int blocksId;

		public ulong Size => sizeClass >> 6;

		public int AlignmentLog2 => (int)sizeClass & 0x3F;

		public uint Alignment => (uint)(1 << AlignmentLog2);

		public SizeBin(ulong size, uint alignment = 1u)
		{
			int y = math.tzcnt(alignment);
			y = math.min(63, y);
			sizeClass = (size << 6) | (uint)y;
			blocksId = -1;
		}

		public SizeBin(HeapBlock block)
		{
			int y = math.tzcnt(block.begin);
			y = math.min(63, y);
			sizeClass = (block.Length << 6) | (uint)y;
			blocksId = -1;
		}

		public int CompareTo(SizeBin other)
		{
			return sizeClass.CompareTo(other.sizeClass);
		}

		public bool Equals(SizeBin other)
		{
			return CompareTo(other) == 0;
		}

		public bool HasCompatibleAlignment(SizeBin requiredAlignment)
		{
			int alignmentLog = AlignmentLog2;
			int alignmentLog2 = requiredAlignment.AlignmentLog2;
			return alignmentLog >= alignmentLog2;
		}
	}

	private struct BlocksOfSize : IDisposable
	{
		private unsafe UnsafeList<HeapBlock>* m_Blocks;

		public unsafe bool Empty => m_Blocks->Length == 0;

		public unsafe int Length => m_Blocks->Length;

		public unsafe BlocksOfSize(int dummy)
		{
			m_Blocks = (UnsafeList<HeapBlock>*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafeList<HeapBlock>>(), UnsafeUtility.AlignOf<UnsafeList<HeapBlock>>(), Allocator.Persistent);
			UnsafeUtility.MemClear(m_Blocks, UnsafeUtility.SizeOf<UnsafeList<HeapBlock>>());
			m_Blocks->Allocator = Allocator.Persistent;
		}

		public unsafe void Push(HeapBlock block)
		{
			m_Blocks->Add(in block);
		}

		public unsafe HeapBlock Pop()
		{
			int length = m_Blocks->Length;
			if (length == 0)
			{
				return default(HeapBlock);
			}
			HeapBlock result = Block(length - 1);
			m_Blocks->Resize(length - 1);
			return result;
		}

		public unsafe bool Remove(HeapBlock block)
		{
			for (int i = 0; i < m_Blocks->Length; i++)
			{
				if (block.CompareTo(Block(i)) == 0)
				{
					m_Blocks->RemoveAtSwapBack(i);
					return true;
				}
			}
			return false;
		}

		public unsafe void Dispose()
		{
			m_Blocks->Dispose();
			UnsafeUtility.Free(m_Blocks, Allocator.Persistent);
		}

		public unsafe HeapBlock Block(int i)
		{
			return UnsafeUtility.ReadArrayElement<HeapBlock>(m_Blocks->Ptr, i);
		}
	}

	public const int MaxAlignmentLog2 = 63;

	public const int AlignmentBits = 6;

	private NativeList<SizeBin> m_SizeBins;

	private NativeList<BlocksOfSize> m_Blocks;

	private NativeList<int> m_BlocksFreelist;

	private NativeParallelHashMap<ulong, ulong> m_FreeEndpoints;

	private ulong m_Size;

	private ulong m_Free;

	private readonly int m_MinimumAlignmentLog2;

	private bool m_IsCreated;

	public uint MinimumAlignment => (uint)(1 << m_MinimumAlignmentLog2);

	public ulong FreeSpace => m_Free;

	public ulong UsedSpace => m_Size - m_Free;

	public ulong OnePastHighestUsedAddress
	{
		get
		{
			if (!m_FreeEndpoints.TryGetValue(m_Size, out var item))
			{
				return m_Size;
			}
			return item;
		}
	}

	public ulong Size => m_Size;

	public bool Empty => m_Free == m_Size;

	public bool Full => m_Free == 0;

	public bool IsCreated => m_IsCreated;

	public HeapAllocator(ulong size = 0uL, uint minimumAlignment = 1u)
	{
		m_SizeBins = new NativeList<SizeBin>(Allocator.Persistent);
		m_Blocks = new NativeList<BlocksOfSize>(Allocator.Persistent);
		m_BlocksFreelist = new NativeList<int>(Allocator.Persistent);
		m_FreeEndpoints = new NativeParallelHashMap<ulong, ulong>(0, Allocator.Persistent);
		m_Size = 0uL;
		m_Free = 0uL;
		m_MinimumAlignmentLog2 = math.tzcnt(minimumAlignment);
		m_IsCreated = true;
		Resize(size);
	}

	public void Clear()
	{
		ulong size = m_Size;
		m_SizeBins.Clear();
		m_Blocks.Clear();
		m_BlocksFreelist.Clear();
		m_FreeEndpoints.Clear();
		m_Size = 0uL;
		m_Free = 0uL;
		Resize(size);
	}

	public void Dispose()
	{
		if (IsCreated)
		{
			for (int i = 0; i < m_Blocks.Length; i++)
			{
				m_Blocks[i].Dispose();
			}
			m_FreeEndpoints.Dispose();
			m_Blocks.Dispose();
			m_BlocksFreelist.Dispose();
			m_SizeBins.Dispose();
			m_IsCreated = false;
		}
	}

	public bool Resize(ulong newSize)
	{
		if (newSize == m_Size)
		{
			return true;
		}
		if (newSize > m_Size)
		{
			ulong size = newSize - m_Size;
			HeapBlock block = HeapBlock.OfSize(m_Size, size);
			Release(block);
			m_Size = newSize;
			return true;
		}
		return false;
	}

	public HeapBlock Allocate(ulong size, uint alignment = 1u)
	{
		size = NextAligned(size, m_MinimumAlignmentLog2);
		alignment = math.max(alignment, MinimumAlignment);
		SizeBin sizeBin = new SizeBin(size, alignment);
		for (int i = FindSmallestSufficientBin(sizeBin); i < m_SizeBins.Length; i++)
		{
			SizeBin bin = m_SizeBins[i];
			if (CanFitAllocation(sizeBin, bin))
			{
				HeapBlock block = PopBlockFromBin(bin, i);
				return CutAllocationFromBlock(sizeBin, block);
			}
		}
		return default(HeapBlock);
	}

	public void Release(HeapBlock block)
	{
		block = Coalesce(block);
		SizeBin bin = new SizeBin(block);
		int num = FindSmallestSufficientBin(bin);
		if (num >= m_SizeBins.Length || bin.CompareTo(m_SizeBins[num]) != 0)
		{
			num = AddNewBin(ref bin, num);
		}
		m_Blocks[m_SizeBins[num].blocksId].Push(block);
		m_Free += block.Length;
		m_FreeEndpoints[block.begin] = block.end;
		m_FreeEndpoints[block.end] = block.begin;
	}

	public void DebugValidateInternalState()
	{
		int length = m_SizeBins.Length;
		int length2 = m_BlocksFreelist.Length;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < m_Blocks.Length; i++)
		{
			if (m_Blocks[i].Empty)
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		Assert.AreEqual(length, num2, "There should be exactly one non-empty block list per size bin");
		Assert.AreEqual(num, length2, "All empty block lists should be in the free list");
		for (int j = 0; j < m_BlocksFreelist.Length; j++)
		{
			int index = m_BlocksFreelist[j];
			Assert.IsTrue(m_Blocks[index].Empty, "There should be only empty block lists in the free list");
		}
		ulong num3 = 0uL;
		int num4 = 0;
		for (int k = 0; k < m_SizeBins.Length; k++)
		{
			SizeBin sizeBin = m_SizeBins[k];
			ulong size = sizeBin.Size;
			uint alignment = sizeBin.Alignment;
			BlocksOfSize blocksOfSize = m_Blocks[sizeBin.blocksId];
			Assert.IsFalse(blocksOfSize.Empty, "All block lists should be non-empty, empty lists should be removed");
			int length3 = blocksOfSize.Length;
			for (int l = 0; l < length3; l++)
			{
				HeapBlock block = blocksOfSize.Block(l);
				SizeBin sizeBin2 = new SizeBin(block);
				Assert.AreEqual(size, sizeBin2.Size, "Block size should match its bin");
				Assert.AreEqual(alignment, sizeBin2.Alignment, "Block alignment should match its bin");
				num3 += block.Length;
				if (m_FreeEndpoints.TryGetValue(block.begin, out var item))
				{
					Assert.AreEqual(block.end, item, "Free block end does not match stored endpoint");
				}
				else
				{
					Assert.IsTrue(condition: false, "No end endpoint found for free block");
				}
				if (m_FreeEndpoints.TryGetValue(block.end, out var item2))
				{
					Assert.AreEqual(block.begin, item2, "Free block begin does not match stored endpoint");
				}
				else
				{
					Assert.IsTrue(condition: false, "No begin endpoint found for free block");
				}
				num4++;
			}
		}
		Assert.AreEqual(num3, FreeSpace, "Free size reported incorrectly");
		Assert.IsTrue(num3 <= Size, "Amount of free size larger than maximum");
		Assert.AreEqual(2 * num4, m_FreeEndpoints.Count(), "Each free block should have exactly 2 stored endpoints");
	}

	private int FindSmallestSufficientBin(SizeBin needle)
	{
		if (m_SizeBins.Length == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = m_SizeBins.Length;
		int num4;
		while (true)
		{
			int num3 = (num2 - num) / 2;
			if (num3 == 0)
			{
				if (needle.CompareTo(m_SizeBins[num]) <= 0)
				{
					return num;
				}
				return num + 1;
			}
			num4 = num + num3;
			int num5 = needle.CompareTo(m_SizeBins[num4]);
			if (num5 < 0)
			{
				num2 = num4;
				continue;
			}
			if (num5 <= 0)
			{
				break;
			}
			num = num4;
		}
		return num4;
	}

	private unsafe int AddNewBin(ref SizeBin bin, int index)
	{
		if (m_BlocksFreelist.IsEmpty)
		{
			bin.blocksId = m_Blocks.Length;
			m_Blocks.Add(new BlocksOfSize(0));
		}
		else
		{
			int num = m_BlocksFreelist.Length - 1;
			bin.blocksId = m_BlocksFreelist[num];
			m_BlocksFreelist.ResizeUninitialized(num);
		}
		int num2 = m_SizeBins.Length - index;
		m_SizeBins.ResizeUninitialized(m_SizeBins.Length + 1);
		SizeBin* unsafePtr = m_SizeBins.GetUnsafePtr();
		UnsafeUtility.MemMove(unsafePtr + (index + 1), unsafePtr + index, num2 * UnsafeUtility.SizeOf<SizeBin>());
		unsafePtr[index] = bin;
		return index;
	}

	private unsafe void RemoveBinIfEmpty(SizeBin bin, int index)
	{
		if (m_Blocks[bin.blocksId].Empty)
		{
			int num = m_SizeBins.Length - (index + 1);
			SizeBin* unsafePtr = m_SizeBins.GetUnsafePtr();
			UnsafeUtility.MemMove(unsafePtr + index, unsafePtr + (index + 1), num * UnsafeUtility.SizeOf<SizeBin>());
			m_SizeBins.ResizeUninitialized(m_SizeBins.Length - 1);
			m_BlocksFreelist.Add(in bin.blocksId);
		}
	}

	private HeapBlock PopBlockFromBin(SizeBin bin, int index)
	{
		HeapBlock heapBlock = m_Blocks[bin.blocksId].Pop();
		RemoveEndpoints(heapBlock);
		m_Free -= heapBlock.Length;
		RemoveBinIfEmpty(bin, index);
		return heapBlock;
	}

	private void RemoveEndpoints(HeapBlock block)
	{
		m_FreeEndpoints.Remove(block.begin);
		m_FreeEndpoints.Remove(block.end);
	}

	private void RemoveFreeBlock(HeapBlock block)
	{
		RemoveEndpoints(block);
		SizeBin needle = new SizeBin(block);
		int index = FindSmallestSufficientBin(needle);
		m_Blocks[m_SizeBins[index].blocksId].Remove(block);
		RemoveBinIfEmpty(m_SizeBins[index], index);
		m_Free -= block.Length;
	}

	private HeapBlock Coalesce(HeapBlock block, ulong endpoint)
	{
		if (m_FreeEndpoints.TryGetValue(endpoint, out var item))
		{
			if (endpoint == block.begin)
			{
				HeapBlock block2 = new HeapBlock(item, block.begin);
				RemoveFreeBlock(block2);
				return new HeapBlock(block2.begin, block.end);
			}
			HeapBlock block3 = new HeapBlock(block.end, item);
			RemoveFreeBlock(block3);
			return new HeapBlock(block.begin, block3.end);
		}
		return block;
	}

	private HeapBlock Coalesce(HeapBlock block)
	{
		block = Coalesce(block, block.begin);
		block = Coalesce(block, block.end);
		return block;
	}

	private bool CanFitAllocation(SizeBin allocation, SizeBin bin)
	{
		if (m_Blocks[bin.blocksId].Empty)
		{
			return false;
		}
		if (bin.HasCompatibleAlignment(allocation))
		{
			return true;
		}
		return bin.Size >= allocation.Size + allocation.Alignment;
	}

	private static ulong NextAligned(ulong offset, int alignmentLog2)
	{
		int num = (1 << alignmentLog2) - 1;
		return (ulong)((long)offset + (long)num >>> alignmentLog2 << alignmentLog2);
	}

	private HeapBlock CutAllocationFromBlock(SizeBin allocation, HeapBlock block)
	{
		if (allocation.Size == block.Length)
		{
			return block;
		}
		ulong num = NextAligned(block.begin, allocation.AlignmentLog2);
		ulong num2 = num + allocation.Size;
		if (num > block.begin)
		{
			Release(new HeapBlock(block.begin, num));
		}
		if (num2 < block.end)
		{
			Release(new HeapBlock(num2, block.end));
		}
		return new HeapBlock(num, num2);
	}
}
