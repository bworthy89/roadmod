using System.Runtime.CompilerServices;
using Game.Net;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ConnectedEdgeSystem : GameSystemBase
{
	[BurstCompile]
	private struct ConnectedEdgeJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Edge> nativeArray2 = chunk.GetNativeArray(ref m_EdgeType);
			BufferAccessor<ConnectedNode> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedNodeType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity edge = nativeArray[i];
				Edge edge2 = nativeArray2[i];
				if (m_ConnectedEdges.TryGetBuffer(edge2.m_Start, out var bufferData))
				{
					bufferData.Add(new ConnectedEdge(edge));
				}
				else
				{
					UnityEngine.Debug.Log($"Start node has no ConnectedEdge: {edge.Index}:{edge.Version}");
				}
				if (m_ConnectedEdges.TryGetBuffer(edge2.m_End, out var bufferData2))
				{
					bufferData2.Add(new ConnectedEdge(edge));
				}
				else
				{
					UnityEngine.Debug.Log($"End node has no ConnectedEdge: {edge.Index}:{edge.Version}");
				}
			}
			for (int j = 0; j < bufferAccessor.Length; j++)
			{
				Entity edge3 = nativeArray[j];
				DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[j];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					ConnectedNode connectedNode = dynamicBuffer[k];
					int num = 0;
					while (true)
					{
						if (num < k)
						{
							if (dynamicBuffer[num].m_Node == connectedNode.m_Node)
							{
								dynamicBuffer.RemoveAt(k--);
								break;
							}
							num++;
							continue;
						}
						if (m_ConnectedEdges.TryGetBuffer(connectedNode.m_Node, out var bufferData3))
						{
							bufferData3.Add(new ConnectedEdge(edge3));
							break;
						}
						UnityEngine.Debug.Log($"Middle node has no ConnectedEdge: {edge3.Index}:{edge3.Version}");
						dynamicBuffer.RemoveAt(k--);
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

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		public BufferTypeHandle<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferTypeHandle;

		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RW_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedNode>();
			__Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadWrite<ConnectedNode>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ConnectedEdgeJob jobData = new ConnectedEdgeJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_Query, base.Dependency);
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
	public ConnectedEdgeSystem()
	{
	}
}
