using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Citizens;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class MarkerIconSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public enum MarkerType
	{
		Selected,
		Followed
	}

	private struct Overlap
	{
		public Entity m_Entity;

		public Entity m_Other;

		public Overlap(Entity entity, Entity other)
		{
			m_Entity = entity;
			m_Other = other;
		}
	}

	[BurstCompile]
	private struct FindOverlapIconsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Icon> m_IconType;

		[ReadOnly]
		public Entity m_Entity1;

		[ReadOnly]
		public Entity m_Entity2;

		[ReadOnly]
		public float3 m_Location1;

		[ReadOnly]
		public float3 m_Location2;

		public NativeQueue<Overlap>.ParallelWriter m_OverlapQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Icon> nativeArray2 = chunk.GetNativeArray(ref m_IconType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Icon icon = nativeArray2[i];
				if (math.distancesq(icon.m_Location, m_Location1) < 0.01f && nativeArray[i] != m_Entity1)
				{
					m_OverlapQueue.Enqueue(new Overlap(m_Entity1, nativeArray[i]));
				}
				if (math.distancesq(icon.m_Location, m_Location2) < 0.01f && nativeArray[i] != m_Entity2)
				{
					m_OverlapQueue.Enqueue(new Overlap(m_Entity2, nativeArray[i]));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateMarkerLocationJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> m_IconDisplayData;

		[ReadOnly]
		public Entity m_Entity1;

		[ReadOnly]
		public Entity m_Entity2;

		[ReadOnly]
		public float3 m_Location1;

		[ReadOnly]
		public float3 m_Location2;

		[ReadOnly]
		public float3 m_CameraPos;

		[ReadOnly]
		public float3 m_CameraUp;

		public ComponentLookup<Icon> m_IconData;

		public NativeQueue<Overlap> m_OverlapQueue;

		public void Execute()
		{
			Icon value = default(Icon);
			Icon value2 = default(Icon);
			if (m_Entity1 != Entity.Null)
			{
				value = m_IconData[m_Entity1];
			}
			if (m_Entity2 != Entity.Null)
			{
				value2 = m_IconData[m_Entity2];
			}
			float num = 0f;
			float num2 = 0f;
			Overlap item;
			while (m_OverlapQueue.TryDequeue(out item))
			{
				if (item.m_Entity == m_Entity1)
				{
					num = math.max(num, CalculateOffset(item.m_Other));
				}
				if (item.m_Entity == m_Entity2)
				{
					num2 = math.max(num2, CalculateOffset(item.m_Other));
				}
			}
			value.m_Location = m_Location1 + m_CameraUp * num;
			value2.m_Location = m_Location2 + m_CameraUp * num2;
			if (m_Entity1 != Entity.Null)
			{
				m_IconData[m_Entity1] = value;
			}
			if (m_Entity2 != Entity.Null)
			{
				m_IconData[m_Entity2] = value2;
			}
		}

		private float CalculateOffset(Entity entity)
		{
			Icon icon = m_IconData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			NotificationIconDisplayData notificationIconDisplayData = m_IconDisplayData[prefabRef.m_Prefab];
			float t = (float)(int)icon.m_Priority * 0.003921569f;
			float2 @float = math.lerp(notificationIconDisplayData.m_MinParams, notificationIconDisplayData.m_MaxParams, t);
			return IconClusterSystem.IconCluster.CalculateRadius(distance: math.distance(icon.m_Location, m_CameraPos), radius: @float.x) * 1.5f;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Icon> __Game_Notifications_Icon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup;

		public ComponentLookup<Icon> __Game_Notifications_Icon_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Notifications_Icon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Icon>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup = state.GetComponentLookup<NotificationIconDisplayData>(isReadOnly: true);
			__Game_Notifications_Icon_RW_ComponentLookup = state.GetComponentLookup<Icon>();
		}
	}

	private Entity m_SelectedMarker;

	private Entity m_FollowedMarker;

	private Entity m_SelectedLocation;

	private Entity m_FollowedLocation;

	private EntityQuery m_ConfigurationQuery;

	private EntityQuery m_IconQuery;

	private CameraUpdateSystem m_CameraUpdateSystem;

	private ToolSystem m_ToolSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<IconConfigurationData>());
		m_IconQuery = GetEntityQuery(ComponentType.ReadOnly<Icon>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Entity entity = m_ToolSystem.selected;
		int elementIndex = m_ToolSystem.selectedIndex;
		Entity entity2 = Entity.Null;
		int elementIndex2 = -1;
		if (m_CameraUpdateSystem.orbitCameraController != null)
		{
			entity2 = m_CameraUpdateSystem.orbitCameraController.followedEntity;
		}
		float3 position = default(float3);
		float3 position2 = default(float3);
		Entity location = Entity.Null;
		Entity location2 = Entity.Null;
		Bounds3 bounds = default(Bounds3);
		Bounds3 bounds2 = default(Bounds3);
		if (entity2 != Entity.Null && !SelectedInfoUISystem.TryGetPosition(entity2, base.EntityManager, ref elementIndex2, out location, out position2, out bounds, out var rotation))
		{
			location = Entity.Null;
		}
		if ((entity2 == entity && elementIndex2 == elementIndex) || (base.EntityManager.TryGetComponent<CurrentTransport>(entity2, out var component) && component.m_CurrentTransport == entity))
		{
			entity = Entity.Null;
		}
		if (entity != Entity.Null && (!SelectedInfoUISystem.TryGetPosition(entity, base.EntityManager, ref elementIndex, out location2, out position, out bounds2, out rotation) || location == location2))
		{
			location2 = Entity.Null;
		}
		if (entity2 != Entity.Null)
		{
			if (location != m_FollowedLocation)
			{
				RemoveMarker(ref m_FollowedMarker, location2 == m_FollowedLocation);
			}
			position2.y = bounds.max.y;
			UpdateMarker(ref m_FollowedMarker, entity2, MarkerType.Followed, position2, m_SelectedLocation == location);
		}
		else
		{
			RemoveMarker(ref m_FollowedMarker, location2 == m_FollowedLocation);
		}
		if (entity != Entity.Null)
		{
			if (location2 != m_SelectedLocation)
			{
				RemoveMarker(ref m_SelectedMarker, location == m_SelectedLocation);
			}
			position.y = bounds2.max.y;
			UpdateMarker(ref m_SelectedMarker, entity, MarkerType.Selected, position, m_FollowedLocation == location2);
		}
		else
		{
			RemoveMarker(ref m_SelectedMarker, location == m_SelectedLocation);
		}
		m_FollowedLocation = location;
		m_SelectedLocation = location2;
		if ((m_SelectedMarker != Entity.Null || m_FollowedMarker != Entity.Null) && m_CameraUpdateSystem.activeCameraController != null)
		{
			AdjustLocations(position, position2, m_CameraUpdateSystem.activeCameraController.position, Quaternion.Euler(m_CameraUpdateSystem.activeCameraController.rotation) * Vector3.up);
		}
	}

	private void UpdateMarker(ref Entity marker, Entity target, MarkerType markerType, float3 position, bool skipAnimation)
	{
		if (base.EntityManager.HasComponent<Icon>(target) && base.EntityManager.TryGetComponent<Owner>(target, out var component) && base.EntityManager.Exists(component.m_Owner))
		{
			target = component.m_Owner;
		}
		if (marker == Entity.Null)
		{
			marker = CreateMarker(target, position, markerType, skipAnimation);
			return;
		}
		Target componentData = base.EntityManager.GetComponentData<Target>(marker);
		if (componentData.m_Target != target)
		{
			componentData.m_Target = target;
			base.EntityManager.SetComponentData(marker, componentData);
		}
	}

	private void RemoveMarker(ref Entity marker, bool skipAnimation)
	{
		if (!(marker != Entity.Null))
		{
			return;
		}
		Game.Notifications.Animation component;
		if (skipAnimation || m_ConfigurationQuery.IsEmptyIgnoreFilter)
		{
			base.EntityManager.AddComponent<Deleted>(marker);
		}
		else if (base.EntityManager.TryGetComponent<Game.Notifications.Animation>(marker, out component))
		{
			if (component.m_Type != Game.Notifications.AnimationType.MarkerDisappear)
			{
				component.m_Type = Game.Notifications.AnimationType.MarkerDisappear;
				component.m_Timer = component.m_Duration - component.m_Timer;
				base.EntityManager.SetComponentData(marker, component);
			}
		}
		else
		{
			Entity singletonEntity = m_ConfigurationQuery.GetSingletonEntity();
			float duration = base.EntityManager.GetBuffer<IconAnimationElement>(singletonEntity, isReadOnly: true)[1].m_Duration;
			base.EntityManager.AddComponentData(marker, new Game.Notifications.Animation(Game.Notifications.AnimationType.MarkerDisappear, UnityEngine.Time.deltaTime, duration));
		}
		marker = Entity.Null;
	}

	private Entity CreateMarker(Entity target, float3 position, MarkerType markerType, bool skipAnimation)
	{
		if (m_ConfigurationQuery.IsEmptyIgnoreFilter)
		{
			return Entity.Null;
		}
		Entity singletonEntity = m_ConfigurationQuery.GetSingletonEntity();
		IconConfigurationData componentData = base.EntityManager.GetComponentData<IconConfigurationData>(singletonEntity);
		Entity entity;
		switch (markerType)
		{
		case MarkerType.Selected:
			entity = componentData.m_SelectedMarker;
			break;
		case MarkerType.Followed:
			entity = componentData.m_FollowedMarker;
			break;
		default:
			return Entity.Null;
		}
		NotificationIconData componentData2 = base.EntityManager.GetComponentData<NotificationIconData>(entity);
		Icon componentData3 = new Icon
		{
			m_Priority = IconPriority.Info,
			m_Flags = (IconFlags.Unique | IconFlags.OnTop),
			m_Location = position
		};
		Entity entity2 = base.EntityManager.CreateEntity(componentData2.m_Archetype);
		base.EntityManager.SetComponentData(entity2, new PrefabRef(entity));
		base.EntityManager.SetComponentData(entity2, componentData3);
		base.EntityManager.AddComponentData(entity2, new Target(target));
		base.EntityManager.AddComponent<DisallowCluster>(entity2);
		if (!skipAnimation)
		{
			float duration = base.EntityManager.GetBuffer<IconAnimationElement>(singletonEntity, isReadOnly: true)[0].m_Duration;
			base.EntityManager.AddComponentData(entity2, new Game.Notifications.Animation(Game.Notifications.AnimationType.MarkerAppear, UnityEngine.Time.deltaTime, duration));
		}
		return entity2;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_SelectedMarker;
		writer.Write(value);
		Entity value2 = m_FollowedMarker;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_SelectedMarker;
		reader.Read(out value);
		ref Entity value2 = ref m_FollowedMarker;
		reader.Read(out value2);
		m_SelectedLocation = Entity.Null;
		m_FollowedLocation = Entity.Null;
	}

	public void SetDefaults(Context context)
	{
		m_SelectedMarker = Entity.Null;
		m_FollowedMarker = Entity.Null;
		m_SelectedLocation = Entity.Null;
		m_FollowedLocation = Entity.Null;
	}

	private void AdjustLocations(float3 selectedLocation, float3 followedLocation, float3 cameraPos, float3 cameraUp)
	{
		NativeQueue<Overlap> overlapQueue = new NativeQueue<Overlap>(Allocator.TempJob);
		FindOverlapIconsJob jobData = new FindOverlapIconsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_IconType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Notifications_Icon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_Entity1 = m_SelectedMarker,
			m_Entity2 = m_FollowedMarker,
			m_Location1 = selectedLocation,
			m_Location2 = followedLocation,
			m_OverlapQueue = overlapQueue.AsParallelWriter()
		};
		UpdateMarkerLocationJob jobData2 = new UpdateMarkerLocationJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IconDisplayData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Entity1 = m_SelectedMarker,
			m_Entity2 = m_FollowedMarker,
			m_Location1 = selectedLocation,
			m_Location2 = followedLocation,
			m_CameraPos = cameraPos,
			m_CameraUp = cameraUp,
			m_IconData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Notifications_Icon_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OverlapQueue = overlapQueue
		};
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_IconQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, dependsOn);
		overlapQueue.Dispose(jobHandle);
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
	public MarkerIconSystem()
	{
	}
}
