using System.Runtime.CompilerServices;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateNotificationsSystem : GameSystemBase
{
	[BurstCompile]
	private struct GenerateIconsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<IconDefinition> m_IconDefinitionType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NotificationIconData> m_NotificationIconData;

		[ReadOnly]
		public EntityArchetype m_DefaultArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<IconDefinition> nativeArray2 = chunk.GetNativeArray(ref m_IconDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				IconDefinition iconDefinition = nativeArray2[i];
				Icon component = new Icon
				{
					m_Location = iconDefinition.m_Location,
					m_Priority = iconDefinition.m_Priority,
					m_ClusterLayer = iconDefinition.m_ClusterLayer,
					m_Flags = iconDefinition.m_Flags
				};
				PrefabRef component2 = new PrefabRef
				{
					m_Prefab = creationDefinition.m_Prefab
				};
				if (creationDefinition.m_Original != Entity.Null && m_PrefabRefData.HasComponent(creationDefinition.m_Original))
				{
					component2.m_Prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
				}
				if (component2.m_Prefab == Entity.Null || !m_NotificationIconData.HasComponent(component2.m_Prefab))
				{
					continue;
				}
				NotificationIconData notificationIconData = m_NotificationIconData[component2.m_Prefab];
				if (!notificationIconData.m_Archetype.Valid)
				{
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) != 0)
					{
						continue;
					}
					notificationIconData.m_Archetype = m_DefaultArchetype;
				}
				if (creationDefinition.m_Original != Entity.Null)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, creationDefinition.m_Original, default(Hidden));
				}
				Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, notificationIconData.m_Archetype);
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, component2);
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, component);
				if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0)
				{
					Temp component3 = new Temp
					{
						m_Original = creationDefinition.m_Original
					};
					component3.m_Flags |= TempFlags.Essential;
					if ((creationDefinition.m_Flags & CreationFlags.Delete) != 0)
					{
						component3.m_Flags |= TempFlags.Delete;
					}
					else if ((creationDefinition.m_Flags & CreationFlags.Select) != 0)
					{
						component3.m_Flags |= TempFlags.Select;
					}
					else
					{
						component3.m_Flags |= TempFlags.Create;
					}
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component3);
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, default(DisallowCluster));
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
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<IconDefinition> __Game_Tools_IconDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NotificationIconData> __Game_Prefabs_NotificationIconData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_IconDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IconDefinition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NotificationIconData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconData>(isReadOnly: true);
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityArchetype m_DefaultArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<Updated>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<IconDefinition>() }
		});
		m_DefaultArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<PrefabRef>(), ComponentType.ReadWrite<Icon>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new GenerateIconsJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IconDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_IconDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NotificationIconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DefaultArchetype = m_DefaultArchetype,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_DefinitionQuery, base.Dependency);
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
	public GenerateNotificationsSystem()
	{
	}
}
