using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateWaypointsSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateWaypointsJob : IJob
	{
		private struct SegmentKey : IEquatable<SegmentKey>
		{
			private Entity m_Prefab;

			private Entity m_OriginalRoute;

			private float4 m_Position;

			public SegmentKey(Entity prefab, Entity originalRoute, float4 position)
			{
				m_Prefab = prefab;
				m_OriginalRoute = originalRoute;
				m_Position = position;
			}

			public bool Equals(SegmentKey other)
			{
				if (m_Prefab.Equals(other.m_Prefab) && m_OriginalRoute.Equals(other.m_OriginalRoute))
				{
					return m_Position.Equals(other.m_Position);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return m_Prefab.GetHashCode() ^ m_Position.GetHashCode();
			}
		}

		private struct SegmentValue
		{
			public Entity m_Segment;

			public float4 m_StartPosition;

			public float4 m_EndPosition;

			public int m_Index;

			public SegmentValue(Entity segment, float4 startPosition, float4 endPosition, int index)
			{
				m_Segment = segment;
				m_StartPosition = startPosition;
				m_EndPosition = endPosition;
				m_Index = index;
			}
		}

		private struct SegmentBuffer
		{
			public Entity m_Original;

			public Entity m_OldStart;

			public Entity m_OldEnd;
		}

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DeletedChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Segment> m_SegmentType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> m_WaypointDefinitionType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_RouteData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Segment> m_SegmentData;

		[ReadOnly]
		public ComponentLookup<PathTargets> m_PathTargetsData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegments;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElementData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelMultiHashMap<SegmentKey, SegmentValue> oldSegments = new NativeParallelMultiHashMap<SegmentKey, SegmentValue>(100, Allocator.Temp);
			for (int i = 0; i < m_DeletedChunks.Length; i++)
			{
				FillOldSegments(m_DeletedChunks[i], oldSegments);
			}
			for (int j = 0; j < m_DefinitionChunks.Length; j++)
			{
				CreateWaypointsAndSegments(m_DefinitionChunks[j], oldSegments);
			}
			oldSegments.Dispose();
		}

		private void FillOldSegments(ArchetypeChunk chunk, NativeParallelMultiHashMap<SegmentKey, SegmentValue> oldSegments)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Segment> nativeArray2 = chunk.GetNativeArray(ref m_SegmentType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity segment = nativeArray[i];
				Segment segment2 = nativeArray2[i];
				Entity owner = nativeArray3[i].m_Owner;
				Entity prefab = nativeArray4[i].m_Prefab;
				if (!m_RouteWaypoints.HasBuffer(owner) || !m_TempData.HasComponent(owner))
				{
					continue;
				}
				DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[owner];
				Temp temp = m_TempData[owner];
				if (dynamicBuffer.Length > segment2.m_Index)
				{
					Entity waypoint = dynamicBuffer[segment2.m_Index].m_Waypoint;
					Entity waypoint2 = dynamicBuffer[math.select(segment2.m_Index + 1, 0, segment2.m_Index + 1 == dynamicBuffer.Length)].m_Waypoint;
					if (m_PositionData.HasComponent(waypoint) && m_PositionData.HasComponent(waypoint2))
					{
						float4 @float = new float4(m_PositionData[waypoint].m_Position, 0f);
						float4 float2 = new float4(m_PositionData[waypoint2].m_Position, 1f);
						oldSegments.Add(new SegmentKey(prefab, temp.m_Original, @float), new SegmentValue(segment, @float, float2, segment2.m_Index));
						oldSegments.Add(new SegmentKey(prefab, temp.m_Original, float2), new SegmentValue(segment, @float, float2, segment2.m_Index));
					}
				}
			}
		}

		private void FillOriginalSegments(Entity route, NativeParallelMultiHashMap<SegmentKey, SegmentValue> originalSegments)
		{
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[route];
			DynamicBuffer<RouteSegment> dynamicBuffer2 = m_RouteSegments[route];
			Entity prefab = m_PrefabRefData[route].m_Prefab;
			for (int i = 0; i < dynamicBuffer2.Length; i++)
			{
				Entity segment = dynamicBuffer2[i].m_Segment;
				Segment segment2 = m_SegmentData[segment];
				if (dynamicBuffer.Length > segment2.m_Index)
				{
					Entity waypoint = dynamicBuffer[segment2.m_Index].m_Waypoint;
					Entity waypoint2 = dynamicBuffer[math.select(segment2.m_Index + 1, 0, segment2.m_Index + 1 == dynamicBuffer.Length)].m_Waypoint;
					if (m_PositionData.HasComponent(waypoint) && m_PositionData.HasComponent(waypoint2))
					{
						float4 @float = new float4(m_PositionData[waypoint].m_Position, 0f);
						float4 float2 = new float4(m_PositionData[waypoint2].m_Position, 1f);
						originalSegments.Add(new SegmentKey(prefab, Entity.Null, @float), new SegmentValue(segment, @float, float2, segment2.m_Index));
						originalSegments.Add(new SegmentKey(prefab, Entity.Null, float2), new SegmentValue(segment, @float, float2, segment2.m_Index));
					}
				}
			}
		}

		private void CreateWaypointsAndSegments(ArchetypeChunk chunk, NativeParallelMultiHashMap<SegmentKey, SegmentValue> oldSegments)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			BufferAccessor<WaypointDefinition> bufferAccessor = chunk.GetBufferAccessor(ref m_WaypointDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				DynamicBuffer<WaypointDefinition> dynamicBuffer = bufferAccessor[i];
				Entity prefab = creationDefinition.m_Prefab;
				if (creationDefinition.m_Original != Entity.Null)
				{
					NativeParallelMultiHashMap<SegmentKey, SegmentValue> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<SegmentKey, SegmentValue>(100, Allocator.Temp);
					FillOriginalSegments(creationDefinition.m_Original, nativeParallelMultiHashMap);
					prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
					RouteData prefabRouteData = m_RouteData[prefab];
					TempFlags tempFlags = (TempFlags)0u;
					if ((creationDefinition.m_Flags & CreationFlags.Delete) != 0)
					{
						tempFlags |= TempFlags.Delete;
					}
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						CreateWaypoint(prefabRouteData, prefab, tempFlags, dynamicBuffer[j], j);
					}
					if (dynamicBuffer.Length >= 2)
					{
						NativeArray<SegmentBuffer> array = new NativeArray<SegmentBuffer>(dynamicBuffer.Length, Allocator.Temp);
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							WaypointDefinition startDefinition = dynamicBuffer[k];
							WaypointDefinition endDefinition = dynamicBuffer[math.select(k + 1, 0, k + 1 == dynamicBuffer.Length)];
							ref SegmentBuffer reference = ref array.ElementAt(k);
							reference.m_Original = GetMatchingSegment(nativeParallelMultiHashMap, prefab, Entity.Null, startDefinition, endDefinition, k);
							reference.m_OldStart = GetMatchingSegment(oldSegments, prefab, creationDefinition.m_Original, startDefinition, endDefinition, k);
							reference.m_OldEnd = reference.m_OldStart;
						}
						for (int l = 0; l < dynamicBuffer.Length; l++)
						{
							WaypointDefinition startDefinition2 = dynamicBuffer[l];
							WaypointDefinition endDefinition2 = dynamicBuffer[math.select(l + 1, 0, l + 1 == dynamicBuffer.Length)];
							ref SegmentBuffer reference2 = ref array.ElementAt(l);
							if (reference2.m_Original == Entity.Null)
							{
								reference2.m_Original = GetPartialSegment(nativeParallelMultiHashMap, prefab, Entity.Null, startDefinition2, endDefinition2, start: true, end: true, l);
							}
							if (reference2.m_OldStart == Entity.Null)
							{
								reference2.m_OldStart = GetPartialSegment(oldSegments, prefab, creationDefinition.m_Original, startDefinition2, endDefinition2, start: true, end: false, l);
								reference2.m_OldEnd = GetPartialSegment(oldSegments, prefab, creationDefinition.m_Original, startDefinition2, endDefinition2, start: false, end: true, l);
							}
							CreateSegment(prefabRouteData, prefab, reference2, tempFlags, l);
						}
						array.Dispose();
					}
					nativeParallelMultiHashMap.Dispose();
					continue;
				}
				RouteData prefabRouteData2 = m_RouteData[prefab];
				int num = dynamicBuffer.Length;
				bool flag = false;
				if (num >= 3 && dynamicBuffer[0].m_Position.Equals(dynamicBuffer[num - 1].m_Position))
				{
					num--;
					flag = true;
				}
				for (int m = 0; m < num; m++)
				{
					CreateWaypoint(prefabRouteData2, prefab, TempFlags.Create, dynamicBuffer[m], m);
				}
				NativeArray<SegmentBuffer> array2 = new NativeArray<SegmentBuffer>(math.max(1, num), Allocator.Temp);
				for (int n = 1; n < num; n++)
				{
					WaypointDefinition startDefinition3 = dynamicBuffer[n - 1];
					WaypointDefinition endDefinition3 = dynamicBuffer[n];
					ref SegmentBuffer reference3 = ref array2.ElementAt(n - 1);
					reference3.m_OldStart = GetMatchingSegment(oldSegments, prefab, Entity.Null, startDefinition3, endDefinition3, n - 1);
					reference3.m_OldEnd = reference3.m_OldStart;
				}
				if (flag)
				{
					WaypointDefinition startDefinition4 = dynamicBuffer[num - 1];
					WaypointDefinition endDefinition4 = dynamicBuffer[0];
					ref SegmentBuffer reference4 = ref array2.ElementAt(num - 1);
					reference4.m_OldStart = GetMatchingSegment(oldSegments, prefab, Entity.Null, startDefinition4, endDefinition4, num - 1);
					reference4.m_OldEnd = reference4.m_OldStart;
				}
				for (int num2 = 1; num2 < num; num2++)
				{
					WaypointDefinition startDefinition5 = dynamicBuffer[num2 - 1];
					WaypointDefinition endDefinition5 = dynamicBuffer[num2];
					ref SegmentBuffer reference5 = ref array2.ElementAt(num2 - 1);
					if (reference5.m_OldStart == Entity.Null)
					{
						reference5.m_OldStart = GetPartialSegment(oldSegments, prefab, Entity.Null, startDefinition5, endDefinition5, start: true, end: false, num2 - 1);
						reference5.m_OldEnd = GetPartialSegment(oldSegments, prefab, Entity.Null, startDefinition5, endDefinition5, start: false, end: true, num2 - 1);
					}
					CreateSegment(prefabRouteData2, prefab, reference5, TempFlags.Create, num2 - 1);
				}
				if (flag)
				{
					WaypointDefinition startDefinition6 = dynamicBuffer[num - 1];
					WaypointDefinition endDefinition6 = dynamicBuffer[0];
					ref SegmentBuffer reference6 = ref array2.ElementAt(num - 1);
					if (reference6.m_OldStart == Entity.Null)
					{
						reference6.m_OldStart = GetPartialSegment(oldSegments, prefab, Entity.Null, startDefinition6, endDefinition6, start: true, end: false, num - 1);
						reference6.m_OldEnd = GetPartialSegment(oldSegments, prefab, Entity.Null, startDefinition6, endDefinition6, start: false, end: true, num - 1);
					}
					CreateSegment(prefabRouteData2, prefab, reference6, TempFlags.Create, num - 1);
				}
				array2.Dispose();
			}
		}

		private Entity GetMatchingSegment(NativeParallelMultiHashMap<SegmentKey, SegmentValue> segments, Entity prefab, Entity originalRoute, WaypointDefinition startDefinition, WaypointDefinition endDefinition, int index)
		{
			SegmentKey key = new SegmentKey(prefab, originalRoute, new float4(startDefinition.m_Position, 0f));
			SegmentKey key2 = new SegmentKey(prefab, originalRoute, new float4(endDefinition.m_Position, 1f));
			Entity entity = Entity.Null;
			int num = 1000000;
			NativeParallelMultiHashMapIterator<SegmentKey> it = default(NativeParallelMultiHashMapIterator<SegmentKey>);
			if (segments.TryGetFirstValue(key, out var item, out var it2))
			{
				do
				{
					if (item.m_EndPosition.Equals(new float4(endDefinition.m_Position, 1f)))
					{
						int num2 = math.abs(index - item.m_Index);
						if (num2 < num)
						{
							entity = item.m_Segment;
							num = num2;
							it = it2;
						}
					}
				}
				while (segments.TryGetNextValue(out item, ref it2));
			}
			if (entity != Entity.Null)
			{
				segments.Remove(it);
				if (segments.TryGetFirstValue(key2, out item, out it2))
				{
					do
					{
						if (item.m_Segment == entity)
						{
							segments.Remove(it2);
							break;
						}
					}
					while (segments.TryGetNextValue(out item, ref it2));
				}
			}
			return entity;
		}

		private Entity GetPartialSegment(NativeParallelMultiHashMap<SegmentKey, SegmentValue> originalSegments, Entity prefab, Entity originalRoute, WaypointDefinition startDefinition, WaypointDefinition endDefinition, bool start, bool end, int index)
		{
			SegmentKey key = new SegmentKey(prefab, originalRoute, new float4(startDefinition.m_Position, 0f));
			SegmentKey key2 = new SegmentKey(prefab, originalRoute, new float4(endDefinition.m_Position, 1f));
			Entity entity = Entity.Null;
			int num = 1000000;
			NativeParallelMultiHashMapIterator<SegmentKey> it = default(NativeParallelMultiHashMapIterator<SegmentKey>);
			SegmentKey key3 = default(SegmentKey);
			if (start && originalSegments.TryGetFirstValue(key, out var item, out var it2))
			{
				do
				{
					int num2 = math.abs(index - item.m_Index);
					if (num2 < num)
					{
						entity = item.m_Segment;
						num = num2;
						it = it2;
						key3 = new SegmentKey(prefab, originalRoute, item.m_EndPosition);
					}
				}
				while (originalSegments.TryGetNextValue(out item, ref it2));
			}
			if (entity == Entity.Null && end && originalSegments.TryGetFirstValue(key2, out item, out it2))
			{
				do
				{
					int num3 = math.abs(index - item.m_Index);
					if (num3 < num)
					{
						entity = item.m_Segment;
						num = num3;
						it = it2;
						key3 = new SegmentKey(prefab, originalRoute, item.m_StartPosition);
					}
				}
				while (originalSegments.TryGetNextValue(out item, ref it2));
			}
			if (entity != Entity.Null)
			{
				originalSegments.Remove(it);
				if (originalSegments.TryGetFirstValue(key3, out item, out it2))
				{
					do
					{
						if (item.m_Segment == entity)
						{
							originalSegments.Remove(it2);
							break;
						}
					}
					while (originalSegments.TryGetNextValue(out item, ref it2));
				}
			}
			return entity;
		}

		private void CreateWaypoint(RouteData prefabRouteData, Entity prefab, TempFlags tempFlags, WaypointDefinition definition, int index)
		{
			Entity e;
			if (definition.m_Connection != Entity.Null)
			{
				e = m_CommandBuffer.CreateEntity(prefabRouteData.m_ConnectedArchetype);
				m_CommandBuffer.SetComponent(e, new Connected(definition.m_Connection));
			}
			else
			{
				e = m_CommandBuffer.CreateEntity(prefabRouteData.m_WaypointArchetype);
			}
			m_CommandBuffer.SetComponent(e, new Waypoint(index));
			m_CommandBuffer.SetComponent(e, new Position(definition.m_Position));
			m_CommandBuffer.SetComponent(e, new PrefabRef(prefab));
			m_CommandBuffer.AddComponent(e, new Temp(definition.m_Original, tempFlags));
		}

		private void CreateSegment(RouteData prefabRouteData, Entity prefab, SegmentBuffer segmentData, TempFlags tempFlags, int index)
		{
			bool2 x = new bool2(segmentData.m_OldStart != Entity.Null, segmentData.m_OldEnd != Entity.Null);
			if (math.all(x) && segmentData.m_OldStart != segmentData.m_OldEnd)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(segmentData.m_OldStart);
				m_CommandBuffer.AddComponent(segmentData.m_OldStart, default(Updated));
				m_CommandBuffer.SetComponent(segmentData.m_OldStart, new Segment(index));
				m_CommandBuffer.SetComponent(segmentData.m_OldStart, new Temp(segmentData.m_Original, tempFlags));
				if (m_PathInformationData.HasComponent(segmentData.m_OldStart) && m_PathInformationData.HasComponent(segmentData.m_OldEnd))
				{
					PathInformation pathInformation = m_PathInformationData[segmentData.m_OldStart];
					PathInformation pathInformation2 = m_PathInformationData[segmentData.m_OldEnd];
					m_CommandBuffer.SetComponent(segmentData.m_OldStart, PathUtils.CombinePaths(pathInformation, pathInformation2));
				}
				bool2 x2 = false;
				if (m_PathElementData.HasBuffer(segmentData.m_OldStart) && m_PathElementData.HasBuffer(segmentData.m_OldEnd))
				{
					DynamicBuffer<PathElement> sourceElements = m_PathElementData[segmentData.m_OldStart];
					DynamicBuffer<PathElement> sourceElements2 = m_PathElementData[segmentData.m_OldEnd];
					DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(segmentData.m_OldStart);
					PathUtils.CombinePaths(sourceElements, sourceElements2, targetElements);
					x2 = new bool2(sourceElements.Length == 0, sourceElements2.Length == 0);
				}
				if (m_PathTargetsData.HasComponent(segmentData.m_OldStart) && m_PathTargetsData.HasComponent(segmentData.m_OldEnd))
				{
					PathTargets component = m_PathTargetsData[segmentData.m_OldStart];
					PathTargets pathTargets = m_PathTargetsData[segmentData.m_OldEnd];
					component.m_StartLane = Entity.Null;
					component.m_EndLane = Entity.Null;
					component.m_CurvePositions = 0f;
					if (math.all(x2))
					{
						component.m_ReadyEndPosition = component.m_ReadyStartPosition;
					}
					else if (x2.x)
					{
						component.m_ReadyStartPosition = pathTargets.m_ReadyStartPosition;
						component.m_ReadyEndPosition = pathTargets.m_ReadyEndPosition;
					}
					else if (!x2.y)
					{
						component.m_ReadyEndPosition = pathTargets.m_ReadyEndPosition;
					}
					m_CommandBuffer.SetComponent(segmentData.m_OldStart, component);
				}
			}
			else if (math.any(x))
			{
				Entity e = (x.x ? segmentData.m_OldStart : segmentData.m_OldEnd);
				m_CommandBuffer.RemoveComponent<Deleted>(e);
				m_CommandBuffer.AddComponent(e, default(Updated));
				m_CommandBuffer.SetComponent(e, new Segment(index));
				m_CommandBuffer.SetComponent(e, new Temp(segmentData.m_Original, tempFlags));
			}
			else
			{
				Entity entity = m_CommandBuffer.CreateEntity(prefabRouteData.m_SegmentArchetype);
				m_CommandBuffer.SetComponent(entity, new Segment(index));
				m_CommandBuffer.SetComponent(entity, new PrefabRef(prefab));
				m_CommandBuffer.AddComponent(entity, new Temp(segmentData.m_Original, tempFlags));
				if (m_PathTargetsData.HasComponent(segmentData.m_Original))
				{
					PathTargets component2 = m_PathTargetsData[segmentData.m_Original];
					m_CommandBuffer.SetComponent(entity, component2);
				}
				if (m_PathInformationData.HasComponent(segmentData.m_Original))
				{
					PathInformation component3 = m_PathInformationData[segmentData.m_Original];
					m_CommandBuffer.SetComponent(entity, component3);
				}
				if (m_PathElementData.HasBuffer(segmentData.m_Original))
				{
					DynamicBuffer<PathElement> dynamicBuffer = m_PathElementData[segmentData.m_Original];
					m_CommandBuffer.SetBuffer<PathElement>(entity).CopyFrom(dynamicBuffer.AsNativeArray());
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Segment> __Game_Routes_Segment_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<WaypointDefinition> __Game_Routes_WaypointDefinition_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathTargets> __Game_Routes_PathTargets_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Segment>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_WaypointDefinition_RO_BufferTypeHandle = state.GetBufferTypeHandle<WaypointDefinition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Segment>(isReadOnly: true);
			__Game_Routes_PathTargets_RO_ComponentLookup = state.GetComponentLookup<PathTargets>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<WaypointDefinition>(), ComponentType.ReadOnly<Updated>());
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Segment>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> definitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<ArchetypeChunk> deletedChunks = m_DeletedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
		JobHandle jobHandle = IJobExtensions.Schedule(new CreateWaypointsJob
		{
			m_DefinitionChunks = definitionChunks,
			m_DeletedChunks = deletedChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SegmentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointDefinitionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_WaypointDefinition_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathTargetsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathTargets_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteSegments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElementData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, outJobHandle2));
		definitionChunks.Dispose(jobHandle);
		deletedChunks.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public GenerateWaypointsSystem()
	{
	}
}
