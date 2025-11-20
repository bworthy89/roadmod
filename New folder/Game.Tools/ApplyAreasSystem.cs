using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyAreasSystem : GameSystemBase
{
	[BurstCompile]
	private struct PatchTempReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public BufferLookup<SubArea> m_SubAreas;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity area = nativeArray[i];
				Owner owner = nativeArray3[i];
				Temp temp = nativeArray2[i];
				if (!(temp.m_Original == Entity.Null) || (temp.m_Flags & TempFlags.Delete) != 0)
				{
					continue;
				}
				Owner value = owner;
				if (m_TempData.HasComponent(owner.m_Owner))
				{
					Temp temp2 = m_TempData[owner.m_Owner];
					if (temp2.m_Original != Entity.Null && (temp2.m_Flags & TempFlags.Replace) == 0)
					{
						value.m_Owner = temp2.m_Original;
					}
				}
				if (value.m_Owner != owner.m_Owner)
				{
					if (m_SubAreas.HasBuffer(owner.m_Owner))
					{
						CollectionUtils.RemoveValue(m_SubAreas[owner.m_Owner], new SubArea(area));
					}
					if (m_SubAreas.HasBuffer(value.m_Owner))
					{
						CollectionUtils.TryAddUniqueValue(m_SubAreas[value.m_Owner], new SubArea(area));
					}
					nativeArray3[i] = value;
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_AreaNodeType;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> m_LocalNodeCacheType;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_LocalNodeCache;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_AreaNodeType);
			if (bufferAccessor.Length != 0)
			{
				BufferAccessor<LocalNodeCache> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LocalNodeCacheType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity, temp);
					}
					else if (temp.m_Original != Entity.Null)
					{
						DynamicBuffer<LocalNodeCache> cachedNodes = default(DynamicBuffer<LocalNodeCache>);
						if (bufferAccessor2.Length != 0)
						{
							cachedNodes = bufferAccessor2[i];
						}
						Update(unfilteredChunkIndex, entity, temp, bufferAccessor[i], cachedNodes);
					}
					else
					{
						Create(unfilteredChunkIndex, entity);
					}
				}
				return;
			}
			for (int j = 0; j < nativeArray.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				Temp temp2 = nativeArray2[j];
				if ((temp2.m_Flags & TempFlags.Delete) != 0)
				{
					Delete(unfilteredChunkIndex, entity2, temp2);
				}
				else if (temp2.m_Original != Entity.Null)
				{
					Update(unfilteredChunkIndex, entity2, temp2);
				}
				else
				{
					Create(unfilteredChunkIndex, entity2);
				}
			}
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (temp.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, bool updateOriginal = true)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			if (updateOriginal)
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Updated));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, DynamicBuffer<Node> nodes, DynamicBuffer<LocalNodeCache> cachedNodes)
		{
			DynamicBuffer<Node> dynamicBuffer = m_CommandBuffer.SetBuffer<Node>(chunkIndex, temp.m_Original);
			dynamicBuffer.ResizeUninitialized(nodes.Length);
			for (int i = 0; i < nodes.Length; i++)
			{
				dynamicBuffer[i] = nodes[i];
			}
			if (cachedNodes.IsCreated)
			{
				DynamicBuffer<LocalNodeCache> dynamicBuffer2 = ((!m_LocalNodeCache.HasBuffer(temp.m_Original)) ? m_CommandBuffer.AddBuffer<LocalNodeCache>(chunkIndex, temp.m_Original) : m_CommandBuffer.SetBuffer<LocalNodeCache>(chunkIndex, temp.m_Original));
				dynamicBuffer2.ResizeUninitialized(cachedNodes.Length);
				for (int j = 0; j < cachedNodes.Length; j++)
				{
					dynamicBuffer2[j] = cachedNodes[j];
				}
			}
			else if (m_LocalNodeCache.HasBuffer(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<LocalNodeCache>(chunkIndex, temp.m_Original);
			}
			Update(chunkIndex, entity, temp);
		}

		private void Create(int chunkIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Temp>(chunkIndex, entity);
			m_CommandBuffer.AddComponent(chunkIndex, entity, in m_AppliedTypes);
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Owner> __Game_Common_Owner_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		public BufferLookup<SubArea> __Game_Areas_SubArea_RW_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>();
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Areas_SubArea_RW_BufferLookup = state.GetBufferLookup<SubArea>();
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferTypeHandle = state.GetBufferTypeHandle<LocalNodeCache>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_TempQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Area>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PatchTempReferencesJob jobData = new PatchTempReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RW_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AreaNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LocalNodeCacheType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalNodeCache = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
		}, dependsOn: JobChunkExtensions.Schedule(jobData, m_TempQuery, base.Dependency), query: m_TempQuery);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
	public ApplyAreasSystem()
	{
	}
}
