using System.Runtime.CompilerServices;
using Game.Common;
using Game.Pathfind;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ServiceRequestSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateRequestGroupJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RequestGroup> m_RequestGroupType;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<RequestGroup> nativeArray2 = chunk.GetNativeArray(ref m_RequestGroupType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity e = nativeArray[i];
				uint index = random.NextUInt(nativeArray2[i].m_GroupCount);
				m_CommandBuffer.RemoveComponent<RequestGroup>(unfilteredChunkIndex, e);
				m_CommandBuffer.AddSharedComponent(unfilteredChunkIndex, e, new UpdateFrame(index));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleRequestJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<HandleRequest> m_HandleRequestType;

		public ComponentLookup<Dispatched> m_DispatchedData;

		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, HandleRequest> nativeParallelHashMap = new NativeParallelHashMap<Entity, HandleRequest>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<HandleRequest> nativeArray = m_Chunks[j].GetNativeArray(ref m_HandleRequestType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					HandleRequest handleRequest = nativeArray[k];
					if (nativeParallelHashMap.TryGetValue(handleRequest.m_Request, out var item))
					{
						if (handleRequest.m_Completed)
						{
							nativeParallelHashMap[handleRequest.m_Request] = handleRequest;
						}
						else if (handleRequest.m_PathConsumed)
						{
							item.m_PathConsumed = true;
							nativeParallelHashMap[handleRequest.m_Request] = item;
						}
					}
					else
					{
						nativeParallelHashMap.Add(handleRequest.m_Request, handleRequest);
					}
				}
			}
			NativeParallelHashMap<Entity, HandleRequest>.Enumerator enumerator = nativeParallelHashMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				HandleRequest value = enumerator.Current.Value;
				if (!m_ServiceRequestData.HasComponent(value.m_Request))
				{
					continue;
				}
				if (value.m_Completed)
				{
					m_CommandBuffer.DestroyEntity(value.m_Request);
				}
				else if (value.m_Handler != Entity.Null)
				{
					if (m_DispatchedData.HasComponent(value.m_Request))
					{
						m_DispatchedData[value.m_Request] = new Dispatched(value.m_Handler);
						ServiceRequest value2 = m_ServiceRequestData[value.m_Request];
						value2.m_Cooldown = 0;
						m_ServiceRequestData[value.m_Request] = value2;
					}
					else
					{
						m_CommandBuffer.AddComponent(value.m_Request, new Dispatched(value.m_Handler));
					}
					if (value.m_PathConsumed)
					{
						m_CommandBuffer.RemoveComponent<PathInformation>(value.m_Request);
						m_CommandBuffer.RemoveComponent<PathElement>(value.m_Request);
					}
				}
				else if (m_DispatchedData.HasComponent(value.m_Request))
				{
					ServiceRequest serviceRequest = m_ServiceRequestData[value.m_Request];
					SimulationUtils.ResetFailedRequest(ref serviceRequest);
					m_ServiceRequestData[value.m_Request] = serviceRequest;
					m_CommandBuffer.RemoveComponent<PathInformation>(value.m_Request);
					m_CommandBuffer.RemoveComponent<PathElement>(value.m_Request);
					m_CommandBuffer.RemoveComponent<Dispatched>(value.m_Request);
				}
			}
			enumerator.Dispose();
			nativeParallelHashMap.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RequestGroup> __Game_Simulation_RequestGroup_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HandleRequest> __Game_Simulation_HandleRequest_RO_ComponentTypeHandle;

		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RW_ComponentLookup;

		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_RequestGroup_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RequestGroup>(isReadOnly: true);
			__Game_Simulation_HandleRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HandleRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RW_ComponentLookup = state.GetComponentLookup<Dispatched>();
			__Game_Simulation_ServiceRequest_RW_ComponentLookup = state.GetComponentLookup<ServiceRequest>();
		}
	}

	private ModificationEndBarrier m_ModificationBarrier;

	private EntityQuery m_RequestGroupQuery;

	private EntityQuery m_HandleRequestQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_RequestGroupQuery = GetEntityQuery(ComponentType.ReadOnly<RequestGroup>());
		m_HandleRequestQuery = GetEntityQuery(ComponentType.ReadOnly<HandleRequest>());
		RequireAnyForUpdate(m_RequestGroupQuery, m_HandleRequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_RequestGroupQuery.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateRequestGroupJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RequestGroupType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_RequestGroup_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RandomSeed = RandomSeed.Next(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_RequestGroupQuery, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			base.Dependency = jobHandle;
		}
		if (!m_HandleRequestQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			JobHandle jobHandle2 = IJobExtensions.Schedule(new HandleRequestJob
			{
				m_HandleRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_HandleRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Chunks = m_HandleRequestQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
			}, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			base.Dependency = jobHandle2;
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
	public ServiceRequestSystem()
	{
	}
}
