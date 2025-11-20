using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class SubElementDeleteSystem : GameSystemBase
{
	[BurstCompile]
	private struct DeleteSubElementsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<SubArea> m_SubAreaType;

		[ReadOnly]
		public BufferTypeHandle<SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<SubRoute> m_SubRouteType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<SubArea> bufferAccessor = chunk.GetBufferAccessor(ref m_SubAreaType);
			BufferAccessor<SubNet> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubNetType);
			BufferAccessor<SubRoute> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubRouteType);
			BufferAccessor<OwnedVehicle> bufferAccessor4 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<SubArea> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					SubArea subArea = dynamicBuffer[j];
					if (!m_DeletedData.HasComponent(subArea.m_Area))
					{
						m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, subArea.m_Area, in m_AppliedTypes);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, subArea.m_Area, default(Deleted));
					}
				}
			}
			for (int k = 0; k < bufferAccessor2.Length; k++)
			{
				Entity entity = nativeArray[k];
				DynamicBuffer<SubNet> dynamicBuffer2 = bufferAccessor2[k];
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					SubNet subNet = dynamicBuffer2[l];
					bool flag = true;
					Edge componentData2;
					if (m_ConnectedEdges.TryGetBuffer(subNet.m_SubNet, out var bufferData))
					{
						for (int m = 0; m < bufferData.Length; m++)
						{
							Entity edge = bufferData[m].m_Edge;
							if ((m_OwnerData.TryGetComponent(edge, out var componentData) && (componentData.m_Owner == entity || m_DeletedData.HasComponent(componentData.m_Owner))) || m_DeletedData.HasComponent(edge))
							{
								continue;
							}
							Edge edge2 = m_EdgeData[edge];
							if (edge2.m_Start == subNet.m_SubNet || edge2.m_End == subNet.m_SubNet)
							{
								flag = false;
							}
							if (!m_UpdatedData.HasComponent(edge))
							{
								m_CommandBuffer.AddComponent(unfilteredChunkIndex, edge, default(Updated));
								if (edge2.m_Start != subNet.m_SubNet && !m_UpdatedData.HasComponent(edge2.m_Start) && !m_DeletedData.HasComponent(edge2.m_Start))
								{
									m_CommandBuffer.AddComponent(unfilteredChunkIndex, edge2.m_Start, default(Updated));
								}
								if (edge2.m_End != subNet.m_SubNet && !m_UpdatedData.HasComponent(edge2.m_End) && !m_DeletedData.HasComponent(edge2.m_End))
								{
									m_CommandBuffer.AddComponent(unfilteredChunkIndex, edge2.m_End, default(Updated));
								}
							}
						}
					}
					else if (m_EdgeData.TryGetComponent(subNet.m_SubNet, out componentData2))
					{
						UpdateConnectedNodes(unfilteredChunkIndex, entity, componentData2.m_Start);
						UpdateConnectedNodes(unfilteredChunkIndex, entity, componentData2.m_End);
					}
					if (!m_DeletedData.HasComponent(subNet.m_SubNet))
					{
						if (flag)
						{
							m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, subNet.m_SubNet, in m_AppliedTypes);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subNet.m_SubNet, default(Deleted));
						}
						else if (!m_UpdatedData.HasComponent(subNet.m_SubNet))
						{
							m_CommandBuffer.RemoveComponent<Owner>(unfilteredChunkIndex, subNet.m_SubNet);
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, subNet.m_SubNet, default(Updated));
						}
					}
				}
			}
			for (int n = 0; n < bufferAccessor3.Length; n++)
			{
				DynamicBuffer<SubRoute> dynamicBuffer3 = bufferAccessor3[n];
				for (int num = 0; num < dynamicBuffer3.Length; num++)
				{
					SubRoute subRoute = dynamicBuffer3[num];
					if (!m_DeletedData.HasComponent(subRoute.m_Route))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, subRoute.m_Route, default(Deleted));
					}
				}
			}
			for (int num2 = 0; num2 < bufferAccessor4.Length; num2++)
			{
				DynamicBuffer<OwnedVehicle> dynamicBuffer4 = bufferAccessor4[num2];
				for (int num3 = 0; num3 < dynamicBuffer4.Length; num3++)
				{
					OwnedVehicle ownedVehicle = dynamicBuffer4[num3];
					if (!m_DeletedData.HasComponent(ownedVehicle.m_Vehicle))
					{
						m_LayoutElements.TryGetBuffer(ownedVehicle.m_Vehicle, out var bufferData2);
						VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, ownedVehicle.m_Vehicle, bufferData2);
					}
				}
			}
		}

		private void UpdateConnectedNodes(int jobIndex, Entity entity, Entity node)
		{
			if ((m_OwnerData.TryGetComponent(node, out var componentData) && (componentData.m_Owner == entity || m_DeletedData.HasComponent(componentData.m_Owner))) || m_UpdatedData.HasComponent(node) || m_DeletedData.HasComponent(node))
			{
				return;
			}
			m_CommandBuffer.AddComponent(jobIndex, node, default(Updated));
			if (!m_ConnectedEdges.TryGetBuffer(node, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity edge = bufferData[i].m_Edge;
				if ((!m_OwnerData.TryGetComponent(edge, out componentData) || (!(componentData.m_Owner == entity) && !m_DeletedData.HasComponent(componentData.m_Owner))) && !m_UpdatedData.HasComponent(edge) && !m_DeletedData.HasComponent(edge))
				{
					m_CommandBuffer.AddComponent(jobIndex, edge, default(Updated));
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start != node && !m_UpdatedData.HasComponent(edge2.m_Start) && !m_DeletedData.HasComponent(edge2.m_Start))
					{
						m_CommandBuffer.AddComponent(jobIndex, edge2.m_Start, default(Updated));
					}
					if (edge2.m_End != node && !m_UpdatedData.HasComponent(edge2.m_End) && !m_DeletedData.HasComponent(edge2.m_End))
					{
						m_CommandBuffer.AddComponent(jobIndex, edge2.m_End, default(Updated));
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
	private struct CheckDeletedOwnersJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Owner owner = nativeArray2[i];
				if (m_DeletedData.HasComponent(owner.m_Owner))
				{
					CollectionUtils.TryGet(bufferAccessor, i, out var value);
					VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, nativeArray[i], value);
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
		public BufferTypeHandle<SubArea> __Game_Areas_SubArea_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubRoute> __Game_Routes_SubRoute_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_SubArea_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubArea>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubNet>(isReadOnly: true);
			__Game_Routes_SubRoute_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubRoute>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
		}
	}

	private ToolReadyBarrier m_ModificationBarrier;

	private EntityQuery m_DeletedQuery;

	private EntityQuery m_CreatedQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ToolReadyBarrier>();
		m_DeletedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<SubArea>(),
				ComponentType.ReadOnly<SubNet>(),
				ComponentType.ReadOnly<SubRoute>(),
				ComponentType.ReadOnly<OwnedVehicle>()
			}
		});
		m_CreatedQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<Owner>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_DeletedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new DeleteSubElementsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SubAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubRouteType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_SubRoute_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_DeletedQuery, base.Dependency);
		if (!m_CreatedQuery.IsEmptyIgnoreFilter)
		{
			CheckDeletedOwnersJob jobData = new CheckDeletedOwnersJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			jobHandle = JobHandle.CombineDependencies(jobHandle, JobChunkExtensions.ScheduleParallel(jobData, m_CreatedQuery, base.Dependency));
		}
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
	public SubElementDeleteSystem()
	{
	}
}
