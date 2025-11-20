using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Zones;

public static class CellOverlapJobs
{
	[BurstCompile]
	public struct CheckBlockOverlapJob : IJobParallelForDefer
	{
		private struct CellReduction
		{
			public Entity m_BlockEntity;

			public Entity m_LeftNeightbor;

			public Entity m_RightNeightbor;

			public CellFlags m_Flag;

			public ZonePrefabs m_ZonePrefabs;

			public ComponentLookup<Block> m_BlockDataFromEntity;

			public ComponentLookup<ValidArea> m_ValidAreaDataFromEntity;

			public ComponentLookup<BuildOrder> m_BuildOrderDataFromEntity;

			public ComponentLookup<ZoneData> m_ZoneData;

			public BufferLookup<Cell> m_CellsFromEntity;

			private Block m_BlockData;

			private Block m_LeftBlockData;

			private Block m_RightBlockData;

			private ValidArea m_ValidAreaData;

			private ValidArea m_LeftValidAreaData;

			private ValidArea m_RightValidAreaData;

			private BuildOrder m_BuildOrderData;

			private BuildOrder m_LeftBuildOrderData;

			private BuildOrder m_RightBuildOrderData;

			private DynamicBuffer<Cell> m_Cells;

			private DynamicBuffer<Cell> m_LeftCells;

			private DynamicBuffer<Cell> m_RightCells;

			public void Clear()
			{
				m_BlockData = m_BlockDataFromEntity[m_BlockEntity];
				m_ValidAreaData = m_ValidAreaDataFromEntity[m_BlockEntity];
				m_Cells = m_CellsFromEntity[m_BlockEntity];
				for (int i = m_ValidAreaData.m_Area.x; i < m_ValidAreaData.m_Area.y; i++)
				{
					for (int j = m_ValidAreaData.m_Area.z; j < m_ValidAreaData.m_Area.w; j++)
					{
						int index = j * m_BlockData.m_Size.x + i;
						Cell value = m_Cells[index];
						if ((value.m_State & m_Flag) != CellFlags.None)
						{
							value.m_State &= (CellFlags)(ushort)(~(int)m_Flag);
							m_Cells[index] = value;
						}
					}
				}
			}

			public void Perform()
			{
				m_BlockData = m_BlockDataFromEntity[m_BlockEntity];
				m_ValidAreaData = m_ValidAreaDataFromEntity[m_BlockEntity];
				m_BuildOrderData = m_BuildOrderDataFromEntity[m_BlockEntity];
				m_Cells = m_CellsFromEntity[m_BlockEntity];
				if (m_LeftNeightbor != Entity.Null)
				{
					m_LeftBlockData = m_BlockDataFromEntity[m_LeftNeightbor];
					m_LeftValidAreaData = m_ValidAreaDataFromEntity[m_LeftNeightbor];
					m_LeftBuildOrderData = m_BuildOrderDataFromEntity[m_LeftNeightbor];
					m_LeftCells = m_CellsFromEntity[m_LeftNeightbor];
				}
				else
				{
					m_LeftBlockData = default(Block);
				}
				if (m_RightNeightbor != Entity.Null)
				{
					m_RightBlockData = m_BlockDataFromEntity[m_RightNeightbor];
					m_RightValidAreaData = m_ValidAreaDataFromEntity[m_RightNeightbor];
					m_RightBuildOrderData = m_BuildOrderDataFromEntity[m_RightNeightbor];
					m_RightCells = m_CellsFromEntity[m_RightNeightbor];
				}
				else
				{
					m_RightBlockData = default(Block);
				}
				CellFlags cellFlags = m_Flag | CellFlags.Blocked;
				for (int i = m_ValidAreaData.m_Area.x; i < m_ValidAreaData.m_Area.y; i++)
				{
					Cell value = m_Cells[i];
					Cell cell = m_Cells[m_BlockData.m_Size.x + i];
					if (((value.m_State & cellFlags) == 0) & ((cell.m_State & cellFlags) == m_Flag))
					{
						value.m_State |= m_Flag;
						m_Cells[i] = value;
					}
					for (int j = m_ValidAreaData.m_Area.z + 1; j < m_ValidAreaData.m_Area.w; j++)
					{
						int index = j * m_BlockData.m_Size.x + i;
						Cell cell2 = m_Cells[index];
						if (((cell2.m_State & cellFlags) == 0) & ((value.m_State & cellFlags) == m_Flag))
						{
							cell2.m_State |= m_Flag;
							m_Cells[index] = cell2;
						}
						value = cell2;
					}
				}
				int num = m_ValidAreaData.m_Area.x;
				int num2 = m_ValidAreaData.m_Area.y - 1;
				ValidArea value2 = new ValidArea
				{
					m_Area = 
					{
						xz = m_BlockData.m_Size
					}
				};
				while (num2 >= m_ValidAreaData.m_Area.x)
				{
					if (m_Flag == CellFlags.Occupied)
					{
						Cell cell3 = m_Cells[num];
						Cell cell4 = m_Cells[num2];
						Entity entity = m_ZonePrefabs[cell3.m_Zone];
						Entity entity2 = m_ZonePrefabs[cell4.m_Zone];
						ZoneData zoneData = m_ZoneData[entity];
						ZoneData zoneData2 = m_ZoneData[entity2];
						if ((zoneData.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0)
						{
							int newDepth = CalculateLeftDepth(num, cell3.m_Zone);
							ReduceDepth(num, newDepth);
						}
						if ((zoneData2.m_ZoneFlags & ZoneFlags.SupportNarrow) == 0)
						{
							int newDepth2 = CalculateRightDepth(num2, cell4.m_Zone);
							ReduceDepth(num2, newDepth2);
						}
					}
					else
					{
						int num3 = CalculateLeftDepth(num, ZoneType.None);
						ReduceDepth(num, num3);
						int num4 = CalculateRightDepth(num2, ZoneType.None);
						ReduceDepth(num2, num4);
						if (num2 <= num && m_Flag == CellFlags.Blocked)
						{
							if (num3 != 0 && num != num2)
							{
								value2.m_Area.xz = math.min(value2.m_Area.xz, new int2(num, m_ValidAreaData.m_Area.z));
								value2.m_Area.yw = math.max(value2.m_Area.yw, new int2(num + 1, num3));
							}
							if (num4 != 0)
							{
								value2.m_Area.xz = math.min(value2.m_Area.xz, new int2(num2, m_ValidAreaData.m_Area.z));
								value2.m_Area.yw = math.max(value2.m_Area.yw, new int2(num2 + 1, num4));
							}
						}
					}
					num++;
					num2--;
				}
				if (m_Flag == CellFlags.Blocked)
				{
					m_ValidAreaDataFromEntity[m_BlockEntity] = value2;
				}
			}

			private int CalculateLeftDepth(int x, ZoneType zoneType)
			{
				int depth = GetDepth(x - 1, zoneType);
				int depth2 = GetDepth(x, zoneType);
				if (depth2 <= depth)
				{
					return depth2;
				}
				int depth3 = GetDepth(x - 2, zoneType);
				if (depth != depth3 && depth != 0)
				{
					return depth;
				}
				int depth4 = GetDepth(x + 1, zoneType);
				if (depth4 - depth2 < depth2 - depth)
				{
					return math.min(math.max(depth, depth4), depth2);
				}
				if (GetDepth(x + 2, zoneType) != depth4)
				{
					return math.min(math.max(depth, depth4), depth2);
				}
				return depth;
			}

			private int CalculateRightDepth(int x, ZoneType zoneType)
			{
				int depth = GetDepth(x + 1, zoneType);
				int depth2 = GetDepth(x, zoneType);
				if (depth2 <= depth)
				{
					return depth2;
				}
				int depth3 = GetDepth(x + 2, zoneType);
				if (depth != depth3 && depth != 0)
				{
					return depth;
				}
				int depth4 = GetDepth(x - 1, zoneType);
				if (depth4 - depth2 < depth2 - depth)
				{
					return math.min(math.max(depth4, depth), depth2);
				}
				if (GetDepth(x - 2, zoneType) != depth4)
				{
					return math.min(math.max(depth4, depth), depth2);
				}
				return depth;
			}

			private int GetDepth(int x, ZoneType zoneType)
			{
				if (x < 0)
				{
					x += m_LeftBlockData.m_Size.x;
					if (x < 0)
					{
						return 0;
					}
					if ((m_BuildOrderData.m_Order < m_LeftBuildOrderData.m_Order) & (m_Flag == CellFlags.Blocked))
					{
						return GetDepth(m_BlockData, m_ValidAreaData, m_Cells, 0, m_Flag | CellFlags.Blocked, zoneType);
					}
					return GetDepth(m_LeftBlockData, m_LeftValidAreaData, m_LeftCells, x, m_Flag | CellFlags.Blocked, zoneType);
				}
				if (x >= m_BlockData.m_Size.x)
				{
					x -= m_BlockData.m_Size.x;
					if (x >= m_RightBlockData.m_Size.x)
					{
						return 0;
					}
					if ((m_BuildOrderData.m_Order < m_RightBuildOrderData.m_Order) & (m_Flag == CellFlags.Blocked))
					{
						return GetDepth(m_BlockData, m_ValidAreaData, m_Cells, m_BlockData.m_Size.x - 1, m_Flag | CellFlags.Blocked, zoneType);
					}
					return GetDepth(m_RightBlockData, m_RightValidAreaData, m_RightCells, x, m_Flag | CellFlags.Blocked, zoneType);
				}
				return GetDepth(m_BlockData, m_ValidAreaData, m_Cells, x, m_Flag | CellFlags.Blocked, zoneType);
			}

			private int GetDepth(Block blockData, ValidArea validAreaData, DynamicBuffer<Cell> cells, int x, CellFlags flags, ZoneType zoneType)
			{
				int i = validAreaData.m_Area.z;
				int num = x;
				if (m_Flag == CellFlags.Occupied)
				{
					for (; i < validAreaData.m_Area.w; i++)
					{
						if ((cells[num].m_State & flags) != CellFlags.None)
						{
							break;
						}
						if (!cells[num].m_Zone.Equals(zoneType))
						{
							break;
						}
						num += blockData.m_Size.x;
					}
				}
				else
				{
					for (; i < validAreaData.m_Area.w; i++)
					{
						if ((cells[num].m_State & flags) != CellFlags.None)
						{
							break;
						}
						num += blockData.m_Size.x;
					}
				}
				return i;
			}

			private void ReduceDepth(int x, int newDepth)
			{
				CellFlags cellFlags = m_Flag | CellFlags.Blocked;
				int num = m_BlockData.m_Size.x * newDepth + x;
				for (int i = newDepth; i < m_ValidAreaData.m_Area.w; i++)
				{
					Cell value = m_Cells[num];
					if ((value.m_State & cellFlags) != CellFlags.None)
					{
						break;
					}
					value.m_State |= m_Flag;
					m_Cells[num] = value;
					num += m_BlockData.m_Size.x;
				}
			}
		}

		private struct OverlapIterator
		{
			public Entity m_BlockEntity;

			public Quad2 m_Quad;

			public Bounds2 m_Bounds;

			public Block m_BlockData;

			public ValidArea m_ValidAreaData;

			public BuildOrder m_BuildOrderData;

			public DynamicBuffer<Cell> m_Cells;

			public ComponentLookup<Block> m_BlockDataFromEntity;

			public ComponentLookup<ValidArea> m_ValidAreaDataFromEntity;

			public ComponentLookup<BuildOrder> m_BuildOrderDataFromEntity;

			public BufferLookup<Cell> m_CellsFromEntity;

			public bool m_CheckSharing;

			public bool m_CheckBlocking;

			public bool m_CheckDepth;

			private Block m_BlockData2;

			private ValidArea m_ValidAreaData2;

			private BuildOrder m_BuildOrderData2;

			private DynamicBuffer<Cell> m_Cells2;

			public void Iterate(Entity blockEntity2)
			{
				m_BlockData2 = m_BlockDataFromEntity[blockEntity2];
				m_ValidAreaData2 = m_ValidAreaDataFromEntity[blockEntity2];
				m_BuildOrderData2 = m_BuildOrderDataFromEntity[blockEntity2];
				m_Cells2 = m_CellsFromEntity[blockEntity2];
				if (m_ValidAreaData2.m_Area.y <= m_ValidAreaData2.m_Area.x)
				{
					return;
				}
				if (ZoneUtils.CanShareCells(m_BlockData, m_BlockData2, m_BuildOrderData, m_BuildOrderData2))
				{
					if (!m_CheckSharing)
					{
						return;
					}
					m_CheckDepth = false;
				}
				else
				{
					if (m_CheckSharing)
					{
						return;
					}
					m_CheckDepth = math.dot(m_BlockData.m_Direction, m_BlockData2.m_Direction) < -0.6946584f;
				}
				Quad2 quad = ZoneUtils.CalculateCorners(m_BlockData2, m_ValidAreaData2);
				CheckOverlapX1(m_Bounds, MathUtils.Bounds(quad), m_Quad, quad, m_ValidAreaData.m_Area, m_ValidAreaData2.m_Area);
			}

			private void CheckOverlapX1(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, int4 xxzz2)
			{
				if (xxzz1.y - xxzz1.x >= 2)
				{
					int4 xxzz3 = xxzz1;
					int4 xxzz4 = xxzz1;
					xxzz3.y = xxzz1.x + xxzz1.y >> 1;
					xxzz4.x = xxzz3.y;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz3.y - xxzz1.x) / (float)(xxzz1.y - xxzz1.x);
					quad3.b = math.lerp(quad1.a, quad1.b, t);
					quad3.c = math.lerp(quad1.d, quad1.c, t);
					quad4.a = quad3.b;
					quad4.d = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2))
					{
						CheckOverlapZ1(bounds3, bounds2, quad3, quad2, xxzz3, xxzz2);
					}
					if (MathUtils.Intersect(bounds4, bounds2))
					{
						CheckOverlapZ1(bounds4, bounds2, quad4, quad2, xxzz4, xxzz2);
					}
				}
				else
				{
					CheckOverlapZ1(bounds1, bounds2, quad1, quad2, xxzz1, xxzz2);
				}
			}

			private void CheckOverlapZ1(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, int4 xxzz2)
			{
				if (xxzz1.w - xxzz1.z >= 2)
				{
					int4 xxzz3 = xxzz1;
					int4 xxzz4 = xxzz1;
					xxzz3.w = xxzz1.z + xxzz1.w >> 1;
					xxzz4.z = xxzz3.w;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz3.w - xxzz1.z) / (float)(xxzz1.w - xxzz1.z);
					quad3.d = math.lerp(quad1.a, quad1.d, t);
					quad3.c = math.lerp(quad1.b, quad1.c, t);
					quad4.a = quad3.d;
					quad4.b = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2))
					{
						CheckOverlapX2(bounds3, bounds2, quad3, quad2, xxzz3, xxzz2);
					}
					if (MathUtils.Intersect(bounds4, bounds2))
					{
						CheckOverlapX2(bounds4, bounds2, quad4, quad2, xxzz4, xxzz2);
					}
				}
				else
				{
					CheckOverlapX2(bounds1, bounds2, quad1, quad2, xxzz1, xxzz2);
				}
			}

			private void CheckOverlapX2(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, int4 xxzz2)
			{
				if (xxzz2.y - xxzz2.x >= 2)
				{
					int4 xxzz3 = xxzz2;
					int4 xxzz4 = xxzz2;
					xxzz3.y = xxzz2.x + xxzz2.y >> 1;
					xxzz4.x = xxzz3.y;
					Quad2 quad3 = quad2;
					Quad2 quad4 = quad2;
					float t = (float)(xxzz3.y - xxzz2.x) / (float)(xxzz2.y - xxzz2.x);
					quad3.b = math.lerp(quad2.a, quad2.b, t);
					quad3.c = math.lerp(quad2.d, quad2.c, t);
					quad4.a = quad3.b;
					quad4.d = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds1, bounds3))
					{
						CheckOverlapZ2(bounds1, bounds3, quad1, quad3, xxzz1, xxzz3);
					}
					if (MathUtils.Intersect(bounds1, bounds4))
					{
						CheckOverlapZ2(bounds1, bounds4, quad1, quad4, xxzz1, xxzz4);
					}
				}
				else
				{
					CheckOverlapZ2(bounds1, bounds2, quad1, quad2, xxzz1, xxzz2);
				}
			}

			private void CheckOverlapZ2(Bounds2 bounds1, Bounds2 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, int4 xxzz2)
			{
				if (xxzz2.w - xxzz2.z >= 2)
				{
					int4 xxzz3 = xxzz2;
					int4 xxzz4 = xxzz2;
					xxzz3.w = xxzz2.z + xxzz2.w >> 1;
					xxzz4.z = xxzz3.w;
					Quad2 quad3 = quad2;
					Quad2 quad4 = quad2;
					float t = (float)(xxzz3.w - xxzz2.z) / (float)(xxzz2.w - xxzz2.z);
					quad3.d = math.lerp(quad2.a, quad2.d, t);
					quad3.c = math.lerp(quad2.b, quad2.c, t);
					quad4.a = quad3.d;
					quad4.b = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds1, bounds3))
					{
						CheckOverlapX1(bounds1, bounds3, quad1, quad3, xxzz1, xxzz3);
					}
					if (MathUtils.Intersect(bounds1, bounds4))
					{
						CheckOverlapX1(bounds1, bounds4, quad1, quad4, xxzz1, xxzz4);
					}
					return;
				}
				if (math.any(xxzz1.yw - xxzz1.xz >= 2) | math.any(xxzz2.yw - xxzz2.xz >= 2))
				{
					CheckOverlapX1(bounds1, bounds2, quad1, quad2, xxzz1, xxzz2);
					return;
				}
				int index = xxzz1.z * m_BlockData.m_Size.x + xxzz1.x;
				int index2 = xxzz2.z * m_BlockData2.m_Size.x + xxzz2.x;
				Cell cell = m_Cells[index];
				Cell cell2 = m_Cells2[index2];
				if (((cell.m_State | cell2.m_State) & CellFlags.Blocked) != CellFlags.None)
				{
					return;
				}
				if (m_CheckSharing)
				{
					if (math.lengthsq(MathUtils.Center(quad1) - MathUtils.Center(quad2)) < 16f)
					{
						if (CheckPriority(cell, cell2, xxzz1.z, xxzz2.z, m_BuildOrderData.m_Order, m_BuildOrderData2.m_Order) && (cell2.m_State & CellFlags.Shared) == 0)
						{
							cell.m_State |= CellFlags.Shared;
							cell.m_State = (cell.m_State & ~CellFlags.Overridden) | (cell2.m_State & CellFlags.Overridden);
							cell.m_Zone = cell2.m_Zone;
						}
						if ((cell2.m_State & CellFlags.Roadside) != CellFlags.None && xxzz2.z == 0)
						{
							cell.m_State |= ZoneUtils.GetRoadDirection(m_BlockData, m_BlockData2);
						}
						cell.m_State &= ~CellFlags.Occupied | (cell2.m_State & CellFlags.Occupied);
						m_Cells[index] = cell;
					}
				}
				else if (CheckPriority(cell, cell2, xxzz1.z, xxzz2.z, m_BuildOrderData.m_Order, m_BuildOrderData2.m_Order))
				{
					quad1 = MathUtils.Expand(quad1, -0.01f);
					quad2 = MathUtils.Expand(quad2, -0.01f);
					if (MathUtils.Intersect(quad1, quad2))
					{
						cell.m_State = (CellFlags)((uint)(cell.m_State & ~CellFlags.Shared) | (uint)(m_CheckBlocking ? 1 : 128));
						m_Cells[index] = cell;
					}
				}
				else if (math.lengthsq(MathUtils.Center(quad1) - MathUtils.Center(quad2)) < 64f && (cell2.m_State & CellFlags.Roadside) != CellFlags.None && xxzz2.z == 0)
				{
					cell.m_State |= ZoneUtils.GetRoadDirection(m_BlockData, m_BlockData2);
					m_Cells[index] = cell;
				}
			}

			private bool CheckPriority(Cell cell1, Cell cell2, int depth1, int depth2, uint order1, uint order2)
			{
				if ((cell2.m_State & CellFlags.Updating) == 0)
				{
					return (cell2.m_State & CellFlags.Visible) != 0;
				}
				if (m_CheckBlocking)
				{
					return ((uint)cell1.m_State & (uint)(ushort)(~(int)cell2.m_State) & 0x80) != 0;
				}
				if (m_CheckDepth)
				{
					if (cell1.m_Zone.Equals(ZoneType.None) != cell2.m_Zone.Equals(ZoneType.None))
					{
						return cell1.m_Zone.Equals(ZoneType.None);
					}
					if (cell1.m_Zone.Equals(ZoneType.None) && ((cell1.m_State | cell2.m_State) & CellFlags.Overridden) == 0 && math.max(0, depth1 - 1) != math.max(0, depth2 - 1))
					{
						return depth2 < depth1;
					}
				}
				if (((cell1.m_State ^ cell2.m_State) & CellFlags.Visible) != CellFlags.None)
				{
					return (cell2.m_State & CellFlags.Visible) != 0;
				}
				return order2 < order1;
			}
		}

		[NativeDisableParallelForRestriction]
		public NativeArray<CellCheckHelpers.BlockOverlap> m_BlockOverlaps;

		[ReadOnly]
		public NativeArray<CellCheckHelpers.OverlapGroup> m_OverlapGroups;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<BuildOrder> m_BuildOrderData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Cell> m_Cells;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		public void Execute(int index)
		{
			CellCheckHelpers.OverlapGroup overlapGroup = m_OverlapGroups[index];
			CellCheckHelpers.BlockOverlap value = default(CellCheckHelpers.BlockOverlap);
			int index2 = 0;
			Block block = default(Block);
			BuildOrder buildOrder = default(BuildOrder);
			for (int i = overlapGroup.m_StartIndex; i < overlapGroup.m_EndIndex; i++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap = m_BlockOverlaps[i];
				if (blockOverlap.m_Block != value.m_Block)
				{
					if (value.m_Block != Entity.Null)
					{
						m_BlockOverlaps[index2] = value;
					}
					value = blockOverlap;
					index2 = i;
					block = m_BlockData[blockOverlap.m_Block];
					_ = m_ValidAreaData[blockOverlap.m_Block];
					buildOrder = m_BuildOrderData[blockOverlap.m_Block];
					_ = m_Cells[blockOverlap.m_Block];
				}
				if (!(blockOverlap.m_Other != Entity.Null))
				{
					continue;
				}
				Block block2 = m_BlockData[blockOverlap.m_Other];
				BuildOrder buildOrder2 = m_BuildOrderData[blockOverlap.m_Other];
				if (ZoneUtils.IsNeighbor(block, block2, buildOrder, buildOrder2))
				{
					if (math.dot(block2.m_Position.xz - block.m_Position.xz, MathUtils.Right(block.m_Direction)) > 0f)
					{
						value.m_Left = blockOverlap.m_Other;
					}
					else
					{
						value.m_Right = blockOverlap.m_Other;
					}
				}
			}
			if (value.m_Block != Entity.Null)
			{
				m_BlockOverlaps[index2] = value;
			}
			OverlapIterator overlapIterator = new OverlapIterator
			{
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_BuildOrderDataFromEntity = m_BuildOrderData,
				m_CellsFromEntity = m_Cells
			};
			CellReduction cellReduction = new CellReduction
			{
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_BuildOrderDataFromEntity = m_BuildOrderData,
				m_CellsFromEntity = m_Cells,
				m_Flag = CellFlags.Redundant
			};
			for (int j = overlapGroup.m_StartIndex; j < overlapGroup.m_EndIndex; j++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap2 = m_BlockOverlaps[j];
				if (blockOverlap2.m_Block != overlapIterator.m_BlockEntity)
				{
					if (cellReduction.m_BlockEntity != Entity.Null)
					{
						cellReduction.Perform();
					}
					cellReduction.m_BlockEntity = blockOverlap2.m_Block;
					cellReduction.m_LeftNeightbor = blockOverlap2.m_Left;
					cellReduction.m_RightNeightbor = blockOverlap2.m_Right;
					overlapIterator.m_BlockEntity = blockOverlap2.m_Block;
					overlapIterator.m_BlockData = m_BlockData[overlapIterator.m_BlockEntity];
					overlapIterator.m_ValidAreaData = m_ValidAreaData[overlapIterator.m_BlockEntity];
					overlapIterator.m_BuildOrderData = m_BuildOrderData[overlapIterator.m_BlockEntity];
					overlapIterator.m_Cells = m_Cells[overlapIterator.m_BlockEntity];
					overlapIterator.m_Quad = ZoneUtils.CalculateCorners(overlapIterator.m_BlockData, overlapIterator.m_ValidAreaData);
					overlapIterator.m_Bounds = MathUtils.Bounds(overlapIterator.m_Quad);
				}
				if (overlapIterator.m_ValidAreaData.m_Area.y > overlapIterator.m_ValidAreaData.m_Area.x && blockOverlap2.m_Other != Entity.Null)
				{
					overlapIterator.Iterate(blockOverlap2.m_Other);
				}
			}
			if (cellReduction.m_BlockEntity != Entity.Null)
			{
				cellReduction.Perform();
			}
			overlapIterator.m_BlockEntity = Entity.Null;
			overlapIterator.m_CheckBlocking = true;
			cellReduction.m_BlockEntity = Entity.Null;
			for (int k = overlapGroup.m_StartIndex; k < overlapGroup.m_EndIndex; k++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap3 = m_BlockOverlaps[k];
				if (blockOverlap3.m_Block != overlapIterator.m_BlockEntity)
				{
					if (cellReduction.m_BlockEntity != Entity.Null)
					{
						cellReduction.m_Flag = CellFlags.Redundant;
						cellReduction.Clear();
						cellReduction.m_Flag = CellFlags.Blocked;
						cellReduction.Perform();
					}
					cellReduction.m_BlockEntity = blockOverlap3.m_Block;
					cellReduction.m_LeftNeightbor = blockOverlap3.m_Left;
					cellReduction.m_RightNeightbor = blockOverlap3.m_Right;
					overlapIterator.m_BlockEntity = blockOverlap3.m_Block;
					overlapIterator.m_BlockData = m_BlockData[overlapIterator.m_BlockEntity];
					overlapIterator.m_ValidAreaData = m_ValidAreaData[overlapIterator.m_BlockEntity];
					overlapIterator.m_BuildOrderData = m_BuildOrderData[overlapIterator.m_BlockEntity];
					overlapIterator.m_Cells = m_Cells[overlapIterator.m_BlockEntity];
					overlapIterator.m_Quad = ZoneUtils.CalculateCorners(overlapIterator.m_BlockData, overlapIterator.m_ValidAreaData);
					overlapIterator.m_Bounds = MathUtils.Bounds(overlapIterator.m_Quad);
				}
				if (overlapIterator.m_ValidAreaData.m_Area.y > overlapIterator.m_ValidAreaData.m_Area.x && blockOverlap3.m_Other != Entity.Null)
				{
					overlapIterator.Iterate(blockOverlap3.m_Other);
				}
			}
			if (cellReduction.m_BlockEntity != Entity.Null)
			{
				cellReduction.m_Flag = CellFlags.Redundant;
				cellReduction.Clear();
				cellReduction.m_Flag = CellFlags.Blocked;
				cellReduction.Perform();
			}
			CellReduction cellReduction2 = new CellReduction
			{
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_BuildOrderDataFromEntity = m_BuildOrderData,
				m_CellsFromEntity = m_Cells,
				m_Flag = CellFlags.Redundant
			};
			for (int l = overlapGroup.m_StartIndex; l < overlapGroup.m_EndIndex; l++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap4 = m_BlockOverlaps[l];
				if (blockOverlap4.m_Block != cellReduction2.m_BlockEntity)
				{
					cellReduction2.m_BlockEntity = blockOverlap4.m_Block;
					cellReduction2.m_LeftNeightbor = blockOverlap4.m_Left;
					cellReduction2.m_RightNeightbor = blockOverlap4.m_Right;
					cellReduction2.Perform();
				}
			}
			CellReduction cellReduction3 = new CellReduction
			{
				m_ZonePrefabs = m_ZonePrefabs,
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_BuildOrderDataFromEntity = m_BuildOrderData,
				m_ZoneData = m_ZoneData,
				m_CellsFromEntity = m_Cells,
				m_Flag = CellFlags.Occupied
			};
			for (int m = overlapGroup.m_StartIndex; m < overlapGroup.m_EndIndex; m++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap5 = m_BlockOverlaps[m];
				if (blockOverlap5.m_Block != cellReduction3.m_BlockEntity)
				{
					cellReduction3.m_BlockEntity = blockOverlap5.m_Block;
					cellReduction3.m_LeftNeightbor = blockOverlap5.m_Left;
					cellReduction3.m_RightNeightbor = blockOverlap5.m_Right;
					cellReduction3.Perform();
				}
			}
			OverlapIterator overlapIterator2 = new OverlapIterator
			{
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_BuildOrderDataFromEntity = m_BuildOrderData,
				m_CellsFromEntity = m_Cells,
				m_CheckSharing = true
			};
			for (int n = overlapGroup.m_StartIndex; n < overlapGroup.m_EndIndex; n++)
			{
				CellCheckHelpers.BlockOverlap blockOverlap6 = m_BlockOverlaps[n];
				if (blockOverlap6.m_Block != overlapIterator2.m_BlockEntity)
				{
					overlapIterator2.m_BlockEntity = blockOverlap6.m_Block;
					overlapIterator2.m_BlockData = m_BlockData[overlapIterator2.m_BlockEntity];
					overlapIterator2.m_ValidAreaData = m_ValidAreaData[overlapIterator2.m_BlockEntity];
					overlapIterator2.m_BuildOrderData = m_BuildOrderData[overlapIterator2.m_BlockEntity];
					overlapIterator2.m_Cells = m_Cells[overlapIterator2.m_BlockEntity];
					overlapIterator2.m_Quad = ZoneUtils.CalculateCorners(overlapIterator2.m_BlockData, overlapIterator2.m_ValidAreaData);
					overlapIterator2.m_Bounds = MathUtils.Bounds(overlapIterator2.m_Quad);
				}
				if (overlapIterator2.m_ValidAreaData.m_Area.y > overlapIterator2.m_ValidAreaData.m_Area.x && blockOverlap6.m_Other != Entity.Null)
				{
					overlapIterator2.Iterate(blockOverlap6.m_Other);
				}
			}
		}
	}
}
