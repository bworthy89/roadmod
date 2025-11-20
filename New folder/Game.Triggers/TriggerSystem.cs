#define UNITY_ASSERTIONS
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Audio.Radio;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.PSI;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Game.Tutorials;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Triggers;

[CompilerGenerated]
public class TriggerSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct TriggerActionJob : IJob
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public TriggerPrefabData m_TriggerPrefabData;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<TriggerAction> m_Actions;

		public NativeQueue<ChirpCreationData> m_ChirpQueue;

		public NativeQueue<LifePathEventCreationData> m_LifePathEventQueue;

		public NativeQueue<RadioTag> m_RadioTagQueue;

		public NativeQueue<RadioTag> m_EmergencyRadioTagQueue;

		public NativeQueue<Entity> m_TutorialTriggerQueue;

		public NativeParallelHashMap<Entity, uint> m_TriggerFrames;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Locked> m_LockedData;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectData;

		[ReadOnly]
		public BufferLookup<TriggerChirpData> m_ChirpData;

		[ReadOnly]
		public ComponentLookup<LifePathEventData> m_LifePathEventData;

		[ReadOnly]
		public ComponentLookup<RadioEventData> m_RadioEventData;

		[ReadOnly]
		public BufferLookup<TutorialActivationEventData> m_TutorialEventData;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> m_TrafficAccidentData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PolicyData> m_PolicyData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfo;

		[ReadOnly]
		public BufferLookup<TriggerConditionData> m_TriggerConditions;

		[ReadOnly]
		public float3 m_CameraPosition;

		[ReadOnly]
		public ComponentLookup<TriggerLimitData> m_TriggerLimitData;

		public void Execute()
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(0);
			for (int i = 0; i < m_Actions.Length; i++)
			{
				TriggerAction triggerAction = m_Actions[i];
				if (!m_TriggerPrefabData.HasAnyPrefabs(triggerAction.m_TriggerType, triggerAction.m_TriggerPrefab))
				{
					continue;
				}
				TargetType targetType = TargetType.Nothing;
				if (m_BuildingData.HasComponent(triggerAction.m_PrimaryTarget))
				{
					targetType = TargetType.Building;
					if (m_PrefabRefData.TryGetComponent(triggerAction.m_PrimaryTarget, out var componentData) && m_ServiceObjectData.HasComponent(componentData.m_Prefab))
					{
						targetType |= TargetType.ServiceBuilding;
					}
				}
				if (m_CitizenData.HasComponent(triggerAction.m_PrimaryTarget))
				{
					targetType = TargetType.Citizen;
				}
				if (m_PolicyData.HasComponent(triggerAction.m_TriggerPrefab))
				{
					targetType = TargetType.Policy;
				}
				if (m_RoadData.HasComponent(triggerAction.m_PrimaryTarget))
				{
					targetType = TargetType.Road;
					if (m_TrafficAccidentData.HasComponent(triggerAction.m_TriggerPrefab) && triggerAction.m_PrimaryTarget != Entity.Null && m_CullingInfo.HasComponent(triggerAction.m_PrimaryTarget))
					{
						triggerAction.m_Value = math.distance(MathUtils.Center(m_CullingInfo[triggerAction.m_PrimaryTarget].m_Bounds), m_CameraPosition);
					}
				}
				if (!m_TriggerPrefabData.TryGetFirstPrefab(triggerAction.m_TriggerType, targetType, triggerAction.m_TriggerPrefab, out var prefab, out var iterator))
				{
					continue;
				}
				do
				{
					if (CheckInterval(prefab) && CheckConditions(prefab, triggerAction))
					{
						CreateEntity(ref random, prefab, triggerAction);
					}
				}
				while (m_TriggerPrefabData.TryGetNextPrefab(triggerAction.m_TriggerType, targetType, triggerAction.m_TriggerPrefab, out prefab, ref iterator));
			}
		}

		private bool CheckInterval(Entity prefab)
		{
			if (m_TriggerLimitData.TryGetComponent(prefab, out var componentData) && m_TriggerFrames.TryGetValue(prefab, out var item))
			{
				return m_SimulationFrame >= item + componentData.m_FrameInterval;
			}
			return true;
		}

		private bool CheckConditions(Entity prefab, TriggerAction action)
		{
			if (m_TriggerConditions.HasBuffer(prefab))
			{
				DynamicBuffer<TriggerConditionData> dynamicBuffer = m_TriggerConditions[prefab];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					TriggerConditionData triggerConditionData = dynamicBuffer[i];
					switch (triggerConditionData.m_Type)
					{
					case TriggerConditionType.Equals:
						if (Math.Abs(triggerConditionData.m_Value - action.m_Value) > float.Epsilon)
						{
							return false;
						}
						break;
					case TriggerConditionType.GreaterThan:
						if (action.m_Value <= triggerConditionData.m_Value)
						{
							return false;
						}
						break;
					case TriggerConditionType.LessThan:
						if (action.m_Value >= triggerConditionData.m_Value)
						{
							return false;
						}
						break;
					}
				}
			}
			return true;
		}

		private void CreateEntity(ref Unity.Mathematics.Random random, Entity prefab, TriggerAction triggerAction)
		{
			if (m_TriggerLimitData.HasComponent(prefab))
			{
				m_TriggerFrames[prefab] = m_SimulationFrame;
			}
			if (m_ChirpData.HasBuffer(prefab))
			{
				m_ChirpQueue.Enqueue(new ChirpCreationData
				{
					m_TriggerPrefab = prefab,
					m_Sender = triggerAction.m_PrimaryTarget,
					m_Target = triggerAction.m_SecondaryTarget
				});
			}
			else if (m_LifePathEventData.HasComponent(prefab))
			{
				m_LifePathEventQueue.Enqueue(new LifePathEventCreationData
				{
					m_EventPrefab = prefab,
					m_Sender = triggerAction.m_PrimaryTarget,
					m_Target = triggerAction.m_SecondaryTarget,
					m_OriginalSender = Entity.Null
				});
			}
			else if (m_RadioEventData.HasComponent(prefab))
			{
				RadioEventData radioEventData = m_RadioEventData[prefab];
				if (radioEventData.m_SegmentType == Radio.SegmentType.Emergency)
				{
					m_EmergencyRadioTagQueue.Enqueue(new RadioTag
					{
						m_Event = prefab,
						m_Target = triggerAction.m_PrimaryTarget,
						m_SegmentType = radioEventData.m_SegmentType,
						m_EmergencyFrameDelay = radioEventData.m_EmergencyFrameDelay
					});
				}
				else
				{
					m_RadioTagQueue.Enqueue(new RadioTag
					{
						m_Event = prefab,
						m_Target = triggerAction.m_PrimaryTarget,
						m_SegmentType = radioEventData.m_SegmentType
					});
				}
			}
			else
			{
				if (!m_TutorialEventData.HasBuffer(prefab))
				{
					return;
				}
				DynamicBuffer<TutorialActivationEventData> dynamicBuffer = m_TutorialEventData[prefab];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (!m_LockedData.HasEnabledComponent(dynamicBuffer[i].m_Tutorial))
					{
						m_TutorialTriggerQueue.Enqueue(dynamicBuffer[i].m_Tutorial);
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Locked> __Game_Prefabs_Locked_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TriggerChirpData> __Game_Prefabs_TriggerChirpData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LifePathEventData> __Game_Prefabs_LifePathEventData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RadioEventData> __Game_Prefabs_RadioEventData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TutorialActivationEventData> __Game_Prefabs_TutorialActivationEventData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PolicyData> __Game_Prefabs_PolicyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TriggerConditionData> __Game_Prefabs_TriggerConditionData_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrafficAccidentData> __Game_Prefabs_TrafficAccidentData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TriggerLimitData> __Game_Prefabs_TriggerLimitData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_Locked_RO_ComponentLookup = state.GetComponentLookup<Locked>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Prefabs_TriggerChirpData_RO_BufferLookup = state.GetBufferLookup<TriggerChirpData>(isReadOnly: true);
			__Game_Prefabs_LifePathEventData_RO_ComponentLookup = state.GetComponentLookup<LifePathEventData>(isReadOnly: true);
			__Game_Prefabs_RadioEventData_RO_ComponentLookup = state.GetComponentLookup<RadioEventData>(isReadOnly: true);
			__Game_Prefabs_TutorialActivationEventData_RO_BufferLookup = state.GetBufferLookup<TutorialActivationEventData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PolicyData_RO_ComponentLookup = state.GetComponentLookup<PolicyData>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Prefabs_TriggerConditionData_RO_BufferLookup = state.GetBufferLookup<TriggerConditionData>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentLookup = state.GetComponentLookup<CullingInfo>(isReadOnly: true);
			__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup = state.GetComponentLookup<TrafficAccidentData>(isReadOnly: true);
			__Game_Prefabs_TriggerLimitData_RO_ComponentLookup = state.GetComponentLookup<TriggerLimitData>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TriggerPrefabSystem m_TriggerPrefabSystem;

	private ModificationEndBarrier m_ModificationBarrier;

	private List<NativeQueue<TriggerAction>> m_Queues;

	private JobHandle m_Dependencies;

	private CreateChirpSystem m_CreateChirpSystem;

	private LifePathEventSystem m_LifePathEventSystem;

	private RadioTagSystem m_RadioTagSystem;

	private TutorialEventActivationSystem m_TutorialEventActivationSystem;

	private DateTime m_LastTimedEventTime;

	private TimeSpan m_TimedEventInterval;

	private EntityQuery m_EDWSBuildingQuery;

	private NativeParallelHashMap<Entity, uint> m_TriggerFrames;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TriggerPrefabSystem = base.World.GetOrCreateSystemManaged<TriggerPrefabSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationEndBarrier>();
		m_CreateChirpSystem = base.World.GetOrCreateSystemManaged<CreateChirpSystem>();
		m_LifePathEventSystem = base.World.GetOrCreateSystemManaged<LifePathEventSystem>();
		m_RadioTagSystem = base.World.GetOrCreateSystemManaged<RadioTagSystem>();
		m_TutorialEventActivationSystem = base.World.GetOrCreateSystemManaged<TutorialEventActivationSystem>();
		m_Queues = new List<NativeQueue<TriggerAction>>();
		m_LastTimedEventTime = DateTime.MinValue;
		m_TimedEventInterval = new TimeSpan(0, 15, 0);
		m_TriggerFrames = new NativeParallelHashMap<Entity, uint>(32, Allocator.Persistent);
		m_EDWSBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.EarlyDisasterWarningSystem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		base.Enabled = false;
	}

	protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_Dependencies.Complete();
		for (int i = 0; i < m_Queues.Count; i++)
		{
			m_Queues[i].Dispose();
		}
		m_Queues.Clear();
		base.OnStopRunning();
	}

	public NativeQueue<TriggerAction> CreateActionBuffer()
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		NativeQueue<TriggerAction> nativeQueue = new NativeQueue<TriggerAction>(Allocator.TempJob);
		m_Queues.Add(nativeQueue);
		return nativeQueue;
	}

	public void AddActionBufferWriter(JobHandle handle)
	{
		m_Dependencies = JobHandle.CombineDependencies(m_Dependencies, handle);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.Dependency.Complete();
		m_Dependencies.Complete();
		int num = 0;
		for (int i = 0; i < m_Queues.Count; i++)
		{
			num += m_Queues[i].Count;
		}
		if (num == 0)
		{
			for (int j = 0; j < m_Queues.Count; j++)
			{
				m_Queues[j].Dispose();
			}
			m_Queues.Clear();
		}
		NativeArray<TriggerAction> actions = new NativeArray<TriggerAction>(num, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
		num = 0;
		for (int k = 0; k < m_Queues.Count; k++)
		{
			NativeQueue<TriggerAction> nativeQueue = m_Queues[k];
			int count = nativeQueue.Count;
			for (int l = 0; l < count; l++)
			{
				actions[num++] = nativeQueue.Dequeue();
			}
			nativeQueue.Dispose();
		}
		m_Queues.Clear();
		JobHandle dependencies;
		JobHandle deps;
		JobHandle deps2;
		JobHandle deps3;
		JobHandle deps4;
		JobHandle dependency;
		TriggerActionJob jobData = new TriggerActionJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_TriggerPrefabData = m_TriggerPrefabSystem.ReadTriggerPrefabData(out dependencies),
			m_Actions = actions,
			m_ChirpQueue = m_CreateChirpSystem.GetQueue(out deps),
			m_TriggerFrames = m_TriggerFrames,
			m_LifePathEventQueue = m_LifePathEventSystem.GetQueue(out deps2),
			m_RadioTagQueue = m_RadioTagSystem.GetInputQueue(out deps3),
			m_EmergencyRadioTagQueue = m_RadioTagSystem.GetEmergencyInputQueue(out deps4),
			m_TutorialTriggerQueue = m_TutorialEventActivationSystem.GetQueue(out dependency),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LockedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_Locked_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ChirpData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_TriggerChirpData_RO_BufferLookup, ref base.CheckedStateRef),
			m_LifePathEventData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LifePathEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RadioEventData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RadioEventData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TutorialEventData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_TutorialActivationEventData_RO_BufferLookup, ref base.CheckedStateRef),
			m_CitizenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PolicyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PolicyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerConditions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_TriggerConditionData_RO_BufferLookup, ref base.CheckedStateRef),
			m_CullingInfo = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrafficAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrafficAccidentData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TriggerLimitData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TriggerLimitData_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		Camera main = Camera.main;
		if (main != null)
		{
			jobData.m_CameraPosition = main.transform.position;
		}
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, dependencies, deps2, deps, deps3, deps4, dependency));
		m_CreateChirpSystem.AddQueueWriter(jobHandle);
		m_LifePathEventSystem.AddQueueWriter(jobHandle);
		m_RadioTagSystem.AddInputQueueWriter(jobHandle);
		m_RadioTagSystem.AddEmergencyInputQueueWriter(jobHandle);
		m_TriggerPrefabSystem.AddReader(jobHandle);
		m_TutorialEventActivationSystem.AddQueueWriter(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
		if (DateTime.Now - m_LastTimedEventTime >= m_TimedEventInterval)
		{
			Telemetry.CityStats();
			m_LastTimedEventTime = DateTime.Now;
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_TriggerFrames.Dispose();
	}

	public void SetDefaults(Context context)
	{
		m_TriggerFrames.Clear();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NativeKeyValueArrays<Entity, uint> keyValueArrays = m_TriggerFrames.GetKeyValueArrays(Allocator.Temp);
		int length = keyValueArrays.Length;
		writer.Write(length);
		for (int i = 0; i < keyValueArrays.Length; i++)
		{
			Entity value = keyValueArrays.Keys[i];
			writer.Write(value);
			uint value2 = keyValueArrays.Values[i];
			writer.Write(value2);
		}
		keyValueArrays.Dispose();
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_TriggerFrames.Clear();
		reader.Read(out int value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out Entity value2);
			reader.Read(out uint value3);
			if (value2 != Entity.Null)
			{
				m_TriggerFrames.Add(value2, value3);
			}
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
	public TriggerSystem()
	{
	}
}
