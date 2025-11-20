using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Zones;

[CompilerGenerated]
public class BlockReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateBlockReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		public BufferLookup<SubBlock> m_Blocks;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			if (chunk.Has(ref m_CreatedType))
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity block = nativeArray[i];
					Owner owner = nativeArray2[i];
					CollectionUtils.TryAddUniqueValue(m_Blocks[owner.m_Owner], new SubBlock(block));
				}
			}
			else
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity block2 = nativeArray[j];
					Owner owner2 = nativeArray2[j];
					CollectionUtils.RemoveValue(m_Blocks[owner2.m_Owner], new SubBlock(block2));
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
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		public BufferLookup<SubBlock> __Game_Zones_SubBlock_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Zones_SubBlock_RW_BufferLookup = state.GetBufferLookup<SubBlock>();
		}
	}

	private EntityQuery m_BlockQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BlockQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Block>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireForUpdate(m_BlockQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateBlockReferencesJob jobData = new UpdateBlockReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Blocks = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Zones_SubBlock_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_BlockQuery, base.Dependency);
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
	public BlockReferencesSystem()
	{
	}
}
