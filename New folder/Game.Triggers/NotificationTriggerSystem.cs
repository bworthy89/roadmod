using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class NotificationTriggerSystem : GameSystemBase
{
	[BurstCompile]
	private struct TriggerJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<Entity> m_Created;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<PrefabRef> m_CreatedPrefabRefs;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<Entity> m_Deleted;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<PrefabRef> m_DeletedPrefabRefs;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<PrefabRef> m_AllPrefabRefs;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Target> m_TargetData;

		public NativeQueue<TriggerAction> m_ActionQueue;

		public void Execute()
		{
			Execute(m_Created, m_CreatedPrefabRefs, TriggerType.NewNotification);
			Execute(m_Deleted, m_DeletedPrefabRefs, TriggerType.NotificationResolved);
		}

		private void Execute(NativeArray<Entity> entities, NativeArray<PrefabRef> prefabRefs, TriggerType triggerType)
		{
			for (int i = 0; i < entities.Length; i++)
			{
				Owner componentData;
				bool flag = m_OwnerData.TryGetComponent(entities[i], out componentData);
				Target componentData2;
				bool flag2 = m_TargetData.TryGetComponent(entities[i], out componentData2);
				m_ActionQueue.Enqueue(new TriggerAction(triggerType, prefabRefs[i].m_Prefab, flag ? componentData.m_Owner : Entity.Null, flag2 ? componentData2.m_Target : Entity.Null, Count(prefabRefs[i].m_Prefab)));
			}
		}

		private int Count(Entity notification)
		{
			int num = 0;
			for (int i = 0; i < m_AllPrefabRefs.Length; i++)
			{
				if (notification == m_AllPrefabRefs[i].m_Prefab)
				{
					num++;
				}
			}
			return num;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
		}
	}

	private TriggerSystem m_TriggerSystem;

	private EntityQuery m_CreatedNotificationsQuery;

	private EntityQuery m_DeletedNotificationsQuery;

	private EntityQuery m_AllNotificationsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_CreatedNotificationsQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Icon>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Applied>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_DeletedNotificationsQuery = GetEntityQuery(ComponentType.ReadOnly<Icon>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_AllNotificationsQuery = GetEntityQuery(ComponentType.ReadOnly<Icon>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		base.Enabled = false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TriggerJob jobData = new TriggerJob
		{
			m_Created = m_CreatedNotificationsQuery.ToEntityArray(Allocator.TempJob),
			m_CreatedPrefabRefs = m_CreatedNotificationsQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob),
			m_Deleted = m_DeletedNotificationsQuery.ToEntityArray(Allocator.TempJob),
			m_DeletedPrefabRefs = m_DeletedNotificationsQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob),
			m_AllPrefabRefs = m_AllNotificationsQuery.ToComponentDataArray<PrefabRef>(Allocator.TempJob),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = m_TriggerSystem.CreateActionBuffer()
		};
		base.Dependency = IJobExtensions.Schedule(jobData, base.Dependency);
		m_TriggerSystem.AddActionBufferWriter(base.Dependency);
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
	public NotificationTriggerSystem()
	{
	}
}
