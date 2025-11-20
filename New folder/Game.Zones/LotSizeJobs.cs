using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Zones;

public static class LotSizeJobs
{
	[BurstCompile]
	public struct UpdateLotSizeJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
		{
			public Entity m_Entity;

			public Block m_Block;

			public ValidArea m_ValidArea;

			public BuildOrder m_BuildOrder;

			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public ComponentLookup<Block> m_BlockData;

			public ComponentLookup<ValidArea> m_ValidAreaData;

			public ComponentLookup<BuildOrder> m_BuildOrderData;

			public BufferLookup<Cell> m_CellData;

			public NativeArray<Cell> m_Cells;

			public bool Intersect(Bounds2 bounds)
			{
				return MathUtils.Intersect(bounds, m_Bounds);
			}

			public void Iterate(Bounds2 bounds, Entity entity2)
			{
				if (!MathUtils.Intersect(bounds, m_Bounds) || m_Entity.Equals(entity2))
				{
					return;
				}
				ValidArea validArea = m_ValidAreaData[entity2];
				if (validArea.m_Area.y <= validArea.m_Area.x)
				{
					return;
				}
				Block block = m_BlockData[entity2];
				Quad2 quad = MathUtils.Expand(ZoneUtils.CalculateCorners(block, validArea), -0.01f);
				if (!MathUtils.Intersect(m_Quad, quad))
				{
					return;
				}
				BuildOrder buildOrder = m_BuildOrderData[entity2];
				if (!ZoneUtils.CanShareCells(m_Block, block, m_BuildOrder, buildOrder))
				{
					return;
				}
				float3 cellPosition = ZoneUtils.GetCellPosition(block, default(int2));
				int2 cellIndex = ZoneUtils.GetCellIndex(m_Block, cellPosition.xz);
				float num = math.dot(m_Block.m_Direction, block.m_Direction);
				float num2 = math.dot(MathUtils.Left(m_Block.m_Direction), block.m_Direction);
				int2 x;
				int2 x2;
				if (num > 0.5f)
				{
					x = new int2(1, 0);
					x2 = new int2(0, 1);
				}
				else if (num < -0.5f)
				{
					x = new int2(-1, 0);
					x2 = new int2(0, -1);
				}
				else if (num2 > 0.5f)
				{
					x = new int2(0, -1);
					x2 = new int2(1, 0);
				}
				else
				{
					x = new int2(0, 1);
					x2 = new int2(-1, 0);
				}
				DynamicBuffer<Cell> dynamicBuffer = m_CellData[entity2];
				int2 y = default(int2);
				y.y = validArea.m_Area.z;
				while (y.y < validArea.m_Area.w)
				{
					y.x = validArea.m_Area.x;
					while (y.x < validArea.m_Area.y)
					{
						int2 @int = cellIndex + new int2(math.dot(x, y), math.dot(x2, y));
						if (!(math.any(@int < 0) | math.any(@int >= m_Block.m_Size)))
						{
							int index = y.y * block.m_Size.x + y.x;
							int index2 = @int.y * m_Block.m_Size.x + @int.x;
							Cell value = dynamicBuffer[index];
							if ((value.m_State & (CellFlags.Blocked | CellFlags.Shared | CellFlags.Occupied | CellFlags.Redundant)) == 0 && !value.m_Zone.Equals(ZoneType.None))
							{
								if ((value.m_State & (CellFlags.Roadside | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) != CellFlags.None)
								{
									value.m_State = (value.m_State & ~(CellFlags.Roadside | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) | ZoneUtils.GetRoadDirection(m_Block, block, value.m_State);
								}
								m_Cells[index2] = value;
							}
						}
						y.x++;
					}
					y.y++;
				}
			}
		}

		[ReadOnly]
		public NativeArray<CellCheckHelpers.SortedEntity> m_Blocks;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public ComponentLookup<BuildOrder> m_BuildOrderData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[ReadOnly]
		public BufferLookup<Cell> m_Cells;

		[ReadOnly]
		public NativeQuadTree<Entity, Bounds2> m_SearchTree;

		[NativeDisableParallelForRestriction]
		public BufferLookup<VacantLot> m_VacantLots;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<Bounds2>.ParallelWriter m_BoundsQueue;

		public void Execute(int index)
		{
			Entity entity = m_Blocks[index].m_Entity;
			Block block = m_BlockData[entity];
			ValidArea validArea = m_ValidAreaData[entity];
			BuildOrder buildOrder = m_BuildOrderData[entity];
			DynamicBuffer<Cell> dynamicBuffer = m_Cells[entity];
			DynamicBuffer<VacantLot> dynamicBuffer2 = default(DynamicBuffer<VacantLot>);
			if (m_VacantLots.HasBuffer(entity))
			{
				dynamicBuffer2 = m_VacantLots[entity];
				dynamicBuffer2.Clear();
			}
			NativeArray<Cell> cells = default(NativeArray<Cell>);
			int2 expandedOffset = default(int2);
			Block expandedBlock = default(Block);
			int num = validArea.m_Area.x;
			while (num < validArea.m_Area.y)
			{
				Cell cell = dynamicBuffer[validArea.m_Area.z * block.m_Size.x + num];
				if ((cell.m_State & (CellFlags.Blocked | CellFlags.Occupied)) == 0 && !cell.m_Zone.Equals(ZoneType.None))
				{
					int2 min = new int2(num, validArea.m_Area.z);
					int2 max = default(int2);
					if (!cells.IsCreated)
					{
						FindDepth(block, dynamicBuffer.AsNativeArray(), ref min, ref max, cell.m_Zone);
						ExpandRight(block, dynamicBuffer.AsNativeArray(), ref min, ref max, cell.m_Zone);
						if (min.x == 0 || math.any(max == block.m_Size))
						{
							cells = ExpandArea(entity, block, validArea, buildOrder, dynamicBuffer.AsNativeArray(), out expandedOffset, out expandedBlock);
						}
					}
					int2 @int = min;
					int2 int2 = max;
					Entity entity2 = m_ZonePrefabs[cell.m_Zone];
					ZoneData zoneData = m_ZoneData[entity2];
					Cell cell2;
					Cell cell3;
					if (cells.IsCreated)
					{
						min += expandedOffset;
						FindDepth(expandedBlock, cells, ref min, ref max, cell.m_Zone);
						ExpandRight(expandedBlock, cells, ref min, ref max, cell.m_Zone);
						if (num == 0)
						{
							ExpandLeft(expandedBlock, cells, ref min, ref max, cell.m_Zone);
						}
						min -= expandedOffset;
						max -= expandedOffset;
						@int = min;
						int2 = max;
						int num2 = math.select(0, 1, (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0);
						if (min.x < -num2)
						{
							WidthReductionLeft(block, ref min, ref max, num2);
						}
						if (max.x > block.m_Size.x + num2)
						{
							WidthReductionRight(block, ref min, ref max, num2);
						}
						int2 int3 = min + expandedOffset;
						int2 int4 = max + expandedOffset;
						cell2 = cells[int3.y * expandedBlock.m_Size.x + int3.x];
						cell3 = cells[int3.y * expandedBlock.m_Size.x + (int4.x - 1)];
					}
					else
					{
						cell2 = dynamicBuffer[min.y * block.m_Size.x + min.x];
						cell3 = dynamicBuffer[min.y * block.m_Size.x + (max.x - 1)];
					}
					LotFlags lotFlags = (LotFlags)0;
					if ((cell2.m_State & CellFlags.RoadLeft) != CellFlags.None)
					{
						lotFlags |= LotFlags.CornerLeft;
					}
					if ((cell3.m_State & CellFlags.RoadRight) != CellFlags.None)
					{
						lotFlags |= LotFlags.CornerRight;
					}
					int2 int5 = new int2(math.select(2, 1, (zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) != 0), 2);
					if (math.all(max - min >= int5))
					{
						if (!dynamicBuffer2.IsCreated)
						{
							dynamicBuffer2 = m_CommandBuffer.AddBuffer<VacantLot>(index, entity);
						}
						if (max.x - min.x > 8)
						{
							int x = math.min(min.x + int2.x + 1 >> 1, min.x + 6 + 2);
							int x2 = math.max(@int.x + max.x >> 1, max.x - 6 - 2);
							int2 int6 = new int2(x, max.y);
							int2 int7 = new int2(x2, min.y);
							int height = cell.m_Height;
							int height2 = cell.m_Height;
							FindHeight(block, dynamicBuffer.AsNativeArray(), min, int6, ref height);
							FindHeight(block, dynamicBuffer.AsNativeArray(), min, int6, ref height2);
							if (cells.IsCreated)
							{
								FindHeight(expandedBlock, cells, min + expandedOffset, int6 + expandedOffset, ref height);
								FindHeight(expandedBlock, cells, int7 + expandedOffset, max + expandedOffset, ref height2);
							}
							dynamicBuffer2.Add(new VacantLot(min, int6, cell.m_Zone, height, lotFlags & ~LotFlags.CornerRight));
							dynamicBuffer2.Add(new VacantLot(int7, max, cell.m_Zone, height2, lotFlags & ~LotFlags.CornerLeft));
						}
						else
						{
							int height3 = cell.m_Height;
							FindHeight(block, dynamicBuffer.AsNativeArray(), min, max, ref height3);
							if (cells.IsCreated)
							{
								FindHeight(expandedBlock, cells, min + expandedOffset, max + expandedOffset, ref height3);
							}
							dynamicBuffer2.Add(new VacantLot(min, max, cell.m_Zone, height3, lotFlags));
						}
					}
					num = int2.x;
				}
				else
				{
					num++;
				}
			}
			if (dynamicBuffer2.IsCreated && dynamicBuffer2.Length == 0)
			{
				m_CommandBuffer.RemoveComponent<VacantLot>(index, entity);
			}
			if (cells.IsCreated)
			{
				cells.Dispose();
			}
			if (!m_UpdatedData.HasComponent(entity))
			{
				m_CommandBuffer.AddComponent(index, entity, default(Updated));
				m_BoundsQueue.Enqueue(ZoneUtils.CalculateBounds(block));
			}
		}

		private void FindHeight(Block block, NativeArray<Cell> cells, int2 min, int2 max, ref int height)
		{
			min = math.max(min, 0);
			max = math.min(max, block.m_Size);
			for (int i = min.y; i < max.y; i++)
			{
				for (int j = min.x; j < max.x; j++)
				{
					int index = i * block.m_Size.x + j;
					Cell cell = cells[index];
					height = math.min(height, cell.m_Height);
				}
			}
		}

		private void FindDepth(Block block, NativeArray<Cell> cells, ref int2 min, ref int2 max, ZoneType zone)
		{
			max.y = block.m_Size.y;
			for (int i = min.y + 1; i < block.m_Size.y; i++)
			{
				int index = i * block.m_Size.x + min.x;
				Cell cell = cells[index];
				if ((cell.m_State & (CellFlags.Blocked | CellFlags.Occupied)) != CellFlags.None || !cell.m_Zone.Equals(zone))
				{
					max.y = i;
					break;
				}
			}
			if (max.y > 6)
			{
				max.y = max.y + 1 >> 1;
			}
		}

		private void ExpandRight(Block block, NativeArray<Cell> cells, ref int2 min, ref int2 max, ZoneType zone)
		{
			for (int i = min.x + 1; i < block.m_Size.x; i++)
			{
				for (int j = min.y; j < max.y; j++)
				{
					int index = j * block.m_Size.x + i;
					Cell cell = cells[index];
					if ((cell.m_State & (CellFlags.Blocked | CellFlags.Occupied)) != CellFlags.None || !cell.m_Zone.Equals(zone))
					{
						max.x = i;
						return;
					}
				}
			}
			max.x = block.m_Size.x;
		}

		private void ExpandLeft(Block block, NativeArray<Cell> cells, ref int2 min, ref int2 max, ZoneType zone)
		{
			for (int num = min.x - 1; num >= 0; num--)
			{
				for (int i = min.y; i < max.y; i++)
				{
					int index = i * block.m_Size.x + num;
					Cell cell = cells[index];
					if ((cell.m_State & (CellFlags.Blocked | CellFlags.Occupied)) != CellFlags.None || !cell.m_Zone.Equals(zone))
					{
						min.x = num + 1;
						return;
					}
				}
			}
			min.x = 0;
		}

		private void WidthReductionLeft(Block block, ref int2 min, ref int2 max, int sizeOffset)
		{
			int num = 3;
			if (max.x < num && min.x < -max.x)
			{
				min.x = max.x;
			}
			else if ((min.x <= -num || max.x < -min.x) && max.x - min.x > 6)
			{
				min.x = math.min(-sizeOffset, min.x + max.x >> 1);
			}
		}

		private void WidthReductionRight(Block block, ref int2 min, ref int2 max, int sizeOffset)
		{
			int num = 3;
			if (min.x > block.m_Size.x - num && max.x - block.m_Size.x > block.m_Size.x - min.x)
			{
				max.x = min.x;
			}
			else if ((max.x - block.m_Size.x >= num || block.m_Size.x - min.x < max.x - block.m_Size.x) && max.x - min.x > 6)
			{
				max.x = math.max(block.m_Size.x + sizeOffset, min.x + max.x + 1 >> 1);
			}
		}

		private NativeArray<Cell> ExpandArea(Entity entity, Block block, ValidArea validArea, BuildOrder buildOrder, NativeArray<Cell> cells, out int2 expandedOffset, out Block expandedBlock)
		{
			expandedBlock = block;
			expandedOffset.x = math.select(0, 6, validArea.m_Area.x == 0);
			expandedOffset.y = 0;
			expandedBlock.m_Size += expandedOffset + math.select(default(int2), 6, validArea.m_Area.yw == block.m_Size);
			float3 @float = new float3(0f - block.m_Direction.y, 0f, block.m_Direction.x);
			float3 float2 = new float3(0f - block.m_Direction.x, 0f, 0f - block.m_Direction.y);
			float2 float3 = (float2)(expandedBlock.m_Size - (expandedOffset << 1) - block.m_Size) * 4f;
			expandedBlock.m_Position += @float * float3.x + float2 * float3.y;
			NativeArray<Cell> nativeArray = new NativeArray<Cell>(expandedBlock.m_Size.x * expandedBlock.m_Size.y, Allocator.Temp);
			int2 @int = default(int2);
			@int.y = 0;
			while (@int.y < block.m_Size.y)
			{
				@int.x = 0;
				while (@int.x < block.m_Size.x)
				{
					int2 int2 = @int + expandedOffset;
					int index = @int.y * block.m_Size.x + @int.x;
					int index2 = int2.y * expandedBlock.m_Size.x + int2.x;
					nativeArray[index2] = cells[index];
					@int.x++;
				}
				@int.y++;
			}
			Quad2 quad = ZoneUtils.CalculateCorners(expandedBlock);
			Iterator iterator = new Iterator
			{
				m_Entity = entity,
				m_Block = expandedBlock,
				m_ValidArea = validArea,
				m_BuildOrder = buildOrder,
				m_Bounds = MathUtils.Bounds(quad),
				m_Quad = quad,
				m_BlockData = m_BlockData,
				m_ValidAreaData = m_ValidAreaData,
				m_BuildOrderData = m_BuildOrderData,
				m_CellData = m_Cells,
				m_Cells = nativeArray
			};
			m_SearchTree.Iterate(ref iterator);
			return nativeArray;
		}
	}

	[BurstCompile]
	public struct UpdateBoundsJob : IJob
	{
		public NativeQueue<Bounds2> m_BoundsQueue;

		public NativeList<Bounds2> m_BoundsList;

		public void Execute()
		{
			int count = m_BoundsQueue.Count;
			if (count != 0)
			{
				m_BoundsList.Capacity = math.max(m_BoundsList.Capacity, m_BoundsList.Length + count);
				for (int i = 0; i < count; i++)
				{
					m_BoundsList.Add(m_BoundsQueue.Dequeue());
				}
			}
		}
	}
}
