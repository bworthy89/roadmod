using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Notifications;

[CompilerGenerated]
public class IconDeletedSystem : GameSystemBase
{
	[BurstCompile]
	private struct IconDeletedJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Icon> m_IconType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<IconElement> m_IconElementType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Icon> m_IconData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public IconCommandBuffer m_WarningCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Icon> nativeArray = chunk.GetNativeArray(ref m_IconType);
			if (nativeArray.Length != 0)
			{
				NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
				if (nativeArray2.Length == 0)
				{
					return;
				}
				NativeArray<Target> nativeArray3 = chunk.GetNativeArray(ref m_TargetType);
				NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
				if (nativeArray3.Length != 0)
				{
					for (int i = 0; i < nativeArray.Length; i++)
					{
						Owner owner = nativeArray2[i];
						if (!m_DeletedData.HasComponent(owner.m_Owner))
						{
							Icon icon = nativeArray[i];
							Target target = nativeArray3[i];
							PrefabRef prefabRef = nativeArray4[i];
							m_WarningCommandBuffer.Remove(owner.m_Owner, prefabRef.m_Prefab, target.m_Target, icon.m_Flags & IconFlags.SecondaryLocation);
						}
					}
					return;
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Owner owner2 = nativeArray2[j];
					if (!m_DeletedData.HasComponent(owner2.m_Owner))
					{
						Icon icon2 = nativeArray[j];
						PrefabRef prefabRef2 = nativeArray4[j];
						m_WarningCommandBuffer.Remove(owner2.m_Owner, prefabRef2.m_Prefab, Entity.Null, icon2.m_Flags & IconFlags.SecondaryLocation);
					}
				}
				return;
			}
			BufferAccessor<IconElement> bufferAccessor = chunk.GetBufferAccessor(ref m_IconElementType);
			if (bufferAccessor.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray5 = chunk.GetNativeArray(m_EntityType);
			bool flag = chunk.Has(ref m_DeletedType);
			for (int k = 0; k < bufferAccessor.Length; k++)
			{
				Entity owner3 = nativeArray5[k];
				DynamicBuffer<IconElement> dynamicBuffer = bufferAccessor[k];
				for (int l = 0; l < dynamicBuffer.Length; l++)
				{
					Entity icon3 = dynamicBuffer[l].m_Icon;
					if (m_DeletedData.HasComponent(icon3))
					{
						continue;
					}
					Icon icon4 = m_IconData[icon3];
					PrefabRef prefabRef3 = m_PrefabRefData[icon3];
					if (m_TargetData.HasComponent(icon3))
					{
						Target target2 = m_TargetData[icon3];
						if (flag || m_DeletedData.HasComponent(target2.m_Target))
						{
							m_WarningCommandBuffer.Remove(owner3, prefabRef3.m_Prefab, target2.m_Target, icon4.m_Flags & IconFlags.SecondaryLocation);
						}
					}
					else if (flag)
					{
						m_WarningCommandBuffer.Remove(owner3, prefabRef3.m_Prefab, Entity.Null, icon4.m_Flags & IconFlags.SecondaryLocation);
					}
				}
				if (!flag)
				{
					m_WarningCommandBuffer.Update(owner3);
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
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<IconElement> __Game_Notifications_IconElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Icon> __Game_Notifications_Icon_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Notifications_IconElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<IconElement>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Notifications_Icon_RO_ComponentLookup = state.GetComponentLookup<Icon>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
		}
	}

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_DeletedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Icon>(),
				ComponentType.ReadOnly<IconElement>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<IconElement>()
			}
		});
		RequireForUpdate(m_DeletedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IconDeletedJob jobData = new IconDeletedJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Notifications_IconElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WarningCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_DeletedQuery, base.Dependency);
		m_IconCommandSystem.AddCommandBufferWriter(base.Dependency);
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
	public IconDeletedSystem()
	{
	}
}
