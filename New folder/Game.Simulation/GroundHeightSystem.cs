using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GroundHeightSystem : GameSystemBase, IJobSerializable
{
	private enum LoadHeightsState
	{
		Loaded,
		Pending,
		Reading,
		Ready
	}

	[BurstCompile]
	internal struct SerializeJob<TWriter> : IJob where TWriter : struct, IWriter
	{
		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<Bounds2> m_NewUpdates;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<Bounds2> m_PendingUpdates;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<Bounds2> m_ReadingUpdates;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeList<Bounds2> m_ReadyUpdates;

		public EntityWriterData m_WriterData;

		public void Execute()
		{
			TWriter writer = m_WriterData.GetWriter<TWriter>();
			int num = 0;
			if (m_NewUpdates.IsCreated)
			{
				num += m_NewUpdates.Length;
			}
			if (m_PendingUpdates.IsCreated)
			{
				num += m_PendingUpdates.Length;
			}
			if (m_ReadingUpdates.IsCreated)
			{
				num += m_ReadingUpdates.Length;
			}
			if (m_ReadyUpdates.IsCreated)
			{
				num += m_ReadyUpdates.Length;
			}
			writer.Write(num);
			if (m_NewUpdates.IsCreated)
			{
				for (int i = 0; i < m_NewUpdates.Length; i++)
				{
					Bounds2 bounds = m_NewUpdates[i];
					writer.Write(bounds.min);
					writer.Write(bounds.max);
				}
			}
			if (m_PendingUpdates.IsCreated)
			{
				for (int j = 0; j < m_PendingUpdates.Length; j++)
				{
					Bounds2 bounds2 = m_PendingUpdates[j];
					writer.Write(bounds2.min);
					writer.Write(bounds2.max);
				}
			}
			if (m_ReadingUpdates.IsCreated)
			{
				for (int k = 0; k < m_ReadingUpdates.Length; k++)
				{
					Bounds2 bounds3 = m_ReadingUpdates[k];
					writer.Write(bounds3.min);
					writer.Write(bounds3.max);
				}
			}
			if (m_ReadyUpdates.IsCreated)
			{
				for (int l = 0; l < m_ReadyUpdates.Length; l++)
				{
					Bounds2 bounds4 = m_ReadyUpdates[l];
					writer.Write(bounds4.min);
					writer.Write(bounds4.max);
				}
			}
		}
	}

	[BurstCompile]
	internal struct DeserializeJob<TReader> : IJob where TReader : struct, IReader
	{
		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_NewUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_PendingUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_ReadingUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_ReadyUpdates;

		public EntityReaderData m_ReaderData;

		public void Execute()
		{
			if (m_NewUpdates.IsCreated)
			{
				m_NewUpdates.Clear();
			}
			if (m_PendingUpdates.IsCreated)
			{
				m_PendingUpdates.Clear();
			}
			if (m_ReadyUpdates.IsCreated)
			{
				m_ReadyUpdates.Clear();
			}
			TReader reader = m_ReaderData.GetReader<TReader>();
			reader.Read(out int value);
			m_ReadingUpdates.ResizeUninitialized(value);
			Bounds2 value2 = default(Bounds2);
			for (int i = 0; i < value; i++)
			{
				reader.Read(out value2.min);
				reader.Read(out value2.max);
				m_ReadingUpdates[i] = value2;
			}
		}
	}

	[BurstCompile]
	private struct SetDefaultsJob : IJob
	{
		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_NewUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_PendingUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_ReadingUpdates;

		[NativeDisableContainerSafetyRestriction]
		public NativeList<Bounds2> m_ReadyUpdates;

		public void Execute()
		{
			if (m_NewUpdates.IsCreated)
			{
				m_NewUpdates.Clear();
			}
			if (m_PendingUpdates.IsCreated)
			{
				m_PendingUpdates.Clear();
			}
			if (m_ReadingUpdates.IsCreated)
			{
				m_ReadingUpdates.Clear();
			}
			if (m_ReadyUpdates.IsCreated)
			{
				m_ReadyUpdates.Clear();
			}
		}
	}

	[BurstCompile]
	private struct BoundsFindJob : IJobParallelFor
	{
		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public NativeQueue<Entity>.ParallelWriter m_Queue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					if ((componentData.m_Flags & Game.Objects.GeometryFlags.HasBase) != Game.Objects.GeometryFlags.None)
					{
						goto IL_007f;
					}
					if ((componentData.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) != Game.Objects.GeometryFlags.None || (componentData.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Marker | Game.Objects.GeometryFlags.Brushable)) == 0)
					{
						return;
					}
				}
				if (m_ElevationData.TryGetComponent(entity, out var componentData2) && (componentData2.m_Flags & ElevationFlags.OnGround) == 0)
				{
					return;
				}
				goto IL_007f;
				IL_007f:
				m_Queue.Enqueue(entity);
			}
		}

		private struct LaneIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Objects.Elevation> m_ObjectElevationData;

			public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public NativeQueue<Entity>.ParallelWriter m_Queue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					return;
				}
				if (m_OwnerData.TryGetComponent(entity, out var componentData))
				{
					PrefabRef prefabRef = m_PrefabRefData[componentData.m_Owner];
					if ((m_ObjectElevationData.TryGetComponent(componentData.m_Owner, out var componentData2) && (componentData2.m_Flags & ElevationFlags.OnGround) == 0) || !m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData3) || ((componentData3.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) == 0 && (componentData3.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Marker | Game.Objects.GeometryFlags.Brushable)) != Game.Objects.GeometryFlags.None))
					{
						return;
					}
				}
				if (!m_NetElevationData.TryGetComponent(entity, out var componentData4) || !math.all(componentData4.m_Elevation != float.MinValue))
				{
					m_Queue.Enqueue(entity);
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Buildings.Lot> m_BuildingLotData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Game.Net.Elevation> m_ElevationData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

			public NativeQueue<Entity>.ParallelWriter m_Queue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || (m_ElevationData.HasComponent(entity) && (!m_EdgeData.TryGetComponent(entity, out var componentData) || (m_ElevationData.HasComponent(componentData.m_Start) && m_ElevationData.HasComponent(componentData.m_End)))))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_PrefabNetGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					if ((componentData2.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
					{
						return;
					}
					if ((componentData2.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0)
					{
						bool flag = false;
						Entity entity2 = entity;
						while (m_OwnerData.HasComponent(entity2))
						{
							entity2 = m_OwnerData[entity2].m_Owner;
							if (m_BuildingLotData.HasComponent(entity2))
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							return;
						}
					}
				}
				m_Queue.Enqueue(entity);
			}
		}

		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public BufferLookup<Game.Areas.Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public NativeQueue<Entity>.ParallelWriter m_Queue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && math.any(AreaUtils.GetElevations(m_Nodes[item.m_Area], m_Triangles[item.m_Area][item.m_Triangle]) == float.MinValue))
				{
					m_Queue.Enqueue(item.m_Area);
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ObjectElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> m_BuildingLotData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public NativeList<Bounds2> m_ReadyUpdates;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_LaneSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public NativeQueue<Entity>.ParallelWriter m_Queue;

		public void Execute(int index)
		{
			Bounds2 bounds = m_ReadyUpdates[index];
			ObjectIterator iterator = new ObjectIterator
			{
				m_Bounds = bounds,
				m_ElevationData = m_ObjectElevationData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
				m_Queue = m_Queue
			};
			m_StaticObjectSearchTree.Iterate(ref iterator);
			LaneIterator iterator2 = new LaneIterator
			{
				m_Bounds = bounds,
				m_OwnerData = m_OwnerData,
				m_ObjectElevationData = iterator.m_ElevationData,
				m_NetElevationData = m_NetElevationData,
				m_PrefabRefData = iterator.m_PrefabRefData,
				m_PrefabObjectGeometryData = iterator.m_PrefabObjectGeometryData,
				m_Queue = m_Queue
			};
			m_LaneSearchTree.Iterate(ref iterator2);
			NetIterator iterator3 = new NetIterator
			{
				m_Bounds = bounds,
				m_EdgeData = m_EdgeData,
				m_OwnerData = iterator2.m_OwnerData,
				m_BuildingLotData = m_BuildingLotData,
				m_ElevationData = iterator2.m_NetElevationData,
				m_PrefabRefData = iterator2.m_PrefabRefData,
				m_PrefabNetGeometryData = m_PrefabNetGeometryData,
				m_Queue = m_Queue
			};
			m_NetSearchTree.Iterate(ref iterator3);
			AreaIterator iterator4 = new AreaIterator
			{
				m_Bounds = bounds,
				m_Nodes = m_AreaNodes,
				m_Triangles = m_Triangles,
				m_Queue = m_Queue
			};
			m_AreaSearchTree.Iterate(ref iterator4);
			m_OwnerData = iterator3.m_OwnerData;
			m_ObjectElevationData = iterator2.m_ObjectElevationData;
			m_BuildingLotData = iterator3.m_BuildingLotData;
			m_EdgeData = iterator3.m_EdgeData;
			m_NetElevationData = iterator3.m_ElevationData;
			m_PrefabRefData = iterator3.m_PrefabRefData;
			m_PrefabObjectGeometryData = iterator.m_PrefabObjectGeometryData;
			m_PrefabNetGeometryData = iterator3.m_PrefabNetGeometryData;
			m_AreaNodes = iterator4.m_Nodes;
			m_Triangles = iterator4.m_Triangles;
		}
	}

	[BurstCompile]
	private struct DequeueJob : IJob
	{
		public NativeQueue<Entity> m_Queue;

		public NativeList<Entity> m_List;

		public NativeList<Bounds2> m_ReadyUpdates;

		public void Execute()
		{
			NativeArray<Entity> array = m_Queue.ToArray(Allocator.Temp);
			m_List.Capacity = array.Length;
			array.Sort();
			Entity entity = Entity.Null;
			for (int i = 0; i < array.Length; i++)
			{
				Entity value = array[i];
				if (value != entity)
				{
					m_List.Add(in value);
					entity = value;
				}
			}
			m_ReadyUpdates.Clear();
			array.Dispose();
		}
	}

	[BurstCompile]
	private struct UpdateHeightsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ObjectElevationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<MovedLocation> m_MovedLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> m_BuildingLotData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Curve> m_CurveData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeList<Entity> m_List;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_List[index];
			Curve componentData5;
			Game.Net.Node componentData9;
			if (m_TransformData.TryGetComponent(entity, out var componentData))
			{
				bool flag = true;
				bool flag2 = false;
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_ObjectElevationData.TryGetComponent(entity, out var componentData2))
				{
					flag = (componentData2.m_Flags & ElevationFlags.OnGround) != 0;
				}
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					flag2 = (componentData3.m_Flags & Game.Objects.GeometryFlags.HasBase) != 0;
					flag &= (componentData3.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) == 0 && (componentData3.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.Marker | Game.Objects.GeometryFlags.Brushable)) != 0;
				}
				if (flag)
				{
					bool angledSample;
					Transform transform = ObjectUtils.AdjustPosition(componentData, ref componentData2, prefabRef.m_Prefab, out angledSample, ref m_TerrainHeightData, ref m_WaterSurfaceData, ref m_PrefabPlaceableObjectData, ref m_PrefabObjectGeometryData);
					if (math.abs(transform.m_Position.y - componentData.m_Position.y) >= 0.01f || (angledSample && MathUtils.RotationAngle(transform.m_Rotation, componentData.m_Rotation) >= math.radians(0.1f)))
					{
						m_TransformData[entity] = transform;
						m_CommandBuffer.AddComponent(index, entity, default(Updated));
						flag2 = false;
						if (m_SpawnLocationData.HasComponent(entity) && !m_MovedLocationData.HasComponent(entity))
						{
							m_CommandBuffer.AddComponent(index, entity, new MovedLocation
							{
								m_OldPosition = componentData.m_Position
							});
						}
						if (m_SubObjects.TryGetBuffer(entity, out var bufferData) && (m_OwnerData.HasComponent(entity) || m_EditorMode))
						{
							HandleSubObjects(index, bufferData, componentData, transform);
						}
					}
				}
				if (flag2 && m_CullingInfoData.TryGetComponent(entity, out var componentData4))
				{
					float min = TerrainUtils.GetHeightRange(ref m_TerrainHeightData, componentData4.m_Bounds).min;
					if (min < componentData4.m_Bounds.min.y)
					{
						componentData4.m_Bounds.min.y = min;
						m_CullingInfoData[entity] = componentData4;
						m_CommandBuffer.AddComponent(index, entity, default(BatchesUpdated));
					}
				}
			}
			else if (m_CurveData.TryGetComponent(entity, out componentData5))
			{
				if (m_EdgeData.TryGetComponent(entity, out var componentData6))
				{
					bool flag3 = m_NetElevationData.HasComponent(componentData6.m_Start);
					bool flag4 = m_NetElevationData.HasComponent(componentData6.m_End);
					bool flag5 = false;
					bool flag6 = false;
					PrefabRef prefabRef2 = m_PrefabRefData[entity];
					if (m_PrefabNetGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData7))
					{
						flag5 = (componentData7.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0;
					}
					bool flag7 = false;
					bool flag8 = false;
					if (GetLotOwner(entity, out var lotOwner))
					{
						flag7 = IsFixedNode(componentData6.m_Start, entity, lotOwner);
						flag8 = IsFixedNode(componentData6.m_End, entity, lotOwner);
						flag3 = flag3 || flag7;
						flag4 = flag4 || flag8;
					}
					else
					{
						flag6 = flag5;
					}
					if (flag6)
					{
						return;
					}
					bool linearMiddle = flag3 || flag4 || m_NetElevationData.HasComponent(entity);
					BuildingUtils.LotInfo lotInfo;
					Curve value = ((!flag5) ? NetUtils.AdjustPosition(componentData5, flag3, linearMiddle, flag4, ref m_TerrainHeightData) : ((!GetOwnerLot(lotOwner, out lotInfo)) ? componentData5 : NetUtils.AdjustPosition(componentData5, new bool2(flag3, flag7), linearMiddle, new bool2(flag4, flag8), ref lotInfo)));
					bool4 x = math.abs(value.m_Bezier.y.abcd - componentData5.m_Bezier.y.abcd) >= 0.01f;
					if (!math.any(x))
					{
						return;
					}
					m_CurveData[entity] = value;
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
					m_CommandBuffer.AddComponent(index, componentData6.m_Start, default(Updated));
					m_CommandBuffer.AddComponent(index, componentData6.m_End, default(Updated));
					if (x.x)
					{
						DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[componentData6.m_Start];
						for (int i = 0; i < dynamicBuffer.Length; i++)
						{
							Entity edge = dynamicBuffer[i].m_Edge;
							if (edge != entity)
							{
								Edge edge2 = m_EdgeData[edge];
								m_CommandBuffer.AddComponent(index, edge, default(Updated));
								if (edge2.m_Start != componentData6.m_Start)
								{
									m_CommandBuffer.AddComponent(index, edge2.m_Start, default(Updated));
								}
								if (edge2.m_End != componentData6.m_Start)
								{
									m_CommandBuffer.AddComponent(index, edge2.m_End, default(Updated));
								}
							}
						}
					}
					if (!x.w)
					{
						return;
					}
					DynamicBuffer<ConnectedEdge> dynamicBuffer2 = m_ConnectedEdges[componentData6.m_End];
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Entity edge3 = dynamicBuffer2[j].m_Edge;
						if (edge3 != entity)
						{
							Edge edge4 = m_EdgeData[edge3];
							m_CommandBuffer.AddComponent(index, edge3, default(Updated));
							if (edge4.m_Start != componentData6.m_End)
							{
								m_CommandBuffer.AddComponent(index, edge4.m_Start, default(Updated));
							}
							if (edge4.m_End != componentData6.m_End)
							{
								m_CommandBuffer.AddComponent(index, edge4.m_End, default(Updated));
							}
						}
					}
				}
				else
				{
					bool2 x2 = false;
					if (m_NetElevationData.TryGetComponent(entity, out var componentData8))
					{
						x2 = componentData8.m_Elevation != float.MinValue;
					}
					Curve value2 = NetUtils.AdjustPosition(componentData5, x2.x, math.any(x2), x2.y, ref m_TerrainHeightData);
					if (math.any(math.abs(value2.m_Bezier.y.abcd - componentData5.m_Bezier.y.abcd) >= 0.01f))
					{
						m_CurveData[entity] = value2;
						m_CommandBuffer.AddComponent(index, entity, default(Updated));
					}
				}
			}
			else if (m_NodeData.TryGetComponent(entity, out componentData9))
			{
				bool flag9 = false;
				bool flag10 = false;
				PrefabRef prefabRef3 = m_PrefabRefData[entity];
				if (m_PrefabNetGeometryData.TryGetComponent(prefabRef3.m_Prefab, out var componentData10))
				{
					flag9 = (componentData10.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0;
				}
				if (!((!GetLotOwner(entity, out var lotOwner2)) ? flag9 : IsFixedNode(entity, Entity.Null, lotOwner2)))
				{
					BuildingUtils.LotInfo lotInfo2;
					Game.Net.Node value3 = ((!flag9) ? NetUtils.AdjustPosition(componentData9, ref m_TerrainHeightData) : ((!GetOwnerLot(lotOwner2, out lotInfo2)) ? componentData9 : NetUtils.AdjustPosition(componentData9, ref lotInfo2)));
					if (math.abs(value3.m_Position.y - componentData9.m_Position.y) >= 0.01f)
					{
						m_NodeData[entity] = value3;
						m_CommandBuffer.AddComponent(index, entity, default(Updated));
					}
				}
			}
			else
			{
				if (!m_AreaNodes.TryGetBuffer(entity, out var bufferData2))
				{
					return;
				}
				bool flag11 = false;
				bool flag12 = false;
				PrefabRef prefabRef4 = m_PrefabRefData[entity];
				if (m_PrefabAreaGeometryData.TryGetComponent(prefabRef4.m_Prefab, out var componentData11))
				{
					flag12 = (componentData11.m_Flags & Game.Areas.GeometryFlags.OnWaterSurface) != 0;
					if (flag12 && (componentData11.m_Flags & Game.Areas.GeometryFlags.ShiftTerrain) != 0)
					{
						return;
					}
				}
				for (int k = 0; k < bufferData2.Length; k++)
				{
					ref Game.Areas.Node reference = ref bufferData2.ElementAt(k);
					if (reference.m_Elevation == float.MinValue)
					{
						Game.Areas.Node node = ((!flag12) ? AreaUtils.AdjustPosition(reference, ref m_TerrainHeightData) : AreaUtils.AdjustPosition(reference, ref m_TerrainHeightData, ref m_WaterSurfaceData));
						bool flag13 = math.abs(node.m_Position.y - reference.m_Position.y) >= 0.01f;
						reference.m_Position = math.select(reference.m_Position, node.m_Position, flag13);
						flag11 = flag11 || flag13;
					}
				}
				if (flag11)
				{
					m_CommandBuffer.AddComponent(index, entity, default(Updated));
				}
			}
		}

		private bool GetLotOwner(Entity entity, out Entity lotOwner)
		{
			Entity entity2 = entity;
			while (m_OwnerData.HasComponent(entity2))
			{
				entity2 = m_OwnerData[entity2].m_Owner;
				if (m_BuildingLotData.HasComponent(entity2) && (!m_PrefabRefData.TryGetComponent(entity2, out var componentData) || !m_PrefabBuildingExtensionData.TryGetComponent(componentData.m_Prefab, out var componentData2) || componentData2.m_External))
				{
					lotOwner = entity2;
					return true;
				}
			}
			lotOwner = Entity.Null;
			return false;
		}

		private bool GetOwnerLot(Entity lotOwner, out BuildingUtils.LotInfo lotInfo)
		{
			Game.Buildings.Lot lot = m_BuildingLotData[lotOwner];
			if (m_TransformData.TryGetComponent(lotOwner, out var componentData) && m_PrefabRefData.TryGetComponent(lotOwner, out var componentData2))
			{
				bool hasExtensionLots;
				if (m_PrefabBuildingData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
				{
					float2 extents = new float2(componentData3.m_LotSize) * 4f;
					m_ObjectElevationData.TryGetComponent(lotOwner, out var componentData4);
					m_InstalledUpgrades.TryGetBuffer(lotOwner, out var bufferData);
					lotInfo = BuildingUtils.CalculateLotInfo(extents, componentData, componentData4, lot, componentData2, bufferData, m_TransformData, m_PrefabRefData, m_PrefabObjectGeometryData, m_PrefabBuildingTerraformData, m_PrefabBuildingExtensionData, defaultNoSmooth: false, out hasExtensionLots);
					return true;
				}
				if (m_PrefabBuildingExtensionData.TryGetComponent(componentData2.m_Prefab, out var componentData5))
				{
					float2 extents2 = new float2(componentData5.m_LotSize) * 4f;
					m_ObjectElevationData.TryGetComponent(lotOwner, out var componentData6);
					lotInfo = BuildingUtils.CalculateLotInfo(extents2, componentData, componentData6, lot, componentData2, default(DynamicBuffer<InstalledUpgrade>), m_TransformData, m_PrefabRefData, m_PrefabObjectGeometryData, m_PrefabBuildingTerraformData, m_PrefabBuildingExtensionData, defaultNoSmooth: false, out hasExtensionLots);
					return true;
				}
			}
			lotInfo = default(BuildingUtils.LotInfo);
			return false;
		}

		private bool IsFixedNode(Entity node, Entity ignoreEdge, Entity lotOwner)
		{
			if (m_ConnectedEdges.TryGetBuffer(node, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity edge = bufferData[i].m_Edge;
					if (edge == ignoreEdge)
					{
						continue;
					}
					Edge edge2 = m_EdgeData[edge];
					if (!(edge2.m_Start != node) || !(edge2.m_End != node))
					{
						PrefabRef prefabRef = m_PrefabRefData[edge];
						if (m_PrefabNetGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0 && (!GetLotOwner(edge, out var lotOwner2) || lotOwner2 != lotOwner))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private void HandleSubObjects(int jobIndex, DynamicBuffer<Game.Objects.SubObject> subObjects, Transform transform, Transform adjusted)
		{
			if (subObjects.Length == 0)
			{
				return;
			}
			quaternion quaternion = math.mul(adjusted.m_Rotation, math.inverse(transform.m_Rotation));
			Transform transform3 = default(Transform);
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_ObjectElevationData.TryGetComponent(subObject, out var componentData) && (componentData.m_Flags & ElevationFlags.OnGround) == 0)
				{
					Transform transform2 = m_TransformData[subObject];
					transform3.m_Position = adjusted.m_Position + math.mul(quaternion, transform2.m_Position - transform.m_Position);
					transform3.m_Rotation = math.normalize(math.mul(quaternion, transform2.m_Rotation));
					m_TransformData[subObject] = transform3;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(Updated));
					if (m_SpawnLocationData.HasComponent(subObject) && !m_MovedLocationData.HasComponent(subObject))
					{
						m_CommandBuffer.AddComponent(jobIndex, subObject, new MovedLocation
						{
							m_OldPosition = transform.m_Position
						});
					}
					if (m_SubObjects.TryGetBuffer(subObject, out var bufferData))
					{
						HandleSubObjects(jobIndex, bufferData, transform2, transform3);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovedLocation> __Game_Objects_MovedLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RW_ComponentLookup;

		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RW_ComponentLookup;

		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Buildings_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Lot>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_MovedLocation_RO_ComponentLookup = state.GetComponentLookup<MovedLocation>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>();
			__Game_Net_Node_RW_ComponentLookup = state.GetComponentLookup<Game.Net.Node>();
			__Game_Rendering_CullingInfo_RW_ComponentLookup = state.GetComponentLookup<CullingInfo>();
			__Game_Areas_Node_RW_BufferLookup = state.GetBufferLookup<Game.Areas.Node>();
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ModificationBarrier2 m_ModificationBarrier;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private ToolSystem m_ToolSystem;

	private Game.Areas.GeometrySystem m_AreaGeometrySystem;

	private NativeList<Bounds2> m_NewUpdates;

	private NativeList<Bounds2> m_PendingUpdates;

	private NativeList<Bounds2> m_ReadingUpdates;

	private NativeList<Bounds2> m_ReadyUpdates;

	private JobHandle m_UpdateDeps;

	private LoadHeightsState m_LoadHeightsState;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaGeometrySystem = base.World.GetOrCreateSystemManaged<Game.Areas.GeometrySystem>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_UpdateDeps.Complete();
		if (m_NewUpdates.IsCreated)
		{
			m_NewUpdates.Dispose();
		}
		if (m_PendingUpdates.IsCreated)
		{
			m_PendingUpdates.Dispose();
		}
		if (m_ReadingUpdates.IsCreated)
		{
			m_ReadingUpdates.Dispose();
		}
		if (m_ReadyUpdates.IsCreated)
		{
			m_ReadyUpdates.Dispose();
		}
		base.OnDestroy();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_LoadHeightsState = LoadHeightsState.Loaded;
	}

	public NativeList<Bounds2> GetUpdateBuffer()
	{
		m_UpdateDeps.Complete();
		if (!m_NewUpdates.IsCreated)
		{
			if (m_ReadyUpdates.IsCreated && m_ReadyUpdates.Length == 0)
			{
				m_NewUpdates = m_ReadyUpdates;
				m_ReadyUpdates = default(NativeList<Bounds2>);
			}
			else
			{
				m_NewUpdates = new NativeList<Bounds2>(Allocator.Persistent);
			}
		}
		return m_NewUpdates;
	}

	public void BeforeUpdateHeights()
	{
		m_UpdateDeps.Complete();
		if (m_NewUpdates.IsCreated && m_NewUpdates.Length != 0)
		{
			if (m_PendingUpdates.IsCreated)
			{
				m_PendingUpdates.AddRange(m_NewUpdates.AsArray());
				m_NewUpdates.Clear();
			}
			else
			{
				m_PendingUpdates = m_NewUpdates;
				m_NewUpdates = default(NativeList<Bounds2>);
			}
		}
		if (m_LoadHeightsState == LoadHeightsState.Loaded)
		{
			m_LoadHeightsState = LoadHeightsState.Pending;
		}
	}

	public void BeforeReadHeights()
	{
		m_UpdateDeps.Complete();
		if (m_PendingUpdates.IsCreated && m_PendingUpdates.Length != 0)
		{
			if (m_ReadingUpdates.IsCreated)
			{
				m_ReadingUpdates.AddRange(m_PendingUpdates.AsArray());
				m_PendingUpdates.Clear();
			}
			else
			{
				m_ReadingUpdates = m_PendingUpdates;
				m_PendingUpdates = default(NativeList<Bounds2>);
			}
		}
		if (m_LoadHeightsState == LoadHeightsState.Pending)
		{
			m_LoadHeightsState = LoadHeightsState.Reading;
		}
	}

	public void AfterReadHeights()
	{
		m_UpdateDeps.Complete();
		if (m_ReadingUpdates.IsCreated && m_ReadingUpdates.Length != 0)
		{
			if (m_ReadyUpdates.IsCreated)
			{
				m_ReadyUpdates.AddRange(m_ReadingUpdates.AsArray());
				m_ReadingUpdates.Clear();
			}
			else
			{
				m_ReadyUpdates = m_ReadingUpdates;
				m_ReadingUpdates = default(NativeList<Bounds2>);
			}
		}
		if (m_LoadHeightsState == LoadHeightsState.Reading)
		{
			m_LoadHeightsState = LoadHeightsState.Ready;
			m_AreaGeometrySystem.TerrainHeightsReadyAfterLoading();
			m_TerrainSystem.TerrainHeightsReadyAfterLoading();
		}
	}

	public JobHandle Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps) where TWriter : struct, IWriter
	{
		SerializeJob<TWriter> jobData = new SerializeJob<TWriter>
		{
			m_NewUpdates = m_NewUpdates,
			m_PendingUpdates = m_PendingUpdates,
			m_ReadingUpdates = m_ReadingUpdates,
			m_ReadyUpdates = m_ReadyUpdates,
			m_WriterData = writerData
		};
		m_UpdateDeps = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, m_UpdateDeps));
		return m_UpdateDeps;
	}

	public JobHandle Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps) where TReader : struct, IReader
	{
		if (!m_ReadingUpdates.IsCreated)
		{
			m_ReadingUpdates = new NativeList<Bounds2>(Allocator.Persistent);
		}
		DeserializeJob<TReader> jobData = new DeserializeJob<TReader>
		{
			m_NewUpdates = m_NewUpdates,
			m_PendingUpdates = m_PendingUpdates,
			m_ReadingUpdates = m_ReadingUpdates,
			m_ReadyUpdates = m_ReadyUpdates,
			m_ReaderData = readerData
		};
		m_UpdateDeps = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(inputDeps, m_UpdateDeps));
		return m_UpdateDeps;
	}

	public JobHandle SetDefaults(Context context)
	{
		SetDefaultsJob jobData = new SetDefaultsJob
		{
			m_NewUpdates = m_NewUpdates,
			m_PendingUpdates = m_PendingUpdates,
			m_ReadingUpdates = m_ReadingUpdates,
			m_ReadyUpdates = m_ReadyUpdates
		};
		m_UpdateDeps = IJobExtensions.Schedule(jobData, m_UpdateDeps);
		return m_UpdateDeps;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_UpdateDeps.Complete();
		if (m_ReadyUpdates.IsCreated && m_ReadyUpdates.Length != 0)
		{
			NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);
			NativeList<Entity> list = new NativeList<Entity>(Allocator.TempJob);
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle dependencies3;
			JobHandle dependencies4;
			BoundsFindJob jobData = new BoundsFindJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_ReadyUpdates = m_ReadyUpdates,
				m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
				m_LaneSearchTree = m_NetSearchSystem.GetLaneSearchTree(readOnly: true, out dependencies2),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies3),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies4),
				m_Queue = queue.AsParallelWriter()
			};
			DequeueJob jobData2 = new DequeueJob
			{
				m_Queue = queue,
				m_List = list,
				m_ReadyUpdates = m_ReadyUpdates
			};
			JobHandle deps;
			UpdateHeightsJob jobData3 = new UpdateHeightsJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovedLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_MovedLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingTerraformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CullingInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RW_BufferLookup, ref base.CheckedStateRef),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_List = list,
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle = IJobParallelForExtensions.Schedule(jobData, m_ReadyUpdates.Length, 1, JobHandle.CombineDependencies(base.Dependency, dependencies4, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3)));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			JobHandle jobHandle3 = jobData3.Schedule(list, 4, JobHandle.CombineDependencies(jobHandle2, deps));
			queue.Dispose(jobHandle2);
			list.Dispose(jobHandle3);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
			m_NetSearchSystem.AddLaneSearchTreeReader(jobHandle);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
			m_TerrainSystem.AddCPUHeightReader(jobHandle3);
			m_WaterSystem.AddSurfaceReader(jobHandle3);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
			base.Dependency = jobHandle3;
			m_UpdateDeps = jobHandle2;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public GroundHeightSystem()
	{
	}
}
