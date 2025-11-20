using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class SubAreaReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateSubAreaReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public BufferLookup<SubArea> m_SubAreas;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			if (chunk.Has(ref m_CreatedType))
			{
				if (chunk.Has(ref m_TempType))
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Owner owner = nativeArray2[i];
						if (m_TempData.HasComponent(owner.m_Owner) && m_SubAreas.HasBuffer(owner.m_Owner))
						{
							CollectionUtils.TryAddUniqueValue(m_SubAreas[owner.m_Owner], new SubArea(nativeArray[i]));
						}
					}
					return;
				}
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity area = nativeArray[j];
					Owner owner2 = nativeArray2[j];
					if (m_SubAreas.HasBuffer(owner2.m_Owner))
					{
						CollectionUtils.TryAddUniqueValue(m_SubAreas[owner2.m_Owner], new SubArea(area));
					}
				}
				return;
			}
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity area2 = nativeArray[k];
				Owner owner3 = nativeArray2[k];
				if (m_SubAreas.HasBuffer(owner3.m_Owner))
				{
					CollectionUtils.RemoveValue(m_SubAreas[owner3.m_Owner], new SubArea(area2));
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

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		public BufferLookup<SubArea> __Game_Areas_SubArea_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Areas_SubArea_RW_BufferLookup = state.GetBufferLookup<SubArea>();
		}
	}

	private EntityQuery m_SubAreaQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SubAreaQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		RequireForUpdate(m_SubAreaQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateSubAreaReferencesJob jobData = new UpdateSubAreaReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RW_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.Schedule(jobData, m_SubAreaQuery, base.Dependency);
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
	public SubAreaReferencesSystem()
	{
	}
}
