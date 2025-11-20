using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class AttachPositionSystem : GameSystemBase
{
	[BurstCompile]
	private struct AttachPositionJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<PillarData> m_PrefabPillarData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> m_NetCompositionAreas;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> m_NetCompositionLanes;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Attached> nativeArray2 = chunk.GetNativeArray(ref m_AttachedType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray[i];
				Attached attached = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				Transform transform = m_TransformData[entity];
				Transform transform2 = transform;
				FixAttachedPosition(attached, prefabRef, ref transform2);
				if (!transform2.Equals(transform))
				{
					MoveObject(entity, attached, transform, transform2);
				}
			}
		}

		private void MoveObject(Entity entity, Attached attached, Transform oldTransform, Transform newTransform)
		{
			m_TransformData[entity] = newTransform;
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			Transform inverseParentTransform = ObjectUtils.InverseTransform(oldTransform);
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (m_OwnerData.TryGetComponent(subObject, out var componentData) && !(componentData.m_Owner != entity) && m_UpdatedData.HasComponent(subObject) && !m_AttachedData.HasComponent(subObject))
				{
					Transform transform = m_TransformData[subObject];
					Transform transform2 = ObjectUtils.LocalToWorld(newTransform, ObjectUtils.WorldToLocal(inverseParentTransform, transform));
					if (m_ElevationData.TryGetComponent(subObject, out var componentData2) && (componentData2.m_Flags & ElevationFlags.OnAttachedParent) != 0 && m_EdgeGeometryData.TryGetComponent(attached.m_Parent, out var componentData3))
					{
						transform2.m_Position.y = ObjectUtils.GetAttachedParentHeight(componentData3, transform2);
					}
					if (!transform2.Equals(transform))
					{
						MoveObject(subObject, attached, transform, transform2);
					}
				}
			}
		}

		private void FixAttachedPosition(Attached attached, PrefabRef prefabRef, ref Transform transform)
		{
			if (!m_PrefabPlaceableObjectData.HasComponent(prefabRef.m_Prefab) || (m_PrefabPlaceableObjectData[prefabRef.m_Prefab].m_Flags & PlacementFlags.CanOverlap) != PlacementFlags.None)
			{
				return;
			}
			if (m_PrefabPillarData.HasComponent(prefabRef.m_Prefab))
			{
				PillarData pillarData = m_PrefabPillarData[prefabRef.m_Prefab];
				if (pillarData.m_Type != PillarType.Vertical && pillarData.m_Type != PillarType.Base)
				{
					return;
				}
			}
			if (m_NodeData.HasComponent(attached.m_Parent))
			{
				Node node = m_NodeData[attached.m_Parent];
				transform.m_Position = node.m_Position;
				transform.m_Rotation = node.m_Rotation;
			}
			else
			{
				if (!m_CompositionData.HasComponent(attached.m_Parent))
				{
					return;
				}
				Composition composition = m_CompositionData[attached.m_Parent];
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[attached.m_Parent];
				Curve curve = m_CurveData[attached.m_Parent];
				if (!m_NetCompositionAreas.HasBuffer(composition.m_Edge))
				{
					return;
				}
				DynamicBuffer<NetCompositionArea> areas = m_NetCompositionAreas[composition.m_Edge];
				NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
				float num = 0f;
				if (m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					if ((objectGeometryData.m_Flags & GeometryFlags.Standing) != GeometryFlags.None)
					{
						num = objectGeometryData.m_LegSize.z * 0.5f + objectGeometryData.m_LegOffset.y;
						if (objectGeometryData.m_LegSize.y <= prefabCompositionData.m_HeightRange.max)
						{
							num = math.max(num, objectGeometryData.m_Size.z * 0.5f);
						}
					}
					else
					{
						num = objectGeometryData.m_Size.z * 0.5f;
					}
				}
				Transform bestTransform = transform;
				Transform bestTransform2 = transform;
				float3 curvePosition = MathUtils.Position(curve.m_Bezier, attached.m_CurvePosition);
				bool snapEdge = false;
				if (m_PrefabRouteConnectionData.HasComponent(prefabRef.m_Prefab) && m_NetCompositionLanes.HasBuffer(composition.m_Edge))
				{
					RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef.m_Prefab];
					DynamicBuffer<NetCompositionLane> lanes = m_NetCompositionLanes[composition.m_Edge];
					if (routeConnectionData.m_RouteConnectionType != RouteConnectionType.None)
					{
						float3 curveTangent = MathUtils.Tangent(curve.m_Bezier, attached.m_CurvePosition);
						SnapRouteLane(transform, ref bestTransform, ref snapEdge, curvePosition, curveTangent, routeConnectionData.m_RouteConnectionType, routeConnectionData.m_RouteTrackType, routeConnectionData.m_RouteRoadType, prefabCompositionData, lanes);
					}
				}
				if (attached.m_CurvePosition < 0.5f)
				{
					SnapSegmentAreas(bestTransform, ref bestTransform2, snapEdge, num, curvePosition, edgeGeometry.m_Start, prefabCompositionData, areas);
				}
				else
				{
					SnapSegmentAreas(bestTransform, ref bestTransform2, snapEdge, num, curvePosition, edgeGeometry.m_End, prefabCompositionData, areas);
				}
				transform = bestTransform2;
			}
		}

		private void SnapRouteLane(Transform transform, ref Transform bestTransform, ref bool snapEdge, float3 curvePosition, float3 curveTangent, RouteConnectionType connectionType, TrackTypes trackType, RoadTypes carType, NetCompositionData prefabCompositionData, DynamicBuffer<NetCompositionLane> lanes)
		{
			LaneFlags laneFlags;
			switch (connectionType)
			{
			default:
				return;
			case RouteConnectionType.Road:
				laneFlags = LaneFlags.Road;
				break;
			case RouteConnectionType.Track:
				laneFlags = LaneFlags.Track;
				break;
			case RouteConnectionType.Pedestrian:
				laneFlags = LaneFlags.Pedestrian;
				break;
			}
			float2 @float = MathUtils.Right(math.normalizesafe(curveTangent.xz));
			float3 float2 = transform.m_Position - curvePosition;
			float2 y = new float2(math.dot(@float, float2.xz), float2.y);
			float num = float.MaxValue;
			for (int i = 0; i < lanes.Length; i++)
			{
				NetCompositionLane netCompositionLane = lanes[i];
				if ((netCompositionLane.m_Flags & laneFlags) != laneFlags)
				{
					continue;
				}
				float num2 = math.distancesq(netCompositionLane.m_Position.xy, y);
				if (!(num2 < num))
				{
					continue;
				}
				switch (connectionType)
				{
				case RouteConnectionType.Track:
					if (!m_PrefabTrackLaneData.HasComponent(netCompositionLane.m_Lane) || (m_PrefabTrackLaneData[netCompositionLane.m_Lane].m_TrackTypes & trackType) == 0)
					{
						continue;
					}
					break;
				case RouteConnectionType.Road:
					if (!m_PrefabCarLaneData.HasComponent(netCompositionLane.m_Lane) || (m_PrefabCarLaneData[netCompositionLane.m_Lane].m_RoadTypes & carType) == 0)
					{
						continue;
					}
					break;
				}
				num = num2;
				float2 direction = @float;
				if ((netCompositionLane.m_Flags & LaneFlags.Invert) == 0)
				{
					direction = -@float;
				}
				float num3 = netCompositionLane.m_Position.x;
				if ((laneFlags & (LaneFlags.Road | LaneFlags.Track)) != 0)
				{
					num3 += m_PrefabLaneData[netCompositionLane.m_Lane].m_Width * math.select(0.25f, -0.25f, math.dot(transform.m_Position.xz - curvePosition.xz, @float) < 0f);
				}
				bestTransform.m_Position = curvePosition;
				bestTransform.m_Position.xz += @float * num3;
				bestTransform.m_Position.y += netCompositionLane.m_Position.y;
				bestTransform.m_Rotation = ToolUtils.CalculateRotation(direction);
				snapEdge = true;
			}
		}

		private void SnapSegmentAreas(Transform transform, ref Transform bestTransform, bool snapEdge, float radius, float3 curvePosition, Segment segment, NetCompositionData prefabCompositionData, DynamicBuffer<NetCompositionArea> areas)
		{
			float num = float.MaxValue;
			for (int i = 0; i < areas.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas[i];
				if ((netCompositionArea.m_Flags & NetAreaFlags.Buildable) == 0)
				{
					continue;
				}
				float num2 = netCompositionArea.m_Width * 0.51f;
				if (radius >= num2)
				{
					continue;
				}
				Bezier4x3 curve = MathUtils.Lerp(segment.m_Left, segment.m_Right, netCompositionArea.m_Position.x / prefabCompositionData.m_Width + 0.5f);
				MathUtils.Distance(curve, curvePosition, out var t);
				float3 @float = MathUtils.Position(curve, t);
				float num3 = math.distancesq(@float, transform.m_Position);
				if (!(num3 < num))
				{
					continue;
				}
				num = num3;
				float2 forward = math.normalizesafe(MathUtils.Tangent(curve, t).xz);
				if ((netCompositionArea.m_Flags & NetAreaFlags.Median) == 0)
				{
					forward = (((netCompositionArea.m_Flags & NetAreaFlags.Invert) == 0) ? MathUtils.Left(forward) : MathUtils.Right(forward));
				}
				else
				{
					forward = MathUtils.Right(forward);
					if (math.dot(math.forward(transform.m_Rotation).xz, forward) < 0f)
					{
						forward = -forward;
					}
				}
				float3 float2 = MathUtils.Position(MathUtils.Lerp(segment.m_Left, segment.m_Right, netCompositionArea.m_SnapPosition.x / prefabCompositionData.m_Width + 0.5f), t);
				float maxLength = math.max(0f, math.min(netCompositionArea.m_Width * 0.5f, math.abs(netCompositionArea.m_SnapPosition.x - netCompositionArea.m_Position.x) + netCompositionArea.m_SnapWidth * 0.5f) - radius);
				@float.xz += MathUtils.ClampLength(float2.xz - @float.xz, maxLength);
				if (snapEdge)
				{
					float maxLength2 = math.max(0f, netCompositionArea.m_SnapWidth * 0.5f - radius);
					@float.xz += MathUtils.ClampLength(transform.m_Position.xz - @float.xz, maxLength2);
				}
				@float.y += netCompositionArea.m_Position.y;
				bestTransform.m_Position = @float;
				bestTransform.m_Rotation = ToolUtils.CalculateRotation(forward);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PillarData> __Game_Prefabs_PillarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionArea> __Game_Prefabs_NetCompositionArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> __Game_Prefabs_NetCompositionLane_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_PillarData_RO_ComponentLookup = state.GetComponentLookup<PillarData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Prefabs_NetCompositionArea_RO_BufferLookup = state.GetBufferLookup<NetCompositionArea>(isReadOnly: true);
			__Game_Prefabs_NetCompositionLane_RO_BufferLookup = state.GetBufferLookup<NetCompositionLane>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
		}
	}

	private EntityQuery m_UpdateQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Attached>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_UpdateQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		AttachPositionJob jobData = new AttachPositionJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPillarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PillarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetCompositionAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetCompositionLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UpdateQuery, base.Dependency);
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
	public AttachPositionSystem()
	{
	}
}
