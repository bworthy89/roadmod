using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class SubObjectSystem : GameSystemBase
{
	[BurstCompile]
	private struct SubObjectJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		public BufferLookup<SubObject> m_SubObjects;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Attached> nativeArray3 = chunk.GetNativeArray(ref m_AttachedType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity subObject = nativeArray[i];
				Owner owner = nativeArray2[i];
				if (m_SubObjects.TryGetBuffer(owner.m_Owner, out var bufferData))
				{
					bufferData.Add(new SubObject(subObject));
				}
				else
				{
					UnityEngine.Debug.Log($"Owner has no SubObject: {subObject.Index}:{subObject.Version}");
				}
			}
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				Entity subObject2 = nativeArray[j];
				Attached attached = nativeArray3[j];
				if (m_SubObjects.TryGetBuffer(attached.m_Parent, out var bufferData2))
				{
					CollectionUtils.TryAddUniqueValue(bufferData2, new SubObject(subObject2));
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
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		public BufferLookup<SubObject> __Game_Objects_SubObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Objects_SubObject_RW_BufferLookup = state.GetBufferLookup<SubObject>();
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
			All = new ComponentType[1] { ComponentType.ReadOnly<Game.Objects.Object>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Owner>(),
				ComponentType.ReadOnly<Attached>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Vehicle>(),
				ComponentType.ReadOnly<Creature>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		SubObjectJob jobData = new SubObjectJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RW_BufferLookup, ref base.CheckedStateRef)
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
	public SubObjectSystem()
	{
	}
}
