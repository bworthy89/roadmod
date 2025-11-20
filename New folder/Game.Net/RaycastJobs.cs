using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Net;

public static class RaycastJobs
{
	[BurstCompile]
	public struct RaycastEdgesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public float m_FovTan;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeArray<RaycastSystem.EntityResult> m_Edges;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastSystem.EntityResult entityResult = m_Edges[index];
			RaycastInput input = m_Input[entityResult.m_RaycastIndex];
			if ((input.m_TypeMask & TypeMask.Net) != TypeMask.None)
			{
				if (m_EdgeData.HasComponent(entityResult.m_Entity))
				{
					CheckEdge(entityResult.m_Entity, entityResult.m_RaycastIndex, input);
				}
				else if (m_NodeData.HasComponent(entityResult.m_Entity))
				{
					CheckNode(entityResult.m_Entity, entityResult.m_RaycastIndex, input);
				}
			}
		}

		private void CheckNode(Entity entity, int raycastIndex, RaycastInput input)
		{
			if (m_NodeGeometryData.HasComponent(entity))
			{
				if (!MathUtils.Intersect(m_NodeGeometryData[entity].m_Bounds, input.m_Line, out var _) || !m_OrphanData.HasComponent(entity))
				{
					return;
				}
				Node node = m_NodeData[entity];
				Orphan orphan = m_OrphanData[entity];
				NetCompositionData compositionData = m_PrefabCompositionData[orphan.m_Composition];
				if ((compositionData.m_State & CompositionState.Marker) != 0)
				{
					if ((input.m_Flags & RaycastFlags.Markers) == 0)
					{
						return;
					}
				}
				else if ((NetUtils.GetCollisionMask(compositionData, ignoreMarkers: true) & input.m_CollisionMask) == 0)
				{
					return;
				}
				float3 position = node.m_Position;
				if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
				{
					float maxElevation = 0f - input.m_Offset.y - compositionData.m_SurfaceHeight.max;
					SetElevationOffset(ref position, entity, maxElevation);
				}
				Line3.Segment line = input.m_Line + input.m_Offset;
				if ((input.m_Flags & RaycastFlags.ElevateOffset) == 0)
				{
					position.y += compositionData.m_SurfaceHeight.max;
				}
				if (!MathUtils.Intersect(line.y, position.y, out var t2))
				{
					return;
				}
				float3 hitPosition = MathUtils.Position(line, t2);
				if (math.distance(hitPosition.xz, position.xz) <= compositionData.m_Width * 0.5f)
				{
					RaycastResult result = new RaycastResult
					{
						m_Owner = entity,
						m_Hit = 
						{
							m_HitEntity = entity,
							m_Position = node.m_Position,
							m_HitPosition = hitPosition,
							m_NormalizedDistance = t2 + 0.5f / math.max(1f, MathUtils.Length(line))
						}
					};
					if (ValidateResult(input, ref result))
					{
						m_Results.Accumulate(raycastIndex, result);
					}
				}
			}
			else
			{
				if ((input.m_Flags & RaycastFlags.Markers) == 0)
				{
					return;
				}
				Node node2 = m_NodeData[entity];
				float3 position2 = node2.m_Position;
				if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
				{
					float maxElevation2 = 0f - input.m_Offset.y;
					SetElevationOffset(ref position2, entity, maxElevation2);
				}
				Line3.Segment line2 = input.m_Line + input.m_Offset;
				float t3;
				float num = MathUtils.Distance(line2, position2, out t3);
				if (num < 1f)
				{
					RaycastResult result2 = new RaycastResult
					{
						m_Owner = entity,
						m_Hit = 
						{
							m_HitEntity = entity,
							m_Position = node2.m_Position,
							m_HitPosition = MathUtils.Position(line2, t3),
							m_NormalizedDistance = t3 - (1f - num) / math.max(1f, MathUtils.Length(line2))
						}
					};
					if (ValidateResult(input, ref result2))
					{
						m_Results.Accumulate(raycastIndex, result2);
					}
				}
			}
		}

		private void CheckEdge(Entity entity, int raycastIndex, RaycastInput input)
		{
			if (m_EdgeGeometryData.HasComponent(entity))
			{
				EdgeGeometry geometry = m_EdgeGeometryData[entity];
				EdgeNodeGeometry geometry2 = m_StartNodeGeometryData[entity].m_Geometry;
				EdgeNodeGeometry geometry3 = m_EndNodeGeometryData[entity].m_Geometry;
				geometry.m_Bounds |= geometry.m_Bounds - input.m_Offset;
				geometry2.m_Bounds |= geometry2.m_Bounds - input.m_Offset;
				geometry3.m_Bounds |= geometry3.m_Bounds - input.m_Offset;
				bool3 x = default(bool3);
				x.x = MathUtils.Intersect(geometry.m_Bounds, input.m_Line, out var t);
				x.y = MathUtils.Intersect(geometry2.m_Bounds, input.m_Line, out t);
				x.z = MathUtils.Intersect(geometry3.m_Bounds, input.m_Line, out t);
				if (!math.any(x))
				{
					return;
				}
				Composition composition = m_CompositionData[entity];
				Edge edge = m_EdgeData[entity];
				Curve curve = m_CurveData[entity];
				if (x.x)
				{
					NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
					if (((netCompositionData.m_State & CompositionState.Marker) == 0) ? ((byte)(NetUtils.GetCollisionMask(netCompositionData, ignoreMarkers: true) & input.m_CollisionMask) != 0) : ((byte)(input.m_Flags & RaycastFlags.Markers) != 0))
					{
						if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
						{
							float maxElevation = 0f - input.m_Offset.y - netCompositionData.m_SurfaceHeight.max;
							SetElevationOffset(ref geometry, entity, edge.m_Start, edge.m_End, maxElevation);
						}
						CheckSegment(input, raycastIndex, entity, entity, geometry.m_Start, curve.m_Bezier, netCompositionData);
						CheckSegment(input, raycastIndex, entity, entity, geometry.m_End, curve.m_Bezier, netCompositionData);
					}
				}
				if (x.y)
				{
					NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
					if (((netCompositionData2.m_State & CompositionState.Marker) == 0) ? ((byte)(NetUtils.GetCollisionMask(netCompositionData2, ignoreMarkers: true) & input.m_CollisionMask) != 0) : ((byte)(input.m_Flags & RaycastFlags.Markers) != 0))
					{
						if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
						{
							float maxElevation2 = 0f - input.m_Offset.y - netCompositionData2.m_SurfaceHeight.max;
							SetElevationOffset(ref geometry2, edge.m_Start, maxElevation2);
						}
						if (geometry2.m_MiddleRadius > 0f)
						{
							CheckSegment(input, raycastIndex, edge.m_Start, entity, geometry2.m_Left, curve.m_Bezier, netCompositionData2);
							Segment right = geometry2.m_Right;
							Segment right2 = geometry2.m_Right;
							right.m_Right = MathUtils.Lerp(geometry2.m_Right.m_Left, geometry2.m_Right.m_Right, 0.5f);
							right2.m_Left = MathUtils.Lerp(geometry2.m_Right.m_Left, geometry2.m_Right.m_Right, 0.5f);
							right.m_Right.d = geometry2.m_Middle.d;
							right2.m_Left.d = geometry2.m_Middle.d;
							CheckSegment(input, raycastIndex, edge.m_Start, entity, right, curve.m_Bezier, netCompositionData2);
							CheckSegment(input, raycastIndex, edge.m_Start, entity, right2, curve.m_Bezier, netCompositionData2);
						}
						else
						{
							Segment left = geometry2.m_Left;
							Segment right3 = geometry2.m_Right;
							CheckSegment(input, raycastIndex, edge.m_Start, entity, left, curve.m_Bezier, netCompositionData2);
							CheckSegment(input, raycastIndex, edge.m_Start, entity, right3, curve.m_Bezier, netCompositionData2);
							left.m_Right = geometry2.m_Middle;
							right3.m_Left = geometry2.m_Middle;
							CheckSegment(input, raycastIndex, edge.m_Start, entity, left, curve.m_Bezier, netCompositionData2);
							CheckSegment(input, raycastIndex, edge.m_Start, entity, right3, curve.m_Bezier, netCompositionData2);
						}
					}
				}
				if (!x.z)
				{
					return;
				}
				NetCompositionData netCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
				if (((netCompositionData3.m_State & CompositionState.Marker) == 0) ? ((byte)(NetUtils.GetCollisionMask(netCompositionData3, ignoreMarkers: true) & input.m_CollisionMask) != 0) : ((byte)(input.m_Flags & RaycastFlags.Markers) != 0))
				{
					if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
					{
						float maxElevation3 = 0f - input.m_Offset.y - netCompositionData3.m_SurfaceHeight.max;
						SetElevationOffset(ref geometry3, edge.m_End, maxElevation3);
					}
					if (geometry3.m_MiddleRadius > 0f)
					{
						CheckSegment(input, raycastIndex, edge.m_End, entity, geometry3.m_Left, curve.m_Bezier, netCompositionData3);
						Segment right4 = geometry3.m_Right;
						Segment right5 = geometry3.m_Right;
						right4.m_Right = MathUtils.Lerp(geometry3.m_Right.m_Left, geometry3.m_Right.m_Right, 0.5f);
						right4.m_Right.d = geometry3.m_Middle.d;
						right5.m_Left = right4.m_Right;
						CheckSegment(input, raycastIndex, edge.m_End, entity, right4, curve.m_Bezier, netCompositionData3);
						CheckSegment(input, raycastIndex, edge.m_End, entity, right5, curve.m_Bezier, netCompositionData3);
					}
					else
					{
						Segment left2 = geometry3.m_Left;
						Segment right6 = geometry3.m_Right;
						CheckSegment(input, raycastIndex, edge.m_End, entity, left2, curve.m_Bezier, netCompositionData3);
						CheckSegment(input, raycastIndex, edge.m_End, entity, right6, curve.m_Bezier, netCompositionData3);
						left2.m_Right = geometry3.m_Middle;
						right6.m_Left = geometry3.m_Middle;
						CheckSegment(input, raycastIndex, edge.m_End, entity, left2, curve.m_Bezier, netCompositionData3);
						CheckSegment(input, raycastIndex, edge.m_End, entity, right6, curve.m_Bezier, netCompositionData3);
					}
				}
			}
			else
			{
				if ((input.m_Flags & RaycastFlags.Markers) == 0)
				{
					return;
				}
				Edge edge2 = m_EdgeData[entity];
				Curve curve2 = m_CurveData[entity];
				Bezier4x3 curve3 = curve2.m_Bezier;
				if ((input.m_Flags & RaycastFlags.ElevateOffset) != 0)
				{
					float maxElevation4 = 0f - input.m_Offset.y;
					SetElevationOffset(ref curve3, entity, edge2.m_Start, edge2.m_End, maxElevation4);
				}
				Line3.Segment line = input.m_Line + input.m_Offset;
				float2 t2;
				float num = MathUtils.Distance(curve3, line, out t2);
				if (num < 0.5f)
				{
					RaycastResult result = new RaycastResult
					{
						m_Owner = entity,
						m_Hit = 
						{
							m_HitEntity = entity,
							m_Position = MathUtils.Position(curve2.m_Bezier, t2.x),
							m_HitPosition = MathUtils.Position(line, t2.y),
							m_NormalizedDistance = t2.y - (0.5f - num) / math.max(1f, MathUtils.Length(line)),
							m_CurvePosition = t2.x
						}
					};
					if (ValidateResult(input, ref result))
					{
						m_Results.Accumulate(raycastIndex, result);
					}
				}
			}
		}

		private void SetElevationOffset(ref float3 position, Entity node, float maxElevation)
		{
			if (m_ElevationData.HasComponent(node))
			{
				Elevation elevation = m_ElevationData[node];
				float x = math.lerp(elevation.m_Elevation.x, elevation.m_Elevation.y, 0.5f);
				position.y -= math.min(x, maxElevation);
			}
		}

		private void SetElevationOffset(ref Bezier4x3 curve, Entity edge, Entity startNode, Entity endNode, float maxElevation)
		{
			float3 @float = default(float3);
			if (m_ElevationData.HasComponent(startNode))
			{
				Elevation elevation = m_ElevationData[startNode];
				@float.x = math.lerp(elevation.m_Elevation.x, elevation.m_Elevation.y, 0.5f);
			}
			if (m_ElevationData.HasComponent(edge))
			{
				Elevation elevation2 = m_ElevationData[edge];
				@float.y = math.lerp(elevation2.m_Elevation.x, elevation2.m_Elevation.y, 0.5f);
			}
			if (m_ElevationData.HasComponent(endNode))
			{
				Elevation elevation3 = m_ElevationData[endNode];
				@float.z = math.lerp(elevation3.m_Elevation.x, elevation3.m_Elevation.y, 0.5f);
			}
			if (math.any(@float != 0f))
			{
				SetElevationOffset(ref curve, @float.xy, maxElevation);
				SetElevationOffset(ref curve, @float.yz, maxElevation);
			}
		}

		private void SetElevationOffset(ref EdgeGeometry geometry, Entity edge, Entity startNode, Entity endNode, float maxElevation)
		{
			float3 @float = default(float3);
			if (m_ElevationData.HasComponent(startNode))
			{
				Elevation elevation = m_ElevationData[startNode];
				@float.x = math.lerp(elevation.m_Elevation.x, elevation.m_Elevation.y, 0.5f);
			}
			if (m_ElevationData.HasComponent(edge))
			{
				Elevation elevation2 = m_ElevationData[edge];
				@float.y = math.lerp(elevation2.m_Elevation.x, elevation2.m_Elevation.y, 0.5f);
			}
			if (m_ElevationData.HasComponent(endNode))
			{
				Elevation elevation3 = m_ElevationData[endNode];
				@float.z = math.lerp(elevation3.m_Elevation.x, elevation3.m_Elevation.y, 0.5f);
			}
			if (math.any(@float != 0f))
			{
				SetElevationOffset(ref geometry.m_Start.m_Left, @float.xy, maxElevation);
				SetElevationOffset(ref geometry.m_Start.m_Right, @float.xy, maxElevation);
				SetElevationOffset(ref geometry.m_End.m_Left, @float.yz, maxElevation);
				SetElevationOffset(ref geometry.m_End.m_Right, @float.yz, maxElevation);
			}
		}

		private void SetElevationOffset(ref EdgeNodeGeometry geometry, Entity node, float maxElevation)
		{
			if (m_ElevationData.HasComponent(node))
			{
				Elevation elevation = m_ElevationData[node];
				float offset = math.lerp(elevation.m_Elevation.x, elevation.m_Elevation.y, 0.5f);
				SetElevationOffset(ref geometry.m_Left.m_Left, offset, maxElevation);
				SetElevationOffset(ref geometry.m_Left.m_Right, offset, maxElevation);
				SetElevationOffset(ref geometry.m_Right.m_Left, offset, maxElevation);
				SetElevationOffset(ref geometry.m_Right.m_Right, offset, maxElevation);
				SetElevationOffset(ref geometry.m_Middle, offset, maxElevation);
			}
		}

		private void SetElevationOffset(ref Bezier4x3 curve, float offset, float maxElevation)
		{
			curve.a.y -= math.min(offset, maxElevation);
			curve.b.y -= math.min(offset, maxElevation);
			curve.c.y -= math.min(offset, maxElevation);
			curve.d.y -= math.min(offset, maxElevation);
		}

		private void SetElevationOffset(ref Bezier4x3 curve, float2 offset, float maxElevation)
		{
			curve.a.y -= math.min(offset.x, maxElevation);
			curve.b.y -= math.min(math.lerp(offset.x, offset.y, 1f / 3f), maxElevation);
			curve.c.y -= math.min(math.lerp(offset.x, offset.y, 2f / 3f), maxElevation);
			curve.d.y -= math.min(offset.y, maxElevation);
		}

		private void CheckSegment(RaycastInput input, int raycastIndex, Entity owner, Entity hitEntity, Segment segment, Bezier4x3 curve, NetCompositionData prefabCompositionData)
		{
			Line3.Segment line = input.m_Line + input.m_Offset;
			float3 a = segment.m_Left.a;
			float3 @float = segment.m_Right.a;
			if ((input.m_Flags & RaycastFlags.ElevateOffset) == 0)
			{
				a.y += prefabCompositionData.m_SurfaceHeight.max;
				@float.y += prefabCompositionData.m_SurfaceHeight.max;
			}
			for (int i = 1; i <= 8; i++)
			{
				float t = (float)i / 8f;
				float3 float2 = MathUtils.Position(segment.m_Left, t);
				float3 float3 = MathUtils.Position(segment.m_Right, t);
				if ((input.m_Flags & RaycastFlags.ElevateOffset) == 0)
				{
					float2.y += prefabCompositionData.m_SurfaceHeight.max;
					float3.y += prefabCompositionData.m_SurfaceHeight.max;
				}
				Triangle3 triangle = new Triangle3(a, @float, float2);
				Triangle3 triangle2 = new Triangle3(float3, float2, @float);
				if (MathUtils.Intersect(triangle, line, out var t2))
				{
					float3 hitPosition = MathUtils.Position(line, t2.z);
					MathUtils.Distance(curve.xz, hitPosition.xz, out var t3);
					RaycastResult result = new RaycastResult
					{
						m_Owner = owner,
						m_Hit = 
						{
							m_HitEntity = hitEntity,
							m_Position = MathUtils.Position(curve, t3),
							m_HitPosition = hitPosition,
							m_NormalizedDistance = t2.z + 0.5f / math.max(1f, MathUtils.Length(line)),
							m_CurvePosition = t3
						}
					};
					if (ValidateResult(input, ref result))
					{
						m_Results.Accumulate(raycastIndex, result);
					}
				}
				else if (MathUtils.Intersect(triangle2, line, out t2))
				{
					float3 hitPosition2 = MathUtils.Position(line, t2.z);
					MathUtils.Distance(curve.xz, hitPosition2.xz, out var t4);
					RaycastResult result2 = new RaycastResult
					{
						m_Owner = owner,
						m_Hit = 
						{
							m_HitEntity = hitEntity,
							m_Position = MathUtils.Position(curve, t4),
							m_HitPosition = hitPosition2,
							m_NormalizedDistance = t2.z + 0.5f / math.max(1f, MathUtils.Length(line)),
							m_CurvePosition = t4
						}
					};
					if (ValidateResult(input, ref result2))
					{
						m_Results.Accumulate(raycastIndex, result2);
					}
				}
				a = float2;
				@float = float3;
			}
		}

		private bool ValidateResult(RaycastInput input, ref RaycastResult result)
		{
			TypeMask typeMask = TypeMask.Net;
			Entity owner = Entity.Null;
			TypeMask typeMask2 = TypeMask.None;
			while (true)
			{
				if ((input.m_Flags & RaycastFlags.UpgradeIsMain) != 0)
				{
					if (m_ServiceUpgradeData.HasComponent(result.m_Owner))
					{
						break;
					}
					if (m_InstalledUpgrades.TryGetBuffer(result.m_Owner, out var bufferData) && bufferData.Length != 0)
					{
						owner = Entity.Null;
						typeMask2 = TypeMask.None;
						typeMask = TypeMask.StaticObjects;
						result.m_Owner = bufferData[0].m_Upgrade;
						break;
					}
				}
				else if ((input.m_Flags & RaycastFlags.SubBuildings) != 0 && m_ServiceUpgradeData.HasComponent(result.m_Owner) && (typeMask == TypeMask.Net || m_BuildingData.HasComponent(result.m_Owner)))
				{
					break;
				}
				if (!m_OwnerData.TryGetComponent(result.m_Owner, out var componentData))
				{
					break;
				}
				if ((input.m_TypeMask & typeMask) != TypeMask.None && (typeMask != TypeMask.Net || typeMask2 != TypeMask.Net || (input.m_Flags & RaycastFlags.ElevateOffset) == 0))
				{
					owner = result.m_Owner;
					typeMask2 = typeMask;
				}
				result.m_Owner = componentData.m_Owner;
				typeMask = ((!m_EdgeData.HasComponent(result.m_Owner)) ? TypeMask.StaticObjects : TypeMask.Net);
			}
			if ((input.m_Flags & RaycastFlags.SubElements) != 0 && (input.m_TypeMask & typeMask2) != TypeMask.None)
			{
				result.m_Owner = owner;
				typeMask = typeMask2;
			}
			else if ((input.m_Flags & RaycastFlags.NoMainElements) != 0)
			{
				return false;
			}
			if ((input.m_TypeMask & typeMask) == 0)
			{
				return false;
			}
			switch (typeMask)
			{
			case TypeMask.Net:
			{
				PrefabRef prefabRef = m_PrefabRefData[result.m_Owner];
				return (m_PrefabNetData[prefabRef.m_Prefab].m_ConnectLayers & input.m_NetLayerMask) != 0;
			}
			case TypeMask.StaticObjects:
				return CheckPlaceholder(input, ref result.m_Owner);
			default:
				return true;
			}
		}

		private bool CheckPlaceholder(RaycastInput input, ref Entity entity)
		{
			if ((input.m_Flags & RaycastFlags.Placeholders) != 0)
			{
				return true;
			}
			if (m_PlaceholderData.HasComponent(entity))
			{
				if (m_AttachmentData.HasComponent(entity))
				{
					Attachment attachment = m_AttachmentData[entity];
					if (m_PrefabRefData.HasComponent(attachment.m_Attached))
					{
						entity = attachment.m_Attached;
						return true;
					}
				}
				return false;
			}
			return true;
		}
	}

	[BurstCompile]
	public struct RaycastLabelsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public NativeArray<RaycastSystem.EntityResult> m_Edges;

		[ReadOnly]
		public ComponentLookup<Aggregated> m_AggregatedData;

		[ReadOnly]
		public ComponentLookup<LabelExtents> m_LabelExtentsData;

		[ReadOnly]
		public BufferLookup<LabelPosition> m_LabelPositions;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastSystem.EntityResult entityResult = m_Edges[index];
			RaycastInput input = m_Input[entityResult.m_RaycastIndex];
			if ((input.m_TypeMask & TypeMask.Labels) != TypeMask.None && m_AggregatedData.TryGetComponent(entityResult.m_Entity, out var componentData))
			{
				CheckAggregate(entityResult.m_RaycastIndex, input, componentData.m_Aggregate);
			}
		}

		private void CheckAggregate(int raycastIndex, RaycastInput input, Entity aggregate)
		{
			if (!m_LabelExtentsData.TryGetComponent(aggregate, out var componentData))
			{
				return;
			}
			DynamicBuffer<LabelPosition> dynamicBuffer = m_LabelPositions[aggregate];
			Quad3 quad = default(Quad3);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				LabelPosition labelPosition = dynamicBuffer[i];
				if ((NetUtils.GetCollisionMask(labelPosition) & input.m_CollisionMask) == 0)
				{
					continue;
				}
				float3 @float = MathUtils.Position(labelPosition.m_Curve, 0.5f);
				float num = math.max(math.sqrt(math.distance(input.m_Line.a, @float) * 0.0001f), 0.01f);
				if (num >= labelPosition.m_MaxScale * 0.95f)
				{
					continue;
				}
				float3 y = MathUtils.Tangent(labelPosition.m_Curve, 0.5f);
				Bounds2 bounds = ((math.dot(m_CameraRight, y) < 0f) ? new Bounds2(-componentData.m_Bounds.max, -componentData.m_Bounds.min) : componentData.m_Bounds);
				bounds *= (float2)num;
				Bounds1 t = new Bounds1(0f, 1f);
				Bounds1 t2 = new Bounds1(0f, 1f);
				float num2 = 0f - bounds.min.x - labelPosition.m_HalfLength;
				float num3 = bounds.max.x - labelPosition.m_HalfLength;
				if (num2 < 0f)
				{
					MathUtils.ClampLength(labelPosition.m_Curve, ref t, 0f - num2);
				}
				else
				{
					t.max = 0f;
				}
				if (num3 < 0f)
				{
					MathUtils.ClampLengthInverse(labelPosition.m_Curve, ref t2, 0f - num3);
				}
				else
				{
					t2.min = 1f;
				}
				if (num2 > 0f)
				{
					float3 float2 = math.normalizesafe(MathUtils.StartTangent(labelPosition.m_Curve));
					float3 float3 = new float3(0f - float2.z, 0f, float2.x);
					quad.a = labelPosition.m_Curve.a - float2 * num2 + float3 * bounds.min.y;
					quad.b = labelPosition.m_Curve.a - float2 * num2 + float3 * bounds.max.y;
					quad.c = labelPosition.m_Curve.a + float3 * bounds.max.y;
					quad.d = labelPosition.m_Curve.a + float3 * bounds.min.y;
					if (MathUtils.Intersect(quad, input.m_Line, out var t3))
					{
						float num4 = MathUtils.Size(bounds.y);
						RaycastResult value = default(RaycastResult);
						value.m_Owner = aggregate;
						value.m_Hit.m_HitEntity = value.m_Owner;
						value.m_Hit.m_Position = @float;
						value.m_Hit.m_HitPosition = MathUtils.Position(input.m_Line, t3);
						value.m_Hit.m_NormalizedDistance = t3 - num4 / math.max(1f, MathUtils.Length(input.m_Line));
						value.m_Hit.m_CellIndex = new int2(labelPosition.m_ElementIndex, -1);
						m_Results.Accumulate(raycastIndex, value);
					}
				}
				else
				{
					float3 float4 = MathUtils.Position(labelPosition.m_Curve, t.max);
					float3 float5 = math.normalizesafe(MathUtils.Tangent(labelPosition.m_Curve, t.max));
					float3 float6 = new float3(0f - float5.z, 0f, float5.x);
					quad.c = float4 + float6 * bounds.max.y;
					quad.d = float4 + float6 * bounds.min.y;
				}
				for (int j = 1; j <= 16; j++)
				{
					float t4 = math.lerp(t.max, t2.min, (float)j * 0.0625f);
					float3 float7 = MathUtils.Position(labelPosition.m_Curve, t4);
					float3 float8 = math.normalizesafe(MathUtils.Tangent(labelPosition.m_Curve, t4));
					float3 float9 = new float3(0f - float8.z, 0f, float8.x);
					quad.a = quad.d;
					quad.b = quad.c;
					quad.c = float7 + float9 * bounds.max.y;
					quad.d = float7 + float9 * bounds.min.y;
					if (MathUtils.Intersect(quad, input.m_Line, out var t5))
					{
						float num5 = MathUtils.Size(bounds.y);
						RaycastResult value2 = default(RaycastResult);
						value2.m_Owner = aggregate;
						value2.m_Hit.m_HitEntity = value2.m_Owner;
						value2.m_Hit.m_Position = @float;
						value2.m_Hit.m_HitPosition = MathUtils.Position(input.m_Line, t5);
						value2.m_Hit.m_NormalizedDistance = t5 - num5 / math.max(1f, MathUtils.Length(input.m_Line));
						value2.m_Hit.m_CellIndex = new int2(labelPosition.m_ElementIndex, -1);
						m_Results.Accumulate(raycastIndex, value2);
					}
				}
				if (num3 > 0f)
				{
					float3 float10 = math.normalizesafe(MathUtils.EndTangent(labelPosition.m_Curve));
					float3 float11 = new float3(0f - float10.z, 0f, float10.x);
					quad.a = quad.d;
					quad.b = quad.c;
					quad.c = labelPosition.m_Curve.d + float10 * num3 + float11 * bounds.min.y;
					quad.d = labelPosition.m_Curve.d + float10 * num3 + float11 * bounds.max.y;
					if (MathUtils.Intersect(quad, input.m_Line, out var t6))
					{
						float num6 = MathUtils.Size(bounds.y);
						RaycastResult value3 = default(RaycastResult);
						value3.m_Owner = aggregate;
						value3.m_Hit.m_HitEntity = value3.m_Owner;
						value3.m_Hit.m_Position = @float;
						value3.m_Hit.m_HitPosition = MathUtils.Position(input.m_Line, t6);
						value3.m_Hit.m_NormalizedDistance = t6 - num6 / math.max(1f, MathUtils.Length(input.m_Line));
						value3.m_Hit.m_CellIndex = new int2(labelPosition.m_ElementIndex, -1);
						m_Results.Accumulate(raycastIndex, value3);
					}
				}
			}
		}
	}

	[BurstCompile]
	public struct RaycastLanesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public float m_FovTan;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeArray<RaycastSystem.EntityResult> m_Lanes;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneGeometryData> m_PrefabLaneGeometryData;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastSystem.EntityResult entityResult = m_Lanes[index];
			RaycastInput raycastInput = m_Input[entityResult.m_RaycastIndex];
			if ((raycastInput.m_TypeMask & TypeMask.Lanes) == 0)
			{
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[entityResult.m_Entity];
			if (m_PrefabUtilityLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_UtilityTypes & raycastInput.m_UtilityTypeMask) != UtilityTypes.None)
			{
				Curve curve = m_CurveData[entityResult.m_Entity];
				float2 t;
				float num = MathUtils.Distance(curve.m_Bezier, raycastInput.m_Line, out t);
				float3 @float = MathUtils.Position(raycastInput.m_Line, t.y);
				float cameraDistance = math.distance(@float, raycastInput.m_Line.a);
				float num2 = GetMinLaneRadius(m_FovTan, cameraDistance);
				if (m_PrefabLaneGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					num2 = math.max(num2, componentData2.m_Size.x * 0.5f);
				}
				if (num < num2)
				{
					RaycastResult value = default(RaycastResult);
					value.m_Owner = entityResult.m_Entity;
					value.m_Hit.m_HitEntity = value.m_Owner;
					value.m_Hit.m_Position = MathUtils.Position(curve.m_Bezier, t.x);
					value.m_Hit.m_HitPosition = @float;
					value.m_Hit.m_CurvePosition = t.x;
					value.m_Hit.m_NormalizedDistance = t.y - (num2 - num) / math.max(1f, MathUtils.Length(raycastInput.m_Line));
					m_Results.Accumulate(entityResult.m_RaycastIndex, value);
				}
			}
		}
	}

	public static float GetMinLaneRadius(float fovTan, float cameraDistance)
	{
		return cameraDistance * fovTan * 0.01f;
	}
}
