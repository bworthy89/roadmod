using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Zones;

public static class CellOccupyJobs
{
	[BurstCompile]
	public struct ZoneAndOccupyCellsJob : IJobParallelForDefer
	{
		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_BlockEntity;

			public Block m_BlockData;

			public Bounds2 m_Bounds;

			public Quad2 m_Quad;

			public int4 m_Xxzz;

			public DynamicBuffer<Cell> m_Cells;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<Elevation> m_ElevationData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

			public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

			public ComponentLookup<PlaceholderBuildingData> m_PrefabPlaceholderBuildingData;

			public ComponentLookup<ZoneData> m_PrefabZoneData;

			public ComponentLookup<PrefabData> m_PrefabData;

			private bool m_ShouldOverride;

			private ZoneType m_OverrideZone;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if ((bounds.m_Mask & (BoundsMask.OccupyZone | BoundsMask.NotOverridden)) != (BoundsMask.OccupyZone | BoundsMask.NotOverridden))
				{
					return false;
				}
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if ((bounds.m_Mask & (BoundsMask.OccupyZone | BoundsMask.NotOverridden)) != (BoundsMask.OccupyZone | BoundsMask.NotOverridden) || !MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					return;
				}
				bool flag = false;
				if (m_ElevationData.HasComponent(objectEntity))
				{
					Elevation elevation = m_ElevationData[objectEntity];
					if (elevation.m_Elevation < 0f)
					{
						return;
					}
					flag = elevation.m_Elevation > 0f;
				}
				PrefabRef prefabRef = m_PrefabRefData[objectEntity];
				Game.Objects.Transform transform = m_TransformData[objectEntity];
				if (!m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				m_ShouldOverride = (objectGeometryData.m_Flags & GeometryFlags.OverrideZone) != 0;
				flag &= (objectGeometryData.m_Flags & GeometryFlags.BaseCollision) == 0;
				m_OverrideZone = ZoneType.None;
				PlaceholderBuildingData componentData3;
				ZoneData componentData4;
				if (m_PrefabSpawnableBuildingData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					if (m_PrefabSignatureBuildingData.HasComponent(prefabRef.m_Prefab) && m_PrefabZoneData.TryGetComponent(componentData.m_ZonePrefab, out var componentData2) && m_PrefabData.IsComponentEnabled(componentData.m_ZonePrefab))
					{
						m_OverrideZone = componentData2.m_ZoneType;
					}
				}
				else if (m_PrefabPlaceholderBuildingData.TryGetComponent(prefabRef.m_Prefab, out componentData3) && m_PrefabZoneData.TryGetComponent(componentData3.m_ZonePrefab, out componentData4) && m_PrefabData.IsComponentEnabled(componentData3.m_ZonePrefab))
				{
					m_OverrideZone = componentData4.m_ZoneType;
				}
				if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
				{
					objectGeometryData.m_Bounds.min.y = math.max(objectGeometryData.m_Bounds.min.y, 0f);
				}
				if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount))
				{
					Bounds3 bounds3 = default(Bounds3);
					for (int i = 0; i < legCount; i++)
					{
						float3 standingLegPosition = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, i);
						if ((objectGeometryData.m_Flags & GeometryFlags.CircularLeg) != GeometryFlags.None)
						{
							Circle2 circle = new Circle2(math.max(objectGeometryData.m_LegSize - 0.16f, 0f).x * 0.5f, standingLegPosition.xz);
							Bounds3 bounds2 = bounds.m_Bounds;
							bounds2.xz = MathUtils.Bounds(circle);
							bounds2.min.y = standingLegPosition.y + objectGeometryData.m_Bounds.min.y;
							if (MathUtils.Intersect(m_Quad, circle))
							{
								CheckOverlapX(m_Bounds, bounds2, m_Quad, circle, m_Xxzz, flag);
							}
							continue;
						}
						bounds3.min = objectGeometryData.m_LegSize * -0.5f + 0.08f;
						bounds3.max = objectGeometryData.m_LegSize * 0.5f - 0.08f;
						float3 trueValue = MathUtils.Center(bounds3);
						bool3 test = bounds3.min > bounds3.max;
						bounds3.min = math.select(bounds3.min, trueValue, test);
						bounds3.max = math.select(bounds3.max, trueValue, test);
						Quad3 quad = ObjectUtils.CalculateBaseCorners(standingLegPosition, transform.m_Rotation, bounds3);
						bounds3 = MathUtils.Bounds(quad);
						bounds3.min.y += objectGeometryData.m_Bounds.min.y;
						if (MathUtils.Intersect(m_Quad, quad.xz))
						{
							CheckOverlapX(m_Bounds, bounds3, m_Quad, quad.xz, m_Xxzz, flag);
						}
					}
					transform.m_Position += math.rotate(transform.m_Rotation, new float3(0f, objectGeometryData.m_LegSize.y, 0f));
					objectGeometryData.m_Bounds.min.y = 0f;
					flag = true;
				}
				if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle2 = new Circle2(math.max(objectGeometryData.m_Size - 0.16f, 0f).x * 0.5f, transform.m_Position.xz);
					Bounds3 bounds4 = bounds.m_Bounds;
					bounds4.xz = MathUtils.Bounds(circle2);
					bounds4.min.y = transform.m_Position.y + objectGeometryData.m_Bounds.min.y;
					if (MathUtils.Intersect(m_Quad, circle2))
					{
						CheckOverlapX(m_Bounds, bounds4, m_Quad, circle2, m_Xxzz, flag);
					}
					return;
				}
				Bounds3 bounds5 = MathUtils.Expand(objectGeometryData.m_Bounds, -0.08f);
				float3 trueValue2 = MathUtils.Center(bounds5);
				bool3 test2 = bounds5.min > bounds5.max;
				bounds5.min = math.select(bounds5.min, trueValue2, test2);
				bounds5.max = math.select(bounds5.max, trueValue2, test2);
				Quad3 quad2 = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, bounds5);
				bounds5 = MathUtils.Bounds(quad2);
				bounds5.min.y += objectGeometryData.m_Bounds.min.y;
				if (MathUtils.Intersect(m_Quad, quad2.xz))
				{
					CheckOverlapX(m_Bounds, bounds5, m_Quad, quad2.xz, m_Xxzz, flag);
				}
			}

			private void CheckOverlapX(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, bool isElevated)
			{
				if (xxzz1.y - xxzz1.x >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.y = xxzz1.x + xxzz1.y >> 1;
					xxzz3.x = xxzz2.y;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz2.y - xxzz1.x) / (float)(xxzz1.y - xxzz1.x);
					quad3.b = math.lerp(quad1.a, quad1.b, t);
					quad3.c = math.lerp(quad1.d, quad1.c, t);
					quad4.a = quad3.b;
					quad4.d = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapZ(bounds3, bounds2, quad3, quad2, xxzz2, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapZ(bounds4, bounds2, quad4, quad2, xxzz3, isElevated);
					}
				}
				else
				{
					CheckOverlapZ(bounds1, bounds2, quad1, quad2, xxzz1, isElevated);
				}
			}

			private void CheckOverlapZ(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Quad2 quad2, int4 xxzz1, bool isElevated)
			{
				if (xxzz1.w - xxzz1.z >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.w = xxzz1.z + xxzz1.w >> 1;
					xxzz3.z = xxzz2.w;
					Quad2 quad3 = quad1;
					Quad2 quad4 = quad1;
					float t = (float)(xxzz2.w - xxzz1.z) / (float)(xxzz1.w - xxzz1.z);
					quad3.d = math.lerp(quad1.a, quad1.d, t);
					quad3.c = math.lerp(quad1.b, quad1.c, t);
					quad4.a = quad3.d;
					quad4.b = quad3.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad3);
					Bounds2 bounds4 = MathUtils.Bounds(quad4);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapX(bounds3, bounds2, quad3, quad2, xxzz2, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapX(bounds4, bounds2, quad4, quad2, xxzz3, isElevated);
					}
					return;
				}
				if (xxzz1.y - xxzz1.x >= 2)
				{
					CheckOverlapX(bounds1, bounds2, quad1, quad2, xxzz1, isElevated);
					return;
				}
				int index = xxzz1.z * m_BlockData.m_Size.x + xxzz1.x;
				Cell value = m_Cells[index];
				if ((value.m_State & CellFlags.Blocked) != CellFlags.None)
				{
					return;
				}
				quad1 = MathUtils.Expand(quad1, -0.01f);
				if (MathUtils.Intersect(quad1, quad2))
				{
					value.m_State |= CellFlags.Occupied;
					if (m_ShouldOverride && (!m_OverrideZone.Equals(value.m_Zone) || m_OverrideZone.Equals(ZoneType.None)))
					{
						value.m_State |= CellFlags.Overridden;
						value.m_Zone = m_OverrideZone;
					}
					m_Cells[index] = value;
				}
			}

			private void CheckOverlapX(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Circle2 circle2, int4 xxzz1, bool isElevated)
			{
				if (xxzz1.y - xxzz1.x >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.y = xxzz1.x + xxzz1.y >> 1;
					xxzz3.x = xxzz2.y;
					Quad2 quad2 = quad1;
					Quad2 quad3 = quad1;
					float t = (float)(xxzz2.y - xxzz1.x) / (float)(xxzz1.y - xxzz1.x);
					quad2.b = math.lerp(quad1.a, quad1.b, t);
					quad2.c = math.lerp(quad1.d, quad1.c, t);
					quad3.a = quad2.b;
					quad3.d = quad2.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad2);
					Bounds2 bounds4 = MathUtils.Bounds(quad3);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapZ(bounds3, bounds2, quad2, circle2, xxzz2, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapZ(bounds4, bounds2, quad3, circle2, xxzz3, isElevated);
					}
				}
				else
				{
					CheckOverlapZ(bounds1, bounds2, quad1, circle2, xxzz1, isElevated);
				}
			}

			private void CheckOverlapZ(Bounds2 bounds1, Bounds3 bounds2, Quad2 quad1, Circle2 circle2, int4 xxzz1, bool isElevated)
			{
				if (xxzz1.w - xxzz1.z >= 2)
				{
					int4 xxzz2 = xxzz1;
					int4 xxzz3 = xxzz1;
					xxzz2.w = xxzz1.z + xxzz1.w >> 1;
					xxzz3.z = xxzz2.w;
					Quad2 quad2 = quad1;
					Quad2 quad3 = quad1;
					float t = (float)(xxzz2.w - xxzz1.z) / (float)(xxzz1.w - xxzz1.z);
					quad2.d = math.lerp(quad1.a, quad1.d, t);
					quad2.c = math.lerp(quad1.b, quad1.c, t);
					quad3.a = quad2.d;
					quad3.b = quad2.c;
					Bounds2 bounds3 = MathUtils.Bounds(quad2);
					Bounds2 bounds4 = MathUtils.Bounds(quad3);
					if (MathUtils.Intersect(bounds3, bounds2.xz))
					{
						CheckOverlapX(bounds3, bounds2, quad2, circle2, xxzz2, isElevated);
					}
					if (MathUtils.Intersect(bounds4, bounds2.xz))
					{
						CheckOverlapX(bounds4, bounds2, quad3, circle2, xxzz3, isElevated);
					}
					return;
				}
				if (xxzz1.y - xxzz1.x >= 2)
				{
					CheckOverlapX(bounds1, bounds2, quad1, circle2, xxzz1, isElevated);
					return;
				}
				int index = xxzz1.z * m_BlockData.m_Size.x + xxzz1.x;
				Cell value = m_Cells[index];
				if ((value.m_State & CellFlags.Blocked) != CellFlags.None)
				{
					return;
				}
				quad1 = MathUtils.Expand(quad1, -0.01f);
				if (!MathUtils.Intersect(quad1, circle2))
				{
					return;
				}
				if (isElevated)
				{
					value.m_Height = (short)math.clamp(Mathf.FloorToInt(bounds2.min.y), -32768, math.min(value.m_Height, 32767));
				}
				else
				{
					value.m_State |= CellFlags.Occupied;
					if (m_ShouldOverride && (!m_OverrideZone.Equals(value.m_Zone) || m_OverrideZone.Equals(ZoneType.None)))
					{
						value.m_State |= CellFlags.Overridden;
						value.m_Zone = m_OverrideZone;
					}
				}
				m_Cells[index] = value;
			}
		}

		private struct DeletedBlockIterator
		{
			public Quad2 m_Quad;

			public Bounds2 m_Bounds;

			public Block m_BlockData;

			public ValidArea m_ValidAreaData;

			public DynamicBuffer<Cell> m_Cells;

			public ComponentLookup<Block> m_BlockDataFromEntity;

			public ComponentLookup<ValidArea> m_ValidAreaDataFromEntity;

			public BufferLookup<Cell> m_CellsFromEntity;

			private Block m_BlockData2;

			private ValidArea m_ValidAreaData2;

			private DynamicBuffer<Cell> m_Cells2;

			private NativeArray<float> m_BestDistance;

			public void Iterate(Entity blockEntity2)
			{
				m_ValidAreaData2 = m_ValidAreaDataFromEntity[blockEntity2];
				if (m_ValidAreaData2.m_Area.y <= m_ValidAreaData2.m_Area.x)
				{
					return;
				}
				m_BlockData2 = m_BlockDataFromEntity[blockEntity2];
				if (MathUtils.Intersect(m_Bounds, ZoneUtils.CalculateBounds(m_BlockData2)))
				{
					Quad2 quad = ZoneUtils.CalculateCorners(m_BlockData2, m_ValidAreaData2);
					if (MathUtils.Intersect(MathUtils.Expand(m_Quad, -0.01f), MathUtils.Expand(quad, -0.01f)))
					{
						m_Cells2 = m_CellsFromEntity[blockEntity2];
						CheckOverlapX1(m_Bounds, MathUtils.Bounds(quad), m_Quad, quad, m_ValidAreaData.m_Area, m_ValidAreaData2.m_Area);
					}
				}
			}

			public void Dispose()
			{
				if (m_BestDistance.IsCreated)
				{
					m_BestDistance.Dispose();
				}
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
				Cell value = m_Cells[index];
				Cell cell = m_Cells2[index2];
				if ((value.m_State & CellFlags.Blocked) != CellFlags.None || (cell.m_State & (CellFlags.Shared | CellFlags.Visible | CellFlags.Overridden)) != CellFlags.Visible || !value.m_Zone.Equals(ZoneType.None) || cell.m_Zone.Equals(ZoneType.None))
				{
					return;
				}
				float num = math.lengthsq(MathUtils.Center(quad1) - MathUtils.Center(quad2));
				if (num > 32f)
				{
					return;
				}
				if (m_BestDistance.IsCreated)
				{
					if (m_BestDistance[index] <= num)
					{
						return;
					}
				}
				else
				{
					m_BestDistance = new NativeArray<float>(m_BlockData.m_Size.x * m_BlockData.m_Size.y, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					for (int i = 0; i < m_BestDistance.Length; i++)
					{
						m_BestDistance[i] = float.MaxValue;
					}
				}
				value.m_Zone = cell.m_Zone;
				m_Cells[index] = value;
				m_BestDistance[index] = num;
			}
		}

		[ReadOnly]
		public NativeArray<CellCheckHelpers.SortedEntity> m_Blocks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DeletedBlockChunks;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<Block> m_BlockData;

		[ReadOnly]
		public ComponentLookup<ValidArea> m_ValidAreaData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> m_PrefabPlaceholderBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_PrefabZoneData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Cell> m_Cells;

		public void Execute(int index)
		{
			Entity entity = m_Blocks[index].m_Entity;
			Block block = m_BlockData[entity];
			ValidArea validArea = m_ValidAreaData[entity];
			DynamicBuffer<Cell> cells = m_Cells[entity];
			ClearOverrideStatus(block, cells);
			if (validArea.m_Area.y <= validArea.m_Area.x)
			{
				return;
			}
			Quad2 quad = ZoneUtils.CalculateCorners(block, validArea);
			Bounds2 bounds = ZoneUtils.CalculateBounds(block);
			DeletedBlockIterator deletedBlockIterator = new DeletedBlockIterator
			{
				m_Quad = quad,
				m_Bounds = bounds,
				m_BlockData = block,
				m_ValidAreaData = validArea,
				m_Cells = cells,
				m_BlockDataFromEntity = m_BlockData,
				m_ValidAreaDataFromEntity = m_ValidAreaData,
				m_CellsFromEntity = m_Cells
			};
			for (int i = 0; i < m_DeletedBlockChunks.Length; i++)
			{
				NativeArray<Entity> nativeArray = m_DeletedBlockChunks[i].GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					deletedBlockIterator.Iterate(nativeArray[j]);
				}
			}
			deletedBlockIterator.Dispose();
			ObjectIterator iterator = new ObjectIterator
			{
				m_BlockEntity = entity,
				m_BlockData = block,
				m_Bounds = bounds,
				m_Quad = quad,
				m_Xxzz = validArea.m_Area,
				m_Cells = cells,
				m_TransformData = m_TransformData,
				m_ElevationData = m_ElevationData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
				m_PrefabSpawnableBuildingData = m_PrefabSpawnableBuildingData,
				m_PrefabSignatureBuildingData = m_PrefabSignatureBuildingData,
				m_PrefabPlaceholderBuildingData = m_PrefabPlaceholderBuildingData,
				m_PrefabZoneData = m_PrefabZoneData,
				m_PrefabData = m_PrefabData
			};
			m_ObjectSearchTree.Iterate(ref iterator);
			SetOccupiedWithHeight(block, cells);
		}

		private static void ClearOverrideStatus(Block blockData, DynamicBuffer<Cell> cells)
		{
			for (int i = 0; i < blockData.m_Size.y; i++)
			{
				for (int j = 0; j < blockData.m_Size.x; j++)
				{
					int index = i * blockData.m_Size.x + j;
					Cell value = cells[index];
					if ((value.m_State & CellFlags.Overridden) != CellFlags.None)
					{
						value.m_State &= ~CellFlags.Overridden;
						value.m_Zone = ZoneType.None;
						cells[index] = value;
					}
				}
			}
		}

		private void SetOccupiedWithHeight(Block blockData, DynamicBuffer<Cell> cells)
		{
			for (int i = 0; i < blockData.m_Size.y; i++)
			{
				for (int j = 0; j < blockData.m_Size.x; j++)
				{
					int index = i * blockData.m_Size.x + j;
					Cell value = cells[index];
					if ((value.m_State & CellFlags.Occupied) == 0 && value.m_Height < short.MaxValue)
					{
						Entity entity = m_ZonePrefabs[value.m_Zone];
						ZoneData zoneData = m_PrefabZoneData[entity];
						if ((float)math.max((int)zoneData.m_MinOddHeight, (int)zoneData.m_MinEvenHeight) > (float)value.m_Height - blockData.m_Position.y)
						{
							value.m_State |= CellFlags.Occupied;
							cells[index] = value;
						}
					}
				}
			}
		}
	}
}
