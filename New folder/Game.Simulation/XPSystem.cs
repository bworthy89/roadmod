using System.Runtime.CompilerServices;
using Game.City;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class XPSystem : GameSystemBase, IXPSystem
{
	private struct XPQueueProcessJob : IJob
	{
		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public uint m_FrameIndex;

		public ComponentLookup<XP> m_CityXPs;

		public NativeQueue<XPGain> m_XPQueue;

		public NativeQueue<XPMessage> m_XPMessages;

		public void Execute()
		{
			XP value = m_CityXPs[m_City];
			XPGain item;
			while (m_XPQueue.TryDequeue(out item))
			{
				if (item.amount != 0)
				{
					value.m_XP += item.amount;
					m_XPMessages.Enqueue(new XPMessage(m_FrameIndex, item.amount, item.reason));
				}
			}
			m_CityXPs[m_City] = value;
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<XP> __Game_City_XP_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_XP_RW_ComponentLookup = state.GetComponentLookup<XP>();
		}
	}

	private NativeQueue<XPMessage> m_XPMessages;

	private NativeQueue<XPGain> m_XPQueue;

	private JobHandle m_QueueWriters;

	private CitySystem m_CitySystem;

	private SimulationSystem m_SimulationSystem;

	private TypeHandle __TypeHandle;

	public void TransferMessages(IXPMessageHandler handler)
	{
		base.Dependency.Complete();
		while (m_XPMessages.Count > 0)
		{
			XPMessage message = m_XPMessages.Dequeue();
			handler.AddMessage(message);
		}
	}

	public NativeQueue<XPGain> GetQueue(out JobHandle deps)
	{
		deps = m_QueueWriters;
		return m_XPQueue;
	}

	public void AddQueueWriter(JobHandle handle)
	{
		m_QueueWriters = JobHandle.CombineDependencies(m_QueueWriters, handle);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_XPMessages = new NativeQueue<XPMessage>(Allocator.Persistent);
		m_XPQueue = new NativeQueue<XPGain>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_XPQueue.Dispose();
		m_XPMessages.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!(m_CitySystem.City == Entity.Null))
		{
			XPQueueProcessJob jobData = new XPQueueProcessJob
			{
				m_City = m_CitySystem.City,
				m_FrameIndex = m_SimulationSystem.frameIndex,
				m_XPMessages = m_XPMessages,
				m_XPQueue = m_XPQueue,
				m_CityXPs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_XP_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(m_QueueWriters, base.Dependency));
			m_QueueWriters = base.Dependency;
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
	public XPSystem()
	{
	}
}
