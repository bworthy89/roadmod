using System.Runtime.CompilerServices;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateAggregatesSystem : GameSystemBase
{
	[BurstCompile]
	private struct CreateAggregatesJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public BufferTypeHandle<AggregateElement> m_AggregateElementType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AggregateNetData> m_AggregateData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DefinitionChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_DeletedChunks;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelMultiHashMap<Entity, Entity> deletedAggregates = new NativeParallelMultiHashMap<Entity, Entity>(16, Allocator.Temp);
			for (int i = 0; i < m_DeletedChunks.Length; i++)
			{
				FillDeletedAggregates(m_DeletedChunks[i], deletedAggregates);
			}
			for (int j = 0; j < m_DefinitionChunks.Length; j++)
			{
				CreateAggregates(m_DefinitionChunks[j], deletedAggregates);
			}
			deletedAggregates.Dispose();
		}

		private void FillDeletedAggregates(ArchetypeChunk chunk, NativeParallelMultiHashMap<Entity, Entity> deletedAggregates)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity item = nativeArray[i];
				deletedAggregates.Add(nativeArray2[i].m_Prefab, item);
			}
		}

		private void CreateAggregates(ArchetypeChunk chunk, NativeParallelMultiHashMap<Entity, Entity> deletedAggregates)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			BufferAccessor<AggregateElement> bufferAccessor = chunk.GetBufferAccessor(ref m_AggregateElementType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				DynamicBuffer<AggregateElement> dynamicBuffer = bufferAccessor[i];
				TempFlags tempFlags = (TempFlags)0u;
				if (creationDefinition.m_Original != Entity.Null)
				{
					m_CommandBuffer.AddComponent(creationDefinition.m_Original, default(Hidden));
					creationDefinition.m_Prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
					if ((creationDefinition.m_Flags & CreationFlags.Delete) != 0)
					{
						tempFlags |= TempFlags.Delete;
					}
					else if ((creationDefinition.m_Flags & CreationFlags.Select) != 0)
					{
						tempFlags |= TempFlags.Select;
					}
					else if ((creationDefinition.m_Flags & CreationFlags.Relocate) != 0)
					{
						tempFlags |= TempFlags.Modify;
					}
				}
				else
				{
					tempFlags |= TempFlags.Create;
				}
				tempFlags |= TempFlags.Essential;
				if (deletedAggregates.TryGetFirstValue(creationDefinition.m_Prefab, out var item, out var it))
				{
					deletedAggregates.Remove(it);
					m_CommandBuffer.SetComponent(item, new Temp(creationDefinition.m_Original, tempFlags));
					m_CommandBuffer.AddComponent(item, default(Updated));
					m_CommandBuffer.RemoveComponent<Deleted>(item);
				}
				else
				{
					AggregateNetData aggregateNetData = m_AggregateData[creationDefinition.m_Prefab];
					item = m_CommandBuffer.CreateEntity(aggregateNetData.m_Archetype);
					m_CommandBuffer.SetComponent(item, new PrefabRef(creationDefinition.m_Prefab));
					m_CommandBuffer.AddComponent(item, new Temp(creationDefinition.m_Original, tempFlags));
				}
				DynamicBuffer<AggregateElement> dynamicBuffer2 = m_CommandBuffer.SetBuffer<AggregateElement>(item);
				dynamicBuffer2.ResizeUninitialized(dynamicBuffer.Length);
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					AggregateElement value = dynamicBuffer[j];
					m_CommandBuffer.AddComponent(value.m_Edge, default(Highlighted));
					m_CommandBuffer.AddComponent(value.m_Edge, default(BatchesUpdated));
					dynamicBuffer2[j] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<AggregateElement> __Game_Net_AggregateElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AggregateNetData> __Game_Prefabs_AggregateNetData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Net_AggregateElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<AggregateElement>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_AggregateNetData_RO_ComponentLookup = state.GetComponentLookup<AggregateNetData>(isReadOnly: true);
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
		m_DefinitionQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<AggregateElement>(), ComponentType.ReadOnly<Updated>());
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<AggregateElement>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Deleted>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> definitionChunks = m_DefinitionQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<ArchetypeChunk> deletedChunks = m_DeletedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
		JobHandle jobHandle = IJobExtensions.Schedule(new CreateAggregatesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AggregateElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_AggregateElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AggregateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AggregateNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DefinitionChunks = definitionChunks,
			m_DeletedChunks = deletedChunks,
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
	public GenerateAggregatesSystem()
	{
	}
}
