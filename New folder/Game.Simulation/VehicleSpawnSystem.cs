using System;
using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class VehicleSpawnSystem : GameSystemBase
{
	private struct SpawnData : IComparable<SpawnData>
	{
		public Entity m_Source;

		public Entity m_Vehicle;

		public int m_Priority;

		public int CompareTo(SpawnData other)
		{
			return math.select(m_Priority - other.m_Priority, m_Source.Index - other.m_Source.Index, m_Source.Index != other.m_Source.Index);
		}
	}

	private struct SpawnRange
	{
		public int m_Start;

		public int m_End;
	}

	[BurstCompile]
	private struct GroupSpawnSourcesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		public ComponentTypeHandle<TripSource> m_SpawnSourceType;

		public NativeList<SpawnData> m_SpawnData;

		public NativeList<SpawnRange> m_SpawnGroups;

		public void Execute()
		{
			SpawnData value = default(SpawnData);
			SpawnData value4 = default(SpawnData);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<TripSource> nativeArray2 = archetypeChunk.GetNativeArray(ref m_SpawnSourceType);
				NativeArray<Controller> nativeArray3 = archetypeChunk.GetNativeArray(ref m_ControllerType);
				if (nativeArray3.Length != 0)
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						value.m_Vehicle = nativeArray[j];
						Controller controller = nativeArray3[j];
						if (!(controller.m_Controller != Entity.Null) || !(controller.m_Controller != value.m_Vehicle))
						{
							TripSource value2 = nativeArray2[j];
							if (value2.m_Timer <= 0)
							{
								value.m_Source = nativeArray2[j].m_Source;
								value.m_Priority = value2.m_Timer;
								m_SpawnData.Add(in value);
							}
							value2.m_Timer -= 16;
							nativeArray2[j] = value2;
						}
					}
					continue;
				}
				for (int k = 0; k < nativeArray.Length; k++)
				{
					TripSource value3 = nativeArray2[k];
					if (value3.m_Timer <= 0)
					{
						value4.m_Source = nativeArray2[k].m_Source;
						value4.m_Vehicle = nativeArray[k];
						value4.m_Priority = value3.m_Timer;
						m_SpawnData.Add(in value4);
					}
					value3.m_Timer -= 16;
					nativeArray2[k] = value3;
				}
			}
			m_SpawnData.Sort();
			SpawnRange value5 = default(SpawnRange);
			value5.m_Start = -1;
			Entity entity = Entity.Null;
			for (int l = 0; l < m_SpawnData.Length; l++)
			{
				Entity entity2 = m_SpawnData[l].m_Source;
				if (entity2 != entity)
				{
					if (value5.m_Start != -1)
					{
						value5.m_End = l;
						m_SpawnGroups.Add(in value5);
					}
					value5.m_Start = l;
					entity = entity2;
				}
			}
			if (value5.m_Start != -1)
			{
				value5.m_End = m_SpawnData.Length;
				m_SpawnGroups.Add(in value5);
			}
		}
	}

	private struct LaneBufferItem
	{
		public Entity m_Lane;

		public float2 m_Delta;

		public LaneBufferItem(Entity lane, float2 delta)
		{
			m_Lane = lane;
			m_Delta = delta;
		}
	}

	[BurstCompile]
	private struct TrySpawnVehiclesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<SpawnData> m_SpawnData;

		[ReadOnly]
		public NativeArray<SpawnRange> m_SpawnGroups;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_Layouts;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			SpawnRange spawnRange = m_SpawnGroups[index];
			_ = m_SpawnData[spawnRange.m_Start];
			Entity lastRemoved = Entity.Null;
			for (int i = spawnRange.m_Start; i < spawnRange.m_End; i++)
			{
				Entity entity = m_SpawnData[i].m_Vehicle;
				if (m_Layouts.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
				{
					bool flag = true;
					for (int j = 0; j < bufferData.Length; j++)
					{
						flag &= CheckSpaceForVehicle(index, entity, bufferData[j].m_Vehicle, ref lastRemoved);
					}
					if (flag)
					{
						for (int k = 0; k < bufferData.Length; k++)
						{
							m_CommandBuffer.RemoveComponent<TripSource>(index, bufferData[k].m_Vehicle);
						}
					}
				}
				else if (CheckSpaceForVehicle(index, entity, entity, ref lastRemoved))
				{
					m_CommandBuffer.RemoveComponent<TripSource>(index, entity);
				}
			}
		}

		private bool CheckSpaceForVehicle(int jobIndex, Entity vehicle, Entity vehicle2, ref Entity lastRemoved)
		{
			bool flag = true;
			if (m_TrainCurrentLaneData.TryGetComponent(vehicle2, out var componentData))
			{
				flag &= CheckSpaceForTrain(jobIndex, vehicle, componentData.m_Front.m_Lane, ref lastRemoved);
				if (componentData.m_Rear.m_Lane != componentData.m_Front.m_Lane)
				{
					flag &= CheckSpaceForTrain(jobIndex, vehicle, componentData.m_Rear.m_Lane, ref lastRemoved);
				}
			}
			return flag;
		}

		private bool CheckSpaceForTrain(int jobIndex, Entity vehicle, Entity lane, ref Entity lastRemoved)
		{
			bool result = true;
			if (m_LaneObjects.TryGetBuffer(lane, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					LaneObject laneObject = bufferData[i];
					Entity entity = Entity.Null;
					if (m_ParkedTrainData.HasComponent(laneObject.m_LaneObject))
					{
						entity = laneObject.m_LaneObject;
						if (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData))
						{
							entity = componentData.m_Controller;
						}
					}
					if (entity != Entity.Null && entity != vehicle && lastRemoved != entity)
					{
						m_Layouts.TryGetBuffer(entity, out var bufferData2);
						VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity, bufferData2);
						lastRemoved = entity;
					}
				}
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Controller> __Game_Vehicles_Controller_RO_ComponentTypeHandle;

		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Controller_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Controller>(isReadOnly: true);
			__Game_Objects_TripSource_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>();
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<TripSource>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeList<SpawnData> spawnData = new NativeList<SpawnData>(Allocator.TempJob);
		NativeList<SpawnRange> nativeList = new NativeList<SpawnRange>(Allocator.TempJob);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_VehicleQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		GroupSpawnSourcesJob jobData = new GroupSpawnSourcesJob
		{
			m_Chunks = chunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ControllerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnData = spawnData,
			m_SpawnGroups = nativeList
		};
		JobHandle jobHandle = new TrySpawnVehiclesJob
		{
			m_SpawnData = spawnData.AsDeferredJobArray(),
			m_SpawnGroups = nativeList.AsDeferredJobArray(),
			m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Layouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}.Schedule(dependsOn: IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle)), list: nativeList, innerloopBatchCount: 1);
		spawnData.Dispose(jobHandle);
		nativeList.Dispose(jobHandle);
		chunks.Dispose(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public VehicleSpawnSystem()
	{
	}
}
