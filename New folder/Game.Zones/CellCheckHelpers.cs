using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Zones;

public static class CellCheckHelpers
{
	public struct SortedEntity : IComparable<SortedEntity>
	{
		public Entity m_Entity;

		public int CompareTo(SortedEntity other)
		{
			return m_Entity.Index - other.m_Entity.Index;
		}
	}

	public struct BlockOverlap : IComparable<BlockOverlap>
	{
		public int m_Group;

		public uint m_Priority;

		public Entity m_Block;

		public Entity m_Other;

		public Entity m_Left;

		public Entity m_Right;

		public int CompareTo(BlockOverlap other)
		{
			int num = m_Group - other.m_Group;
			int num2 = math.select(math.select(0, 1, m_Priority > other.m_Priority), -1, m_Priority < other.m_Priority);
			return math.select(math.select(m_Block.Index - other.m_Block.Index, num2, num2 != 0), num, num != 0);
		}
	}

	public struct OverlapGroup
	{
		public int m_StartIndex;

		public int m_EndIndex;
	}

	[BurstCompile]
	public struct FindUpdatedBlocksSingleIterationJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (MathUtils.Intersect(bounds, m_Bounds))
				{
					m_ResultQueue.Enqueue(blockEntity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = m_Bounds[index],
				m_ResultQueue = m_ResultQueue
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	public struct FindUpdatedBlocksDoubleIterationJob : IJobParallelForDefer
	{
		private struct FirstIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public Bounds2 m_ResultBounds;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (MathUtils.Intersect(bounds, m_Bounds))
				{
					m_ResultBounds |= bounds;
				}
			}
		}

		private struct SecondIterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Bounds2 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity)
			{
				if (MathUtils.Intersect(bounds, m_Bounds))
				{
					m_ResultQueue.Enqueue(blockEntity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			FirstIterator iterator = new FirstIterator
			{
				m_Bounds = m_Bounds[index],
				m_ResultBounds = new Bounds2(float.MaxValue, float.MinValue)
			};
			m_SearchTree.Iterate(ref iterator);
			SecondIterator iterator2 = new SecondIterator
			{
				m_Bounds = iterator.m_ResultBounds,
				m_ResultQueue = m_ResultQueue
			};
			m_SearchTree.Iterate(ref iterator2);
		}
	}

	[BurstCompile]
	public struct CollectBlocksJob : IJob
	{
		public NativeQueue<Entity> m_Queue1;

		public NativeQueue<Entity> m_Queue2;

		public NativeQueue<Entity> m_Queue3;

		public NativeQueue<Entity> m_Queue4;

		public NativeList<SortedEntity> m_ResultList;

		public void Execute()
		{
			ProcessQueue(m_Queue1);
			ProcessQueue(m_Queue2);
			ProcessQueue(m_Queue3);
			ProcessQueue(m_Queue4);
			RemoveDuplicates();
		}

		private void ProcessQueue(NativeQueue<Entity> queue)
		{
			Entity item;
			while (queue.TryDequeue(out item))
			{
				m_ResultList.Add(new SortedEntity
				{
					m_Entity = item
				});
			}
		}

		private void RemoveDuplicates()
		{
			m_ResultList.Sort();
			int i = 0;
			int num = 0;
			while (i < m_ResultList.Length)
			{
				SortedEntity value;
				for (value = m_ResultList[i++]; i < m_ResultList.Length && m_ResultList[i].m_Entity.Equals(value.m_Entity); i++)
				{
				}
				m_ResultList[num++] = value;
			}
			if (num < m_ResultList.Length)
			{
				m_ResultList.RemoveRange(num, m_ResultList.Length - num);
			}
		}
	}

	[BurstCompile]
	public struct FindOverlappingBlocksJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Entity m_BlockEntity;

			public Block m_BlockData;

			public ValidArea m_ValidAreaData;

			public BuildOrder m_BuildOrderData;

			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public int m_OverlapCount;

			public ComponentLookup<Block> m_BlockDataFromEntity;

			public ComponentLookup<ValidArea> m_ValidAreaDataEntity;

			public ComponentLookup<BuildOrder> m_BuildOrderDataEntity;

			public NativeQueue<BlockOverlap>.ParallelWriter m_ResultQueue;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity blockEntity2)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds) || m_BlockEntity.Equals(blockEntity2))
				{
					return;
				}
				Block block = m_BlockDataFromEntity[blockEntity2];
				ValidArea validArea = m_ValidAreaDataEntity[blockEntity2];
				BuildOrder buildOrder = m_BuildOrderDataEntity[blockEntity2];
				if (validArea.m_Area.y <= validArea.m_Area.x)
				{
					return;
				}
				Quad2 quad = MathUtils.Expand(ZoneUtils.CalculateCorners(block, validArea), -0.01f);
				if (!MathUtils.Intersect(m_Quad, quad))
				{
					if (!ZoneUtils.IsNeighbor(m_BlockData, block, m_BuildOrderData, buildOrder))
					{
						return;
					}
					if (math.dot(block.m_Position.xz - m_BlockData.m_Position.xz, MathUtils.Right(m_BlockData.m_Direction)) > 0f)
					{
						if ((m_ValidAreaData.m_Area.x != 0) | (validArea.m_Area.y != block.m_Size.x))
						{
							return;
						}
					}
					else if ((m_ValidAreaData.m_Area.y != m_BlockData.m_Size.x) | (validArea.m_Area.x != 0))
					{
						return;
					}
				}
				BlockOverlap value = new BlockOverlap
				{
					m_Priority = m_BuildOrderData.m_Order,
					m_Block = m_BlockEntity,
					m_Other = blockEntity2
				};
				m_ResultQueue.Enqueue(value);
				m_OverlapCount++;
			}
		}

		[ReadOnly]
		public NativeArray<SortedEntity> m_Blocks;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public ComponentLookup<BuildOrder> m_BuildOrderData;

		public NativeQueue<BlockOverlap>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Entity entity = m_Blocks[index].m_Entity;
			Block block = m_BlockData[entity];
			ValidArea validArea = m_ValidAreaData[entity];
			BuildOrder buildOrderData = m_BuildOrderData[entity];
			if (validArea.m_Area.y > validArea.m_Area.x)
			{
				Iterator iterator = new Iterator
				{
					m_BlockEntity = entity,
					m_BlockData = block,
					m_ValidAreaData = validArea,
					m_BuildOrderData = buildOrderData,
					m_Bounds = MathUtils.Expand(ZoneUtils.CalculateBounds(block), 1.6f),
					m_Quad = MathUtils.Expand(ZoneUtils.CalculateCorners(block, validArea), -0.01f),
					m_BlockDataFromEntity = m_BlockData,
					m_ValidAreaDataEntity = m_ValidAreaData,
					m_BuildOrderDataEntity = m_BuildOrderData,
					m_ResultQueue = m_ResultQueue
				};
				m_SearchTree.Iterate(ref iterator);
				if (iterator.m_OverlapCount == 0)
				{
					BlockOverlap value = new BlockOverlap
					{
						m_Priority = buildOrderData.m_Order,
						m_Block = entity
					};
					m_ResultQueue.Enqueue(value);
				}
			}
		}
	}

	[BurstCompile]
	public struct GroupOverlappingBlocksJob : IJob
	{
		[ReadOnly]
		public NativeArray<SortedEntity> m_Blocks;

		public NativeQueue<BlockOverlap> m_OverlapQueue;

		public NativeList<BlockOverlap> m_BlockOverlaps;

		public NativeList<OverlapGroup> m_OverlapGroups;

		public void Execute()
		{
			NativeParallelHashMap<Entity, int> nativeParallelHashMap = new NativeParallelHashMap<Entity, int>(m_Blocks.Length, Allocator.Temp);
			NativeList<int> groups = new NativeList<int>(Allocator.Temp);
			BlockOverlap item;
			while (m_OverlapQueue.TryDequeue(out item))
			{
				if (nativeParallelHashMap.TryGetValue(item.m_Block, out var item2))
				{
					item2 = groups[item2];
					if (item.m_Other != Entity.Null)
					{
						if (nativeParallelHashMap.TryGetValue(item.m_Other, out var item3))
						{
							item3 = groups[item3];
							if (item2 != item3)
							{
								item2 = MergeGroups(groups, item2, item3);
							}
						}
						else
						{
							nativeParallelHashMap.TryAdd(item.m_Other, item2);
						}
					}
				}
				else if (item.m_Other != Entity.Null)
				{
					if (nativeParallelHashMap.TryGetValue(item.m_Other, out item2))
					{
						item2 = groups[item2];
						nativeParallelHashMap.TryAdd(item.m_Block, item2);
					}
					else
					{
						item2 = CreateGroup(groups);
						nativeParallelHashMap.TryAdd(item.m_Block, item2);
						nativeParallelHashMap.TryAdd(item.m_Other, item2);
					}
				}
				else
				{
					item2 = CreateGroup(groups);
					nativeParallelHashMap.TryAdd(item.m_Block, item2);
				}
				item.m_Group = item2;
				m_BlockOverlaps.Add(in item);
			}
			if (m_BlockOverlaps.Length != 0)
			{
				for (int i = 0; i < groups.Length; i++)
				{
					groups[i] = groups[groups[i]];
				}
				for (int j = 0; j < m_BlockOverlaps.Length; j++)
				{
					item = m_BlockOverlaps[j];
					item.m_Group = groups[item.m_Group];
					m_BlockOverlaps[j] = item;
				}
				m_BlockOverlaps.Sort();
				OverlapGroup value = new OverlapGroup
				{
					m_StartIndex = 0
				};
				int num = m_BlockOverlaps[0].m_Group;
				for (int k = 0; k < m_BlockOverlaps.Length; k++)
				{
					int num2 = m_BlockOverlaps[k].m_Group;
					if (num2 != num)
					{
						value.m_EndIndex = k;
						m_OverlapGroups.Add(in value);
						value.m_StartIndex = k;
						num = num2;
					}
				}
				value.m_EndIndex = m_BlockOverlaps.Length;
				m_OverlapGroups.Add(in value);
			}
			groups.Dispose();
			nativeParallelHashMap.Dispose();
		}

		private int CreateGroup(NativeList<int> groups)
		{
			int value = groups.Length;
			groups.Add(in value);
			return value;
		}

		private int MergeGroups(NativeList<int> groups, int group1, int group2)
		{
			int num = math.min(group1, group2);
			groups[math.max(group1, group2)] = num;
			return num;
		}
	}

	[BurstCompile]
	public struct UpdateBlocksJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<SortedEntity> m_Blocks;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Cell> m_Cells;

		public void Execute(int index)
		{
			Entity entity = m_Blocks[index].m_Entity;
			Block blockData = m_BlockData[entity];
			DynamicBuffer<Cell> cells = m_Cells[entity];
			SetVisible(blockData, cells);
		}

		private void SetVisible(Block blockData, DynamicBuffer<Cell> cells)
		{
			for (int i = 0; i < blockData.m_Size.y; i++)
			{
				for (int j = 0; j < blockData.m_Size.x; j++)
				{
					int index = i * blockData.m_Size.x + j;
					Cell value = cells[index];
					if ((value.m_State & (CellFlags.Blocked | CellFlags.Redundant)) != CellFlags.None)
					{
						value.m_State &= ~(CellFlags.Shared | CellFlags.Visible);
					}
					else
					{
						value.m_State |= CellFlags.Visible;
					}
					value.m_State &= ~CellFlags.Updating;
					cells[index] = value;
				}
			}
		}
	}
}
