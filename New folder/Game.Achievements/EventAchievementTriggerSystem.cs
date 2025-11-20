using System.Runtime.CompilerServices;
using Colossal.PSI.Common;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Achievements;

[CompilerGenerated]
public class EventAchievementTriggerSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<EventAchievementData> __Game_Prefabs_EventAchievementData_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_EventAchievementData_RO_BufferLookup = state.GetBufferLookup<EventAchievementData>(isReadOnly: true);
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private ModificationEndBarrier m_ModifiactionEndBarrier;

	private EntityQuery m_TrackingQuery;

	private EntityQuery m_CreatedEventQuery;

	private EntityArchetype m_TrackingArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModifiactionEndBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_CreatedEventQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Events.Event>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<EventAchievement>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		m_TrackingArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<EventAchievementTrackingData>());
		m_TrackingQuery = GetEntityQuery(ComponentType.ReadWrite<EventAchievementTrackingData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_CreatedEventQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<ArchetypeChunk> nativeArray = m_CreatedEventQuery.ToArchetypeChunkArray(Allocator.TempJob);
			BufferLookup<EventAchievementData> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_EventAchievementData_RO_BufferLookup, ref base.CheckedStateRef);
			ComponentTypeHandle<Duration> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			EntityCommandBuffer buffer = m_ModifiactionEndBarrier.CreateCommandBuffer();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].Has(ref typeHandle))
				{
					NativeArray<Duration> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
					NativeArray<PrefabRef> nativeArray3 = nativeArray[i].GetNativeArray(ref typeHandle2);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						DynamicBuffer<EventAchievementData> dynamicBuffer = bufferLookup[nativeArray3[j].m_Prefab];
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							StartTracking(dynamicBuffer[k].m_ID, nativeArray2[j].m_StartFrame + dynamicBuffer[k].m_FrameDelay, buffer);
						}
					}
					continue;
				}
				NativeArray<PrefabRef> nativeArray4 = nativeArray[i].GetNativeArray(ref typeHandle2);
				for (int l = 0; l < nativeArray4.Length; l++)
				{
					DynamicBuffer<EventAchievementData> dynamicBuffer2 = bufferLookup[nativeArray4[l].m_Prefab];
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						StartTracking(dynamicBuffer2[m].m_ID, m_SimulationSystem.frameIndex + dynamicBuffer2[m].m_FrameDelay, buffer);
					}
				}
			}
			nativeArray.Dispose();
		}
		if (m_TrackingQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<EventAchievementTrackingData> nativeArray5 = m_TrackingQuery.ToComponentDataArray<EventAchievementTrackingData>(Allocator.TempJob);
		NativeArray<Entity> nativeArray6 = m_TrackingQuery.ToEntityArray(Allocator.TempJob);
		EntityCommandBuffer buffer2 = m_ModifiactionEndBarrier.CreateCommandBuffer();
		for (int n = 0; n < nativeArray5.Length; n++)
		{
			if (m_SimulationSystem.frameIndex > nativeArray5[n].m_StartFrame)
			{
				StopTracking(nativeArray5[n], nativeArray6[n], buffer2);
			}
		}
		nativeArray5.Dispose();
		nativeArray6.Dispose();
	}

	private void StartTracking(AchievementId id, uint startFrame, EntityCommandBuffer buffer)
	{
		Entity e = buffer.CreateEntity(m_TrackingArchetype);
		buffer.SetComponent(e, new EventAchievementTrackingData
		{
			m_ID = id,
			m_StartFrame = startFrame
		});
	}

	private void StopTracking(EventAchievementTrackingData data, Entity entity, EntityCommandBuffer buffer)
	{
		buffer.AddComponent<Deleted>(entity);
		PlatformManager.instance.UnlockAchievement(data.m_ID);
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
	public EventAchievementTriggerSystem()
	{
	}
}
