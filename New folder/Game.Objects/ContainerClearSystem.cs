using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Effects;
using Game.Net;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class ContainerClearSystem : GameSystemBase
{
	[BurstCompile]
	private struct ContainerClearJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<EnabledEffect> m_EffectOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> m_SubNetType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.SubArea> m_SubAreaType;

		[ReadOnly]
		public BufferLookup<Effect> m_PrefabEffects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public ComponentTypeSet m_SubTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<Game.Net.SubNet> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubNetType);
			BufferAccessor<Game.Areas.SubArea> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubAreaType);
			bool flag = chunk.Has(ref m_EffectOwnerType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity e = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				DynamicBuffer<SubObject> dynamicBuffer = bufferAccessor[i];
				DynamicBuffer<Game.Net.SubNet> dynamicBuffer2 = bufferAccessor2[i];
				DynamicBuffer<Game.Areas.SubArea> dynamicBuffer3 = bufferAccessor3[i];
				bool3 x = false;
				x.x = dynamicBuffer.Length == 0 && !m_PrefabSubObjects.HasBuffer(prefabRef.m_Prefab);
				x.y = dynamicBuffer2.Length == 0 && !m_PrefabSubNets.HasBuffer(prefabRef.m_Prefab);
				x.z = dynamicBuffer3.Length == 0 && !m_PrefabSubAreas.HasBuffer(prefabRef.m_Prefab);
				if (math.all(x))
				{
					m_CommandBuffer.RemoveComponent(unfilteredChunkIndex, e, in m_SubTypes);
				}
				else
				{
					if (x.x)
					{
						m_CommandBuffer.RemoveComponent<SubObject>(unfilteredChunkIndex, e);
					}
					if (x.y)
					{
						m_CommandBuffer.RemoveComponent<Game.Net.SubNet>(unfilteredChunkIndex, e);
					}
					if (x.z)
					{
						m_CommandBuffer.RemoveComponent<Game.Areas.SubArea>(unfilteredChunkIndex, e);
					}
				}
				if (!flag && m_PrefabEffects.HasBuffer(prefabRef.m_Prefab))
				{
					m_CommandBuffer.AddBuffer<EnabledEffect>(unfilteredChunkIndex, e);
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
		public BufferTypeHandle<EnabledEffect> __Game_Effects_EnabledEffect_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<Effect> __Game_Prefabs_Effect_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Effects_EnabledEffect_RO_BufferTypeHandle = state.GetBufferTypeHandle<EnabledEffect>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Net.SubNet>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Prefabs_Effect_RO_BufferLookup = state.GetBufferLookup<Effect>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
		}
	}

	private LoadGameSystem m_LoadGameSystem;

	private EntityQuery m_EntityQuery;

	private ComponentTypeSet m_SubTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_LoadGameSystem = base.World.GetOrCreateSystemManaged<LoadGameSystem>();
		m_EntityQuery = GetEntityQuery(ComponentType.ReadOnly<SubObject>(), ComponentType.ReadOnly<Game.Net.SubNet>(), ComponentType.ReadOnly<Game.Areas.SubArea>(), ComponentType.ReadOnly<Object>(), ComponentType.Exclude<Building>());
		m_SubTypes = new ComponentTypeSet(ComponentType.ReadWrite<SubObject>(), ComponentType.ReadWrite<Game.Net.SubNet>(), ComponentType.ReadWrite<Game.Areas.SubArea>());
		RequireForUpdate(m_EntityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_LoadGameSystem.context.purpose == Purpose.NewGame)
		{
			EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);
			JobChunkExtensions.ScheduleParallel(new ContainerClearJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EffectOwnerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Effects_EnabledEffect_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_SubNetType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubNet_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_SubAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabEffects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_Effect_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubTypes = m_SubTypes,
				m_CommandBuffer = entityCommandBuffer.AsParallelWriter()
			}, m_EntityQuery, base.Dependency).Complete();
			entityCommandBuffer.Playback(base.EntityManager);
			entityCommandBuffer.Dispose();
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
	public ContainerClearSystem()
	{
	}
}
