using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PathfindSetupSystem : GameSystemBase, IPreDeserialize
{
	public struct SetupData
	{
		[ReadOnly]
		private int m_StartIndex;

		[ReadOnly]
		private int m_Length;

		[ReadOnly]
		private NativeList<SetupListItem> m_SetupItems;

		[ReadOnly]
		private PathfindTargetSeekerData m_SeekerData;

		private NativeQueue<PathfindSetupTarget>.ParallelWriter m_TargetQueue;

		public int Length => m_Length;

		public SetupData(int startIndex, int endIndex, NativeList<SetupListItem> setupItems, PathfindTargetSeekerData seekerData, NativeQueue<PathfindSetupTarget>.ParallelWriter targetQueue)
		{
			m_StartIndex = startIndex;
			m_Length = endIndex - startIndex;
			m_SetupItems = setupItems;
			m_SeekerData = seekerData;
			m_TargetQueue = targetQueue;
		}

		public void GetItem(int index, out Entity entity, out PathfindTargetSeeker<PathfindSetupBuffer> targetSeeker)
		{
			SetupListItem setupListItem = m_SetupItems[m_StartIndex + index];
			entity = ((setupListItem.m_Target.m_Entity != Entity.Null) ? setupListItem.m_Target.m_Entity : setupListItem.m_Owner);
			PathfindSetupBuffer buffer = new PathfindSetupBuffer
			{
				m_Queue = m_TargetQueue,
				m_SetupIndex = index
			};
			targetSeeker = new PathfindTargetSeeker<PathfindSetupBuffer>(m_SeekerData, setupListItem.m_Parameters, setupListItem.m_Target, buffer, setupListItem.m_RandomSeed, setupListItem.m_ActionStart);
		}

		public void GetItem(int index, out Entity entity, out Entity owner, out PathfindTargetSeeker<PathfindSetupBuffer> targetSeeker)
		{
			SetupListItem setupListItem = m_SetupItems[m_StartIndex + index];
			entity = ((setupListItem.m_Target.m_Entity != Entity.Null) ? setupListItem.m_Target.m_Entity : setupListItem.m_Owner);
			owner = setupListItem.m_Owner;
			PathfindSetupBuffer buffer = new PathfindSetupBuffer
			{
				m_Queue = m_TargetQueue,
				m_SetupIndex = index
			};
			targetSeeker = new PathfindTargetSeeker<PathfindSetupBuffer>(m_SeekerData, setupListItem.m_Parameters, setupListItem.m_Target, buffer, setupListItem.m_RandomSeed, setupListItem.m_ActionStart);
		}
	}

	public struct SetupListItem : IComparable<SetupListItem>
	{
		public SetupQueueTarget m_Target;

		public PathfindParameters m_Parameters;

		public UnsafeList<PathTarget> m_Buffer;

		public Entity m_Owner;

		public RandomSeed m_RandomSeed;

		public int m_ActionIndex;

		public bool m_ActionStart;

		public int CompareTo(SetupListItem other)
		{
			return m_Target.m_Type - other.m_Target.m_Type;
		}

		public SetupListItem(SetupQueueTarget target, PathfindParameters parameters, Entity owner, RandomSeed randomSeed, int actionIndex, bool actionStart)
		{
			m_Target = target;
			m_Parameters = parameters;
			m_Buffer = new UnsafeList<PathTarget>(0, Allocator.Persistent);
			m_Owner = owner;
			m_RandomSeed = randomSeed;
			m_ActionIndex = actionIndex;
			m_ActionStart = actionStart;
		}
	}

	private struct ActionListItem
	{
		public PathfindAction m_Action;

		public Entity m_Owner;

		public uint m_ResultFrame;

		public object m_System;

		public ActionListItem(PathfindAction action, Entity owner, uint resultFrame, object system)
		{
			m_Action = action;
			m_Owner = owner;
			m_ResultFrame = resultFrame;
			m_System = system;
		}
	}

	private struct SetupQueue
	{
		public NativeQueue<SetupQueueItem> m_Queue;

		public uint m_ResultFrame;

		public uint m_SpreadFrame;

		public object m_System;
	}

	[BurstCompile]
	private struct DequePathTargetsJob : IJob
	{
		[NativeDisableUnsafePtrRestriction]
		public unsafe SetupListItem* m_SetupItems;

		public NativeQueue<PathfindSetupTarget> m_TargetQueue;

		public unsafe void Execute()
		{
			PathfindSetupTarget item;
			while (m_TargetQueue.TryDequeue(out item))
			{
				ref SetupListItem reference = ref m_SetupItems[item.m_SetupIndex];
				if ((reference.m_Parameters.m_PathfindFlags & PathfindFlags.SkipPathfind) != 0)
				{
					if (reference.m_Buffer.Length == 0)
					{
						reference.m_Buffer.Add(in item.m_PathTarget);
						continue;
					}
					ref PathTarget reference2 = ref reference.m_Buffer.ElementAt(0);
					if (item.m_PathTarget.m_Cost < reference2.m_Cost)
					{
						reference2 = item.m_PathTarget;
					}
				}
				else
				{
					reference.m_Buffer.Add(in item.m_PathTarget);
				}
			}
		}
	}

	private PathfindTargetSeekerData m_TargetSeekerData;

	private CommonPathfindSetup m_CommonPathfindSetup;

	private PostServicePathfindSetup m_PostServicePathfindSetup;

	private GarbagePathfindSetup m_GarbagePathfindSetup;

	private TransportPathfindSetup m_TransportPathfindSetup;

	private PolicePathfindSetup m_PolicePathfindSetup;

	private FirePathfindSetup m_FirePathfindSetup;

	private HealthcarePathfindSetup m_HealthcarePathfindSetup;

	private AreaPathfindSetup m_AreaPathfindSetup;

	private RoadPathfindSetup m_RoadPathfindSetup;

	private CitizenPathfindSetup m_CitizenPathfindSetup;

	private ResourcePathfindSetup m_ResourcePathfindSetup;

	private GoodsDeliveryPathfindSetup m_GoodsDeliveryPathfindSetup;

	private SimulationSystem m_SimulationSystem;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private AirwaySystem m_AirwaySystem;

	private NativeList<SetupListItem> m_SetupList;

	private List<SetupQueue> m_ActiveQueues;

	private List<SetupQueue> m_FreeQueues;

	private List<ActionListItem> m_ActionList;

	private JobHandle m_QueueDependencies;

	private JobHandle m_SetupDependencies;

	private uint m_QueueSimulationFrameIndex;

	private uint m_SetupSimulationFrameIndex;

	private int m_PendingRequestCount;

	public uint pendingSimulationFrame => math.min(m_QueueSimulationFrameIndex, m_SetupSimulationFrameIndex);

	public int pendingRequestCount => m_PendingRequestCount;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TargetSeekerData = new PathfindTargetSeekerData(this);
		m_CommonPathfindSetup = new CommonPathfindSetup(this);
		m_PostServicePathfindSetup = new PostServicePathfindSetup(this);
		m_GarbagePathfindSetup = new GarbagePathfindSetup(this);
		m_TransportPathfindSetup = new TransportPathfindSetup(this);
		m_PolicePathfindSetup = new PolicePathfindSetup(this);
		m_FirePathfindSetup = new FirePathfindSetup(this);
		m_HealthcarePathfindSetup = new HealthcarePathfindSetup(this);
		m_AreaPathfindSetup = new AreaPathfindSetup(this);
		m_RoadPathfindSetup = new RoadPathfindSetup(this);
		m_CitizenPathfindSetup = new CitizenPathfindSetup(this);
		m_ResourcePathfindSetup = new ResourcePathfindSetup(this);
		m_GoodsDeliveryPathfindSetup = new GoodsDeliveryPathfindSetup(this);
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_SetupList = new NativeList<SetupListItem>(100, Allocator.Persistent);
		m_ActiveQueues = new List<SetupQueue>(10);
		m_FreeQueues = new List<SetupQueue>(10);
		m_ActionList = new List<ActionListItem>(50);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_QueueDependencies.Complete();
		for (int i = 0; i < m_ActiveQueues.Count; i++)
		{
			m_ActiveQueues[i].m_Queue.Dispose();
		}
		for (int j = 0; j < m_FreeQueues.Count; j++)
		{
			m_FreeQueues[j].m_Queue.Dispose();
		}
		m_ActiveQueues.Clear();
		m_FreeQueues.Clear();
		m_SetupDependencies.Complete();
		for (int k = 0; k < m_SetupList.Length; k++)
		{
			m_SetupList[k].m_Buffer.Dispose();
		}
		for (int l = 0; l < m_ActionList.Count; l++)
		{
			m_ActionList[l].m_Action.Dispose();
		}
		m_SetupList.Dispose();
		m_ActionList.Clear();
		base.OnDestroy();
	}

	public void PreDeserialize(Context context)
	{
		m_QueueDependencies.Complete();
		for (int i = 0; i < m_ActiveQueues.Count; i++)
		{
			m_ActiveQueues[i].m_Queue.Dispose();
		}
		for (int j = 0; j < m_FreeQueues.Count; j++)
		{
			m_FreeQueues[j].m_Queue.Dispose();
		}
		m_ActiveQueues.Clear();
		m_FreeQueues.Clear();
		m_SetupDependencies.Complete();
		for (int k = 0; k < m_SetupList.Length; k++)
		{
			m_SetupList[k].m_Buffer.Dispose();
		}
		for (int l = 0; l < m_ActionList.Count; l++)
		{
			m_ActionList[l].m_Action.Dispose();
		}
		m_SetupList.Clear();
		m_ActionList.Clear();
		m_QueueSimulationFrameIndex = uint.MaxValue;
		m_SetupSimulationFrameIndex = uint.MaxValue;
		m_PendingRequestCount = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ActiveQueues.Count == 0)
		{
			return;
		}
		CompleteSetup();
		m_TargetSeekerData.Update(this, m_AirwaySystem.GetAirwayData());
		m_QueueDependencies.Complete();
		m_QueueDependencies = default(JobHandle);
		int num = 0;
		for (int i = 0; i < m_ActiveQueues.Count; i++)
		{
			SetupQueue setupQueue = m_ActiveQueues[i];
			int num2 = int.MaxValue;
			if (setupQueue.m_SpreadFrame > m_SimulationSystem.frameIndex)
			{
				float num3 = ((m_SimulationSystem.smoothSpeed == 0f) ? 1f : (UnityEngine.Time.deltaTime * m_SimulationSystem.smoothSpeed * 60f));
				float num4 = (float)(setupQueue.m_SpreadFrame - m_SimulationSystem.frameIndex) + num3;
				num2 = (int)math.ceil((float)setupQueue.m_Queue.Count * (num3 / num4));
			}
			SetupQueueItem item;
			while (num2-- != 0 && setupQueue.m_Queue.TryDequeue(out item))
			{
				if (item.m_Parameters.m_ParkingTarget != Entity.Null && base.EntityManager.HasComponent<ConnectionLane>(item.m_Parameters.m_ParkingTarget))
				{
					item.m_Parameters.m_ParkingDelta = -1f;
				}
				PathfindAction action = new PathfindAction(0, 0, Allocator.Persistent, item.m_Parameters, item.m_Origin.m_Type, item.m_Destination.m_Type);
				m_SetupList.Add(new SetupListItem(item.m_Origin, item.m_Parameters, item.m_Owner, RandomSeed.Next(), m_ActionList.Count, actionStart: true));
				m_SetupList.Add(new SetupListItem(item.m_Destination, item.m_Parameters, item.m_Owner, RandomSeed.Next(), m_ActionList.Count, actionStart: false));
				m_ActionList.Add(new ActionListItem(action, item.m_Owner, setupQueue.m_ResultFrame, setupQueue.m_System));
			}
			if (setupQueue.m_Queue.IsEmpty())
			{
				m_FreeQueues.Add(setupQueue);
			}
			else
			{
				m_ActiveQueues[num++] = setupQueue;
			}
		}
		if (m_ActiveQueues.Count > num)
		{
			m_ActiveQueues.RemoveRange(num, m_ActiveQueues.Count - num);
		}
		if (m_SetupList.Length == 0)
		{
			m_QueueSimulationFrameIndex = uint.MaxValue;
			return;
		}
		m_SetupList.Sort();
		m_SetupSimulationFrameIndex = m_QueueSimulationFrameIndex;
		m_QueueSimulationFrameIndex = uint.MaxValue;
		m_PendingRequestCount = m_ActionList.Count;
		int num5 = 0;
		int j = 1;
		SetupTargetType setupTargetType = m_SetupList[num5].m_Target.m_Type;
		for (; j < m_SetupList.Length; j++)
		{
			SetupTargetType type = m_SetupList[j].m_Target.m_Type;
			if (setupTargetType != type)
			{
				FindTargets(num5, j);
				num5 = j;
				setupTargetType = type;
			}
		}
		FindTargets(num5, j);
	}

	public void CompleteSetup()
	{
		m_SetupSimulationFrameIndex = uint.MaxValue;
		m_PendingRequestCount = 0;
		m_SetupDependencies.Complete();
		m_SetupDependencies = default(JobHandle);
		for (int i = 0; i < m_SetupList.Length; i++)
		{
			SetupListItem setupListItem = m_SetupList[i];
			ActionListItem value = m_ActionList[setupListItem.m_ActionIndex];
			if (setupListItem.m_ActionStart)
			{
				value.m_Action.data.m_StartTargets = setupListItem.m_Buffer;
			}
			else
			{
				value.m_Action.data.m_EndTargets = setupListItem.m_Buffer;
			}
			m_ActionList[setupListItem.m_ActionIndex] = value;
		}
		for (int j = 0; j < m_ActionList.Count; j++)
		{
			ActionListItem actionListItem = m_ActionList[j];
			m_PathfindQueueSystem.Enqueue(actionListItem.m_Action, actionListItem.m_Owner, m_SetupDependencies, actionListItem.m_ResultFrame, actionListItem.m_System);
		}
		m_SetupList.Clear();
		m_ActionList.Clear();
	}

	public NativeQueue<SetupQueueItem> GetQueue(object system, int maxDelayFrames, int spreadFrames = 0)
	{
		SetupQueue item;
		if (m_FreeQueues.Count != 0)
		{
			item = m_FreeQueues[m_FreeQueues.Count - 1];
			m_FreeQueues.RemoveAt(m_FreeQueues.Count - 1);
		}
		else
		{
			item = new SetupQueue
			{
				m_Queue = new NativeQueue<SetupQueueItem>(Allocator.Persistent)
			};
		}
		item.m_ResultFrame = m_SimulationSystem.frameIndex + (uint)maxDelayFrames;
		item.m_SpreadFrame = m_SimulationSystem.frameIndex + (uint)spreadFrames;
		if (item.m_ResultFrame < m_SimulationSystem.frameIndex)
		{
			item.m_ResultFrame = uint.MaxValue;
		}
		m_QueueSimulationFrameIndex = math.min(m_QueueSimulationFrameIndex, item.m_ResultFrame);
		item.m_System = system;
		m_ActiveQueues.Add(item);
		return item.m_Queue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_QueueDependencies = JobHandle.CombineDependencies(m_QueueDependencies, handle);
	}

	public EntityQuery GetSetupQuery(params EntityQueryDesc[] entityQueryDesc)
	{
		return GetEntityQuery(entityQueryDesc);
	}

	public EntityQuery GetSetupQuery(params ComponentType[] componentTypes)
	{
		return GetEntityQuery(componentTypes);
	}

	private unsafe void FindTargets(int startIndex, int endIndex)
	{
		SetupListItem setupListItem = m_SetupList[startIndex];
		NativeQueue<PathfindSetupTarget> targetQueue = new NativeQueue<PathfindSetupTarget>(Allocator.TempJob);
		SetupData setupData = new SetupData(startIndex, endIndex, m_SetupList, m_TargetSeekerData, targetQueue.AsParallelWriter());
		DequePathTargetsJob jobData = new DequePathTargetsJob
		{
			m_SetupItems = m_SetupList.GetUnsafeReadOnlyPtr() + startIndex,
			m_TargetQueue = targetQueue
		};
		JobHandle jobHandle = FindTargets(setupListItem.m_Target.m_Type, in setupData);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, jobHandle);
		base.Dependency = JobHandle.CombineDependencies(base.Dependency, jobHandle);
		m_SetupDependencies = JobHandle.CombineDependencies(m_SetupDependencies, jobHandle2);
		targetQueue.Dispose(jobHandle2);
	}

	private JobHandle FindTargets(SetupTargetType targetType, in SetupData setupData)
	{
		switch (targetType)
		{
		case SetupTargetType.CurrentLocation:
			return m_CommonPathfindSetup.SetupCurrentLocation(this, setupData, base.Dependency);
		case SetupTargetType.AccidentLocation:
			return m_CommonPathfindSetup.SetupAccidentLocation(this, setupData, base.Dependency);
		case SetupTargetType.Safety:
			return m_CommonPathfindSetup.SetupSafety(this, setupData, base.Dependency);
		case SetupTargetType.PostVan:
			return m_PostServicePathfindSetup.SetupPostVans(this, setupData, base.Dependency);
		case SetupTargetType.MailTransfer:
			return m_PostServicePathfindSetup.SetupMailTransfer(this, setupData, base.Dependency);
		case SetupTargetType.MailBox:
			return m_PostServicePathfindSetup.SetupMailBoxes(this, setupData, base.Dependency);
		case SetupTargetType.PostVanRequest:
			return m_PostServicePathfindSetup.SetupPostVanRequest(this, setupData, base.Dependency);
		case SetupTargetType.GarbageCollector:
			return m_GarbagePathfindSetup.SetupGarbageCollector(this, setupData, base.Dependency);
		case SetupTargetType.GarbageTransfer:
			return m_GarbagePathfindSetup.SetupGarbageTransfer(this, setupData, base.Dependency);
		case SetupTargetType.GarbageCollectorRequest:
			return m_GarbagePathfindSetup.SetupGarbageCollectorRequest(this, setupData, base.Dependency);
		case SetupTargetType.Taxi:
			return m_TransportPathfindSetup.SetupTaxi(this, setupData, base.Dependency);
		case SetupTargetType.TransportVehicle:
			return m_TransportPathfindSetup.SetupTransportVehicle(this, setupData, base.Dependency);
		case SetupTargetType.RouteWaypoints:
			return m_TransportPathfindSetup.SetupRouteWaypoints(this, setupData, base.Dependency);
		case SetupTargetType.TransportVehicleRequest:
			return m_TransportPathfindSetup.SetupTransportVehicleRequest(this, setupData, base.Dependency);
		case SetupTargetType.TaxiRequest:
			return m_TransportPathfindSetup.SetupTaxiRequest(this, setupData, base.Dependency);
		case SetupTargetType.CrimeProducer:
			return m_PolicePathfindSetup.SetupCrimeProducer(this, setupData, base.Dependency);
		case SetupTargetType.PolicePatrol:
			return m_PolicePathfindSetup.SetupPolicePatrols(this, setupData, base.Dependency);
		case SetupTargetType.PrisonerTransport:
			return m_PolicePathfindSetup.SetupPrisonerTransport(this, setupData, base.Dependency);
		case SetupTargetType.PrisonerTransportRequest:
			return m_PolicePathfindSetup.SetupPrisonerTransportRequest(this, setupData, base.Dependency);
		case SetupTargetType.PoliceRequest:
			return m_PolicePathfindSetup.SetupPoliceRequest(this, setupData, base.Dependency);
		case SetupTargetType.GoodsDelivery:
			return m_GoodsDeliveryPathfindSetup.SetupGoodsDelivery(this, setupData, base.Dependency);
		case SetupTargetType.EmergencyShelter:
			return m_FirePathfindSetup.SetupEmergencyShelters(this, setupData, base.Dependency);
		case SetupTargetType.EvacuationTransport:
			return m_FirePathfindSetup.SetupEvacuationTransport(this, setupData, base.Dependency);
		case SetupTargetType.FireEngine:
			return m_FirePathfindSetup.SetupFireEngines(this, setupData, base.Dependency);
		case SetupTargetType.EvacuationRequest:
			return m_FirePathfindSetup.SetupEvacuationRequest(this, setupData, base.Dependency);
		case SetupTargetType.FireRescueRequest:
			return m_FirePathfindSetup.SetupFireRescueRequest(this, setupData, base.Dependency);
		case SetupTargetType.Ambulance:
			return m_HealthcarePathfindSetup.SetupAmbulances(this, setupData, base.Dependency);
		case SetupTargetType.Hospital:
			return m_HealthcarePathfindSetup.SetupHospitals(this, setupData, base.Dependency);
		case SetupTargetType.Hearse:
			return m_HealthcarePathfindSetup.SetupHearses(this, setupData, base.Dependency);
		case SetupTargetType.HealthcareRequest:
			return m_HealthcarePathfindSetup.SetupHealthcareRequest(this, setupData, base.Dependency);
		case SetupTargetType.AreaLocation:
			return m_AreaPathfindSetup.SetupAreaLocation(this, setupData, base.Dependency);
		case SetupTargetType.WoodResource:
			return m_AreaPathfindSetup.SetupWoodResource(this, setupData, base.Dependency);
		case SetupTargetType.Maintenance:
			return m_RoadPathfindSetup.SetupMaintenanceProviders(this, setupData, base.Dependency);
		case SetupTargetType.RandomTraffic:
			return m_RoadPathfindSetup.SetupRandomTraffic(this, setupData, base.Dependency);
		case SetupTargetType.OutsideConnection:
			return m_RoadPathfindSetup.SetupOutsideConnections(this, setupData, base.Dependency);
		case SetupTargetType.MaintenanceRequest:
			return m_RoadPathfindSetup.SetupMaintenanceRequest(this, setupData, base.Dependency);
		case SetupTargetType.TouristFindTarget:
			return m_CitizenPathfindSetup.SetupTouristTarget(this, setupData, base.Dependency);
		case SetupTargetType.Leisure:
			return m_CitizenPathfindSetup.SetupLeisureTarget(this, setupData, base.Dependency);
		case SetupTargetType.SchoolSeekerTo:
			return m_CitizenPathfindSetup.SetupSchoolSeekerTo(this, setupData, base.Dependency);
		case SetupTargetType.JobSeekerTo:
			return m_CitizenPathfindSetup.SetupJobSeekerTo(this, setupData, base.Dependency);
		case SetupTargetType.Attraction:
			return m_CitizenPathfindSetup.SetupAttraction(this, setupData, base.Dependency);
		case SetupTargetType.HomelessShelter:
			return m_CitizenPathfindSetup.SetupHomeless(this, setupData, base.Dependency);
		case SetupTargetType.FindHome:
			return m_CitizenPathfindSetup.SetupFindHome(this, setupData, base.Dependency);
		case SetupTargetType.Sightseeing:
			return default(JobHandle);
		case SetupTargetType.ResourceSeller:
			return m_ResourcePathfindSetup.SetupResourceSeller(this, setupData, base.Dependency);
		case SetupTargetType.ResourceExport:
			return m_ResourcePathfindSetup.SetupResourceExport(this, setupData, base.Dependency);
		case SetupTargetType.StorageTransfer:
			return m_ResourcePathfindSetup.SetupStorageTransfer(this, setupData, base.Dependency);
		default:
			UnityEngine.Debug.LogWarning("Invalid target type in Pathfind setup " + targetType);
			return default(JobHandle);
		}
	}

	[Preserve]
	public PathfindSetupSystem()
	{
	}
}
