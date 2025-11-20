using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Routes;

public static class RaycastJobs
{
	[BurstCompile]
	public struct FindRoutesFromTreeJob : IJob
	{
		private struct FindRoutesIterator : INativeQuadTreeIterator<RouteSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<RouteSearchItem, QuadTreeBoundsXZ>
		{
			public int m_RaycastIndex;

			public Line3.Segment m_Line;

			public NativeList<RouteItem> m_RouteList;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float2 t;
				return MathUtils.Intersect(bounds.m_Bounds, m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, RouteSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Line, out var _))
				{
					ref NativeList<RouteItem> routeList = ref m_RouteList;
					RouteItem value = new RouteItem
					{
						m_Entity = item.m_Entity,
						m_Element = item.m_Element,
						m_RaycastIndex = m_RaycastIndex
					};
					routeList.Add(in value);
				}
			}
		}

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeQuadTree<RouteSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		[WriteOnly]
		public NativeList<RouteItem> m_RouteList;

		public void Execute()
		{
			for (int i = 0; i < m_Input.Length; i++)
			{
				RaycastInput raycastInput = m_Input[i];
				if ((raycastInput.m_TypeMask & (TypeMask.RouteWaypoints | TypeMask.RouteSegments)) != TypeMask.None)
				{
					FindRoutesIterator iterator = new FindRoutesIterator
					{
						m_RaycastIndex = i,
						m_Line = raycastInput.m_Line,
						m_RouteList = m_RouteList
					};
					m_SearchTree.Iterate(ref iterator);
				}
			}
		}
	}

	public struct RouteItem
	{
		public Entity m_Entity;

		public int m_Element;

		public int m_RaycastIndex;
	}

	[BurstCompile]
	public struct RaycastRoutesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeArray<RouteItem> m_Routes;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_PrefabTransportLineData;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_WaypointData;

		[ReadOnly]
		public ComponentLookup<Segment> m_SegmentData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<HiddenRoute> m_HiddenRouteData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public BufferLookup<CurveElement> m_CurveElements;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RouteItem routeItem = m_Routes[index];
			RaycastInput raycastInput = m_Input[routeItem.m_RaycastIndex];
			if ((raycastInput.m_TypeMask & TypeMask.RouteWaypoints) != TypeMask.None && m_WaypointData.TryGetComponent(routeItem.m_Entity, out var componentData))
			{
				PrefabRef prefabRef = m_PrefabRefData[routeItem.m_Entity];
				Position position = m_PositionData[routeItem.m_Entity];
				Owner owner = m_OwnerData[routeItem.m_Entity];
				if (m_HiddenRouteData.HasComponent(owner.m_Owner))
				{
					return;
				}
				RouteData routeData = m_PrefabRouteData[prefabRef.m_Prefab];
				if ((raycastInput.m_RouteType != RouteType.None && raycastInput.m_RouteType != routeData.m_Type) || (m_PrefabTransportLineData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && ((raycastInput.m_TransportType != TransportType.None && raycastInput.m_TransportType != componentData2.m_TransportType) || ((raycastInput.m_Flags & (RaycastFlags.Cargo | RaycastFlags.Passenger)) != 0 && ((raycastInput.m_Flags & RaycastFlags.Passenger) == 0 || !componentData2.m_PassengerTransport) && ((raycastInput.m_Flags & RaycastFlags.Cargo) == 0 || !componentData2.m_CargoTransport)))) || (raycastInput.m_Owner != Entity.Null && raycastInput.m_Owner != owner.m_Owner && !HasOwner(owner.m_Owner, raycastInput.m_Owner)))
				{
					return;
				}
				float t;
				float num = MathUtils.Distance(raycastInput.m_Line, position.m_Position, out t) - routeData.m_SnapDistance;
				if (num < 0f)
				{
					RaycastResult value = default(RaycastResult);
					value.m_Owner = owner.m_Owner;
					value.m_Hit.m_HitEntity = value.m_Owner;
					value.m_Hit.m_Position = position.m_Position;
					value.m_Hit.m_HitPosition = MathUtils.Position(raycastInput.m_Line, t);
					value.m_Hit.m_NormalizedDistance = t;
					value.m_Hit.m_CellIndex = new int2(componentData.m_Index, -1);
					value.m_Hit.m_NormalizedDistance -= 100f / math.max(1f, MathUtils.Length(raycastInput.m_Line));
					value.m_Hit.m_NormalizedDistance += num * 1E-06f / math.max(1f, routeData.m_SnapDistance);
					value.m_Hit.m_NormalizedDistance += (float)componentData.m_Index * (value.m_Hit.m_NormalizedDistance * 1E-06f);
					m_Results.Accumulate(routeItem.m_RaycastIndex, value);
				}
			}
			if ((raycastInput.m_TypeMask & TypeMask.RouteSegments) == 0 || !m_SegmentData.TryGetComponent(routeItem.m_Entity, out var componentData3))
			{
				return;
			}
			PrefabRef prefabRef2 = m_PrefabRefData[routeItem.m_Entity];
			DynamicBuffer<CurveElement> dynamicBuffer = m_CurveElements[routeItem.m_Entity];
			Owner owner2 = m_OwnerData[routeItem.m_Entity];
			if (m_HiddenRouteData.HasComponent(owner2.m_Owner))
			{
				return;
			}
			RouteData routeData2 = m_PrefabRouteData[prefabRef2.m_Prefab];
			if ((raycastInput.m_RouteType == RouteType.None || raycastInput.m_RouteType == routeData2.m_Type) && (!m_PrefabTransportLineData.TryGetComponent(prefabRef2.m_Prefab, out var componentData4) || ((raycastInput.m_TransportType == TransportType.None || raycastInput.m_TransportType == componentData4.m_TransportType) && ((raycastInput.m_Flags & (RaycastFlags.Cargo | RaycastFlags.Passenger)) == 0 || ((raycastInput.m_Flags & RaycastFlags.Passenger) != 0 && componentData4.m_PassengerTransport) || ((raycastInput.m_Flags & RaycastFlags.Cargo) != 0 && componentData4.m_CargoTransport)))) && (!(raycastInput.m_Owner != Entity.Null) || !(raycastInput.m_Owner != owner2.m_Owner) || HasOwner(owner2.m_Owner, raycastInput.m_Owner)) && dynamicBuffer.Length > routeItem.m_Element)
			{
				CurveElement curveElement = dynamicBuffer[routeItem.m_Element];
				float2 t2;
				float num2 = MathUtils.Distance(curveElement.m_Curve, raycastInput.m_Line, out t2) - routeData2.m_SnapDistance * 0.5f;
				if (num2 < 0f)
				{
					RaycastResult value2 = default(RaycastResult);
					value2.m_Owner = owner2.m_Owner;
					value2.m_Hit.m_HitEntity = value2.m_Owner;
					value2.m_Hit.m_Position = MathUtils.Position(curveElement.m_Curve, t2.x);
					value2.m_Hit.m_HitPosition = MathUtils.Position(raycastInput.m_Line, t2.y);
					value2.m_Hit.m_NormalizedDistance = t2.y;
					value2.m_Hit.m_CellIndex = new int2(-1, componentData3.m_Index);
					value2.m_Hit.m_NormalizedDistance -= 100f / math.max(1f, MathUtils.Length(raycastInput.m_Line));
					value2.m_Hit.m_NormalizedDistance += num2 * 1E-06f / math.max(1f, routeData2.m_SnapDistance);
					m_Results.Accumulate(routeItem.m_RaycastIndex, value2);
				}
			}
		}

		private bool HasOwner(Entity entity, Entity owner)
		{
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				if (componentData.m_Owner == owner)
				{
					return true;
				}
				entity = componentData.m_Owner;
			}
			return false;
		}
	}
}
