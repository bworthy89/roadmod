using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class RoutePathSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct RoutePathType : IEquatable<RoutePathType>
	{
		public RouteConnectionType m_ConnectionType;

		public RoadTypes m_RoadType;

		public TrackTypes m_TrackType;

		public bool Equals(RoutePathType other)
		{
			if (m_ConnectionType == other.m_ConnectionType && m_RoadType == other.m_RoadType)
			{
				return m_TrackType == other.m_TrackType;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)((uint)((int)m_ConnectionType << 16) | ((uint)m_RoadType << 8)) | (int)m_TrackType;
		}
	}

	[BurstCompile]
	private struct CheckRoutePathsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public NativeQueue<Entity>.ParallelWriter m_UpdateQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (m_DeletedData.HasComponent(dynamicBuffer[j].m_Target))
					{
						m_UpdateQueue.Enqueue(nativeArray[i]);
						break;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckAppliedLanesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> m_CarLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> m_TrackLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> m_PedestrianLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_TrackLaneData;

		public NativeParallelHashSet<RoutePathType> m_PathTypeSet;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_CarLaneType))
			{
				NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					PrefabRef prefabRef = nativeArray[i];
					CarLaneData carLaneData = m_CarLaneData[prefabRef.m_Prefab];
					if ((carLaneData.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Road,
							m_RoadType = RoadTypes.Car
						});
					}
					if ((carLaneData.m_RoadTypes & RoadTypes.Watercraft) != RoadTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Road,
							m_RoadType = RoadTypes.Watercraft
						});
					}
					if ((carLaneData.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Road,
							m_RoadType = RoadTypes.Helicopter
						});
					}
					if ((carLaneData.m_RoadTypes & RoadTypes.Airplane) != RoadTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Road,
							m_RoadType = RoadTypes.Airplane
						});
					}
				}
			}
			if (chunk.Has(ref m_TrackLaneType))
			{
				NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					PrefabRef prefabRef2 = nativeArray2[j];
					TrackLaneData trackLaneData = m_TrackLaneData[prefabRef2.m_Prefab];
					if ((trackLaneData.m_TrackTypes & TrackTypes.Train) != TrackTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Track,
							m_TrackType = TrackTypes.Train
						});
					}
					if ((trackLaneData.m_TrackTypes & TrackTypes.Tram) != TrackTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Track,
							m_TrackType = TrackTypes.Tram
						});
					}
					if ((trackLaneData.m_TrackTypes & TrackTypes.Subway) != TrackTypes.None)
					{
						m_PathTypeSet.Add(new RoutePathType
						{
							m_ConnectionType = RouteConnectionType.Track,
							m_TrackType = TrackTypes.Subway
						});
					}
				}
			}
			if (chunk.Has(ref m_PedestrianLaneType))
			{
				m_PathTypeSet.Add(new RoutePathType
				{
					m_ConnectionType = RouteConnectionType.Pedestrian
				});
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckSegmentRoutes : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_RouteConnectionData;

		[ReadOnly]
		public NativeParallelHashSet<RoutePathType> m_PathTypeSet;

		public NativeQueue<Entity>.ParallelWriter m_UpdateQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = m_PrefabRefData[owner.m_Owner];
				if (m_RouteConnectionData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					RoutePathType item = new RoutePathType
					{
						m_ConnectionType = componentData.m_RouteConnectionType,
						m_RoadType = componentData.m_RouteRoadType,
						m_TrackType = componentData.m_RouteTrackType
					};
					if (m_PathTypeSet.Contains(item))
					{
						m_UpdateQueue.Enqueue(nativeArray[i]);
					}
				}
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
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Segment> __Game_Routes_Segment_RO_ComponentTypeHandle;

		public ComponentTypeHandle<PathTargets> __Game_Routes_PathTargets_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Segment>(isReadOnly: true);
			__Game_Routes_PathTargets_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathTargets>();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
		}
	}

	private PathfindQueueSystem m_PathfindQueueSystem;

	private EntityQuery m_UpdatedSegmentQuery;

	private EntityQuery m_DeletedLaneQuery;

	private EntityQuery m_AppliedLaneQuery;

	private EntityQuery m_SegmentQuery;

	private NativeParallelHashSet<Entity> m_LazyUpdateSet;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_UpdatedSegmentQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Segment>(), ComponentType.ReadWrite<PathTargets>());
		m_DeletedLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Lane>(), ComponentType.Exclude<Temp>());
		m_AppliedLaneQuery = GetEntityQuery(ComponentType.ReadOnly<Applied>(), ComponentType.ReadOnly<Lane>());
		m_SegmentQuery = GetEntityQuery(ComponentType.ReadOnly<Segment>(), ComponentType.ReadOnly<PathElement>(), ComponentType.Exclude<Deleted>());
		m_LazyUpdateSet = new NativeParallelHashSet<Entity>(20, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LazyUpdateSet.Dispose();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_LazyUpdateSet.Count();
		writer.Write(value);
		NativeParallelHashSet<Entity>.Enumerator enumerator = m_LazyUpdateSet.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Entity current = enumerator.Current;
			writer.Write(current);
		}
		enumerator.Dispose();
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_LazyUpdateSet.Clear();
		reader.Read(out int value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			if (value2 != Entity.Null)
			{
				m_LazyUpdateSet.Add(value2);
			}
		}
	}

	public void SetDefaults(Context context)
	{
		m_LazyUpdateSet.Clear();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_DeletedLaneQuery.IsEmptyIgnoreFilter && !m_SegmentQuery.IsEmptyIgnoreFilter;
		bool flag2 = !m_AppliedLaneQuery.IsEmptyIgnoreFilter && !m_SegmentQuery.IsEmptyIgnoreFilter;
		bool flag3 = !m_UpdatedSegmentQuery.IsEmptyIgnoreFilter;
		if (!flag && !flag2 && !flag3 && m_LazyUpdateSet.IsEmpty)
		{
			return;
		}
		NativeQueue<Entity> nativeQueue = default(NativeQueue<Entity>);
		NativeQueue<Entity> nativeQueue2 = default(NativeQueue<Entity>);
		NativeParallelHashSet<Entity> nativeParallelHashSet = default(NativeParallelHashSet<Entity>);
		JobHandle jobHandle = default(JobHandle);
		JobHandle jobHandle2 = default(JobHandle);
		if (flag)
		{
			nativeQueue = new NativeQueue<Entity>(Allocator.TempJob);
			jobHandle = JobChunkExtensions.ScheduleParallel(new CheckRoutePathsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdateQueue = nativeQueue.AsParallelWriter()
			}, m_SegmentQuery, base.Dependency);
			JobHandle.ScheduleBatchedJobs();
		}
		if (flag2)
		{
			NativeParallelHashSet<RoutePathType> pathTypeSet = new NativeParallelHashSet<RoutePathType>(10, Allocator.TempJob);
			nativeQueue2 = new NativeQueue<Entity>(Allocator.TempJob);
			CheckAppliedLanesJob jobData = new CheckAppliedLanesJob
			{
				m_CarLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TrackLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PedestrianLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathTypeSet = pathTypeSet
			};
			JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(new CheckSegmentRoutes
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathTypeSet = pathTypeSet,
				m_UpdateQueue = nativeQueue2.AsParallelWriter()
			}, dependsOn: JobChunkExtensions.Schedule(jobData, m_AppliedLaneQuery, base.Dependency), query: m_SegmentQuery);
			pathTypeSet.Dispose(jobHandle3);
			jobHandle2 = jobHandle3;
			JobHandle.ScheduleBatchedJobs();
		}
		if (flag || flag3)
		{
			nativeParallelHashSet = new NativeParallelHashSet<Entity>(10, Allocator.Temp);
		}
		if (flag3)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_UpdatedSegmentQuery.ToArchetypeChunkArray(Allocator.TempJob);
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Segment> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Owner> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PathTargets> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_PathTargets_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Temp> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<Segment> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<Owner> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
				NativeArray<PathTargets> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle3);
				NativeArray<PrefabRef> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle5);
				bool highPriority = archetypeChunk.Has(ref typeHandle4);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity = nativeArray2[j];
					Segment segment = nativeArray3[j];
					Owner owner = nativeArray4[j];
					PathTargets pathTargets = nativeArray5[j];
					PrefabRef prefabRef = nativeArray6[j];
					if (SetupPathfind(entity, segment, ref pathTargets, owner, prefabRef, highPriority))
					{
						nativeParallelHashSet.Add(entity);
						m_LazyUpdateSet.Remove(entity);
						nativeArray5[j] = pathTargets;
					}
				}
			}
			nativeArray.Dispose();
		}
		if (flag)
		{
			jobHandle.Complete();
			Entity item;
			while (nativeQueue.TryDequeue(out item))
			{
				if (nativeParallelHashSet.Add(item))
				{
					m_LazyUpdateSet.Remove(item);
					Segment componentData = base.EntityManager.GetComponentData<Segment>(item);
					Owner componentData2 = base.EntityManager.GetComponentData<Owner>(item);
					PrefabRef componentData3 = base.EntityManager.GetComponentData<PrefabRef>(item);
					PathTargets pathTargets2 = new PathTargets
					{
						m_StartLane = 
						{
							Index = -1
						}
					};
					SetupPathfind(item, componentData, ref pathTargets2, componentData2, componentData3, highPriority: false);
				}
			}
		}
		if (flag2)
		{
			jobHandle2.Complete();
			Entity item2;
			while (nativeQueue2.TryDequeue(out item2))
			{
				if (!nativeParallelHashSet.IsCreated || !nativeParallelHashSet.Contains(item2))
				{
					m_LazyUpdateSet.Add(item2);
				}
			}
		}
		if (!m_LazyUpdateSet.IsEmpty && (!nativeParallelHashSet.IsCreated || nativeParallelHashSet.IsEmpty))
		{
			NativeParallelHashSet<Entity>.Enumerator enumerator = m_LazyUpdateSet.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Entity current = enumerator.Current;
				m_LazyUpdateSet.Remove(current);
				enumerator.Dispose();
				enumerator = m_LazyUpdateSet.GetEnumerator();
				if (base.EntityManager.TryGetComponent<Segment>(current, out var component) && !base.EntityManager.HasComponent<Deleted>(current))
				{
					Owner componentData4 = base.EntityManager.GetComponentData<Owner>(current);
					PrefabRef componentData5 = base.EntityManager.GetComponentData<PrefabRef>(current);
					PathTargets pathTargets3 = new PathTargets
					{
						m_StartLane = 
						{
							Index = -1
						}
					};
					SetupPathfind(current, component, ref pathTargets3, componentData4, componentData5, highPriority: false);
					break;
				}
			}
			enumerator.Dispose();
		}
		if (nativeQueue.IsCreated)
		{
			nativeQueue.Dispose();
		}
		if (nativeQueue2.IsCreated)
		{
			nativeQueue2.Dispose();
		}
		if (nativeParallelHashSet.IsCreated)
		{
			nativeParallelHashSet.Dispose();
		}
	}

	private bool SetupPathfind(Entity entity, Segment segment, ref PathTargets pathTargets, Owner owner, PrefabRef prefabRef, bool highPriority)
	{
		RouteData componentData = base.EntityManager.GetComponentData<RouteData>(prefabRef.m_Prefab);
		RouteConnectionData componentData2 = base.EntityManager.GetComponentData<RouteConnectionData>(prefabRef.m_Prefab);
		PathfindParameters parameters = new PathfindParameters
		{
			m_MaxSpeed = 277.77777f,
			m_WalkSpeed = 5.555556f,
			m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
			m_PathfindFlags = (PathfindFlags.Stable | PathfindFlags.IgnoreFlow),
			m_IgnoredRules = (RuleFlags.HasBlockage | RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles),
			m_Methods = RouteUtils.GetPathMethods(componentData2.m_RouteConnectionType, componentData.m_Type, componentData2.m_RouteTrackType, componentData2.m_RouteRoadType, componentData2.m_RouteSizeClass)
		};
		if (componentData2.m_RouteConnectionType != RouteConnectionType.Road || componentData2.m_RouteRoadType != RoadTypes.Car)
		{
			parameters.m_IgnoredRules |= RuleFlags.ForbidTransitTraffic;
		}
		PathEventData eventData = default(PathEventData);
		PathfindAction pathfindAction = default(PathfindAction);
		if (base.EntityManager.HasComponent<VerifiedPath>(entity))
		{
			Owner component;
			while (base.EntityManager.TryGetComponent<Owner>(owner.m_Owner, out component))
			{
				owner = component;
			}
			bool flag = segment.m_Index == 1;
			if (flag)
			{
				parameters.m_PathfindFlags |= PathfindFlags.ForceForward;
			}
			else
			{
				parameters.m_PathfindFlags |= PathfindFlags.ForceBackward;
			}
			pathfindAction = new PathfindAction(10, 10, Allocator.Persistent, parameters, SetupTargetType.None, SetupTargetType.None);
			pathfindAction.data.m_StartTargets.Length = 0;
			pathfindAction.data.m_EndTargets.Length = 0;
			AddBuilding(owner.m_Owner, pathfindAction, componentData2, flag, isSubBuilding: false);
			if (base.EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					AddBuilding(buffer[i].m_Upgrade, pathfindAction, componentData2, flag, isSubBuilding: true);
				}
			}
		}
		else
		{
			if (!base.EntityManager.TryGetBuffer(owner.m_Owner, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer2))
			{
				return false;
			}
			int num = segment.m_Index + 1;
			if (num == buffer2.Length)
			{
				num = 0;
			}
			Entity waypoint = buffer2[segment.m_Index].m_Waypoint;
			Entity waypoint2 = buffer2[num].m_Waypoint;
			if (!base.EntityManager.TryGetComponent<RouteLane>(waypoint, out var component2))
			{
				return false;
			}
			if (!base.EntityManager.TryGetComponent<RouteLane>(waypoint2, out var component3))
			{
				return false;
			}
			float2 @float = new float2(component2.m_EndCurvePos, component3.m_StartCurvePos);
			if (pathTargets.m_StartLane == component2.m_EndLane && pathTargets.m_EndLane == component3.m_StartLane && math.all(math.abs(pathTargets.m_CurvePositions - @float) < 0.001f))
			{
				return false;
			}
			pathTargets.m_StartLane = component2.m_EndLane;
			pathTargets.m_EndLane = component3.m_StartLane;
			pathTargets.m_CurvePositions = @float;
			eventData.m_Position1 = base.EntityManager.GetComponentData<Position>(waypoint).m_Position;
			eventData.m_Position2 = base.EntityManager.GetComponentData<Position>(waypoint2).m_Position;
			pathfindAction = new PathfindAction(1, 1, Allocator.Persistent, parameters, SetupTargetType.None, SetupTargetType.None);
			pathfindAction.data.m_StartTargets[0] = new PathTarget(component2.m_EndLane, component2.m_EndLane, component2.m_EndCurvePos, 0f);
			pathfindAction.data.m_EndTargets[0] = new PathTarget(component3.m_StartLane, component3.m_StartLane, component3.m_StartCurvePos, 0f);
		}
		m_PathfindQueueSystem.Enqueue(pathfindAction, entity, default(JobHandle), uint.MaxValue, this, eventData, highPriority);
		return true;
	}

	private void AddBuilding(Entity building, PathfindAction query, RouteConnectionData routeConnection, bool insideOut, bool isSubBuilding)
	{
		if (!base.EntityManager.TryGetComponent<PrefabRef>(building, out var component))
		{
			return;
		}
		if (base.EntityManager.HasComponent<GateData>(component.m_Prefab))
		{
			if (!base.EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<Game.Net.SubNet> buffer))
			{
				return;
			}
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity subNet = buffer[i].m_SubNet;
				if (!base.EntityManager.HasComponent<Game.Net.Gate>(subNet))
				{
					continue;
				}
				Composition componentData = base.EntityManager.GetComponentData<Composition>(subNet);
				bool flag = (base.EntityManager.GetComponentData<NetCompositionData>(componentData.m_Edge).m_Flags.m_Right & CompositionFlags.Side.Gate) != 0;
				if (!base.EntityManager.TryGetBuffer(subNet, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer2))
				{
					continue;
				}
				for (int j = 0; j < buffer2.Length; j++)
				{
					Game.Net.SubLane subLane = buffer2[j];
					if ((subLane.m_PathMethods & PathMethod.Road) == 0 || !base.EntityManager.TryGetComponent<EdgeLane>(subLane.m_SubLane, out var component2) || base.EntityManager.HasComponent<SlaveLane>(subLane.m_SubLane))
					{
						continue;
					}
					bool4 x = component2.m_EdgeDelta.xxyy == new float4(0f, 1f, 0f, 1f);
					if (!math.any(x))
					{
						continue;
					}
					if (flag == (x.x | x.w) != insideOut)
					{
						EdgeFlags flags = ~(EdgeFlags.Forward | EdgeFlags.Backward | EdgeFlags.AllowMiddle | EdgeFlags.Secondary);
						if (insideOut && (x.x | x.y))
						{
							query.data.m_StartTargets.Add(new PathTarget(building, subLane.m_SubLane, 0f, 0f, flags));
						}
						else if (!insideOut && (x.z | x.w))
						{
							query.data.m_EndTargets.Add(new PathTarget(building, subLane.m_SubLane, 1f, 0f, flags));
						}
					}
					else
					{
						if (x.x | x.y)
						{
							query.data.m_EndTargets.Add(new PathTarget(building, subLane.m_SubLane, 0f, 0f));
						}
						if (x.z | x.w)
						{
							query.data.m_StartTargets.Add(new PathTarget(building, subLane.m_SubLane, 1f, 0f));
						}
					}
				}
			}
		}
		else
		{
			if (!base.EntityManager.TryGetComponent<Building>(building, out var component3))
			{
				return;
			}
			bool flag2 = false;
			if (base.EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<SpawnLocationElement> buffer3))
			{
				for (int k = 0; k < buffer3.Length; k++)
				{
					SpawnLocationElement spawnLocationElement = buffer3[k];
					if (spawnLocationElement.m_Type != SpawnLocationType.SpawnLocation || !base.EntityManager.HasComponent<Game.Objects.SpawnLocation>(spawnLocationElement.m_SpawnLocation))
					{
						continue;
					}
					PrefabRef componentData2 = base.EntityManager.GetComponentData<PrefabRef>(spawnLocationElement.m_SpawnLocation);
					if (base.EntityManager.TryGetComponent<SpawnLocationData>(componentData2.m_Prefab, out var component4) && (component4.m_ConnectionType == routeConnection.m_RouteConnectionType || component4.m_ConnectionType == RouteConnectionType.Cargo) && (component4.m_RoadTypes & routeConnection.m_RouteRoadType) != RoadTypes.None)
					{
						if (insideOut)
						{
							query.data.m_StartTargets.Add(new PathTarget(building, spawnLocationElement.m_SpawnLocation, 1f, 0f));
						}
						else
						{
							query.data.m_EndTargets.Add(new PathTarget(building, spawnLocationElement.m_SpawnLocation, 1f, 0f));
						}
						flag2 = true;
					}
				}
			}
			if (!isSubBuilding || flag2 || !base.EntityManager.TryGetBuffer(component3.m_RoadEdge, isReadOnly: true, out DynamicBuffer<Game.Net.SubLane> buffer4))
			{
				return;
			}
			for (int l = 0; l < buffer4.Length; l++)
			{
				Game.Net.SubLane subLane2 = buffer4[l];
				if ((subLane2.m_PathMethods & PathMethod.Road) != 0 && base.EntityManager.TryGetComponent<EdgeLane>(subLane2.m_SubLane, out var component5) && !base.EntityManager.HasComponent<SlaveLane>(subLane2.m_SubLane) && component3.m_CurvePosition >= math.cmin(component5.m_EdgeDelta) && component3.m_CurvePosition <= math.cmax(component5.m_EdgeDelta))
				{
					float delta = math.saturate((component3.m_CurvePosition - component5.m_EdgeDelta.x) / (component5.m_EdgeDelta.y - component5.m_EdgeDelta.x));
					if (insideOut)
					{
						query.data.m_StartTargets.Add(new PathTarget(building, subLane2.m_SubLane, delta, 0f));
					}
					else
					{
						query.data.m_EndTargets.Add(new PathTarget(building, subLane2.m_SubLane, delta, 0f));
					}
				}
			}
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
	public RoutePathSystem()
	{
	}
}
