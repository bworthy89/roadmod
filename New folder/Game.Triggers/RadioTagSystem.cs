#define UNITY_ASSERTIONS
using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game.Audio.Radio;
using Game.Simulation;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Triggers;

public class RadioTagSystem : GameSystemBase
{
	private static readonly int kFrameDelay = 50000;

	private static readonly int kMaxBufferSize = 10;

	private NativeParallelHashMap<Entity, uint> m_RecentTags;

	private NativeQueue<RadioTag> m_InputQueue;

	private NativeQueue<RadioTag> m_EmergencyInputQueue;

	private NativeQueue<RadioTag> m_EmergencyQueue;

	private Dictionary<Radio.SegmentType, List<RadioTag>> m_Events;

	private JobHandle m_InputDependencies;

	private JobHandle m_EmergencyInputDependencies;

	private JobHandle m_EmergencyDependencies;

	private SimulationSystem m_SimulationSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_InputQueue = new NativeQueue<RadioTag>(Allocator.Persistent);
		m_EmergencyInputQueue = new NativeQueue<RadioTag>(Allocator.Persistent);
		m_EmergencyQueue = new NativeQueue<RadioTag>(Allocator.Persistent);
		m_RecentTags = new NativeParallelHashMap<Entity, uint>(0, Allocator.Persistent);
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_Events = new Dictionary<Radio.SegmentType, List<RadioTag>>();
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
		m_InputDependencies.Complete();
		m_EmergencyInputDependencies.Complete();
		m_EmergencyDependencies.Complete();
		RadioTag item;
		while (m_InputQueue.TryDequeue(out item))
		{
			List<RadioTag> list = EnsureList(item.m_SegmentType);
			if (!m_RecentTags.TryGetValue(item.m_Event, out var item2) || m_SimulationSystem.frameIndex >= item2 + kFrameDelay)
			{
				while (list.Contains(item))
				{
					list.Remove(item);
				}
				list.Add(item);
				list.RemoveRange(0, math.max(list.Count - kMaxBufferSize, 0));
				m_RecentTags[item.m_Event] = m_SimulationSystem.frameIndex;
			}
		}
		RadioTag item3;
		while (m_EmergencyInputQueue.TryDequeue(out item3))
		{
			if (!m_RecentTags.TryGetValue(item3.m_Event, out var item4) || m_SimulationSystem.frameIndex >= item4 + item3.m_EmergencyFrameDelay)
			{
				m_EmergencyQueue.Enqueue(item3);
				m_RecentTags[item3.m_Event] = m_SimulationSystem.frameIndex;
			}
		}
	}

	private List<RadioTag> EnsureList(Radio.SegmentType segmentType)
	{
		if (!m_Events.ContainsKey(segmentType))
		{
			m_Events.Add(segmentType, new List<RadioTag>());
		}
		return m_Events[segmentType];
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		Clear();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_InputQueue.Dispose();
		m_EmergencyInputQueue.Dispose();
		m_EmergencyQueue.Dispose();
		m_RecentTags.Dispose();
		base.OnDestroy();
	}

	public bool TryPopEvent(Radio.SegmentType segmentType, bool newestFirst, out RadioTag radioTag)
	{
		List<RadioTag> list = EnsureList(segmentType);
		if (list.Count > 0)
		{
			int index = (newestFirst ? (list.Count - 1) : 0);
			radioTag = list[index];
			list.RemoveAt(index);
			return true;
		}
		radioTag = default(RadioTag);
		return false;
	}

	public void FlushEvents(Radio.SegmentType segmentType)
	{
		EnsureList(segmentType).Clear();
	}

	public NativeQueue<RadioTag> GetInputQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_InputDependencies;
		return m_InputQueue;
	}

	public NativeQueue<RadioTag> GetEmergencyInputQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_EmergencyInputDependencies;
		return m_EmergencyInputQueue;
	}

	public NativeQueue<RadioTag> GetEmergencyQueue(out JobHandle deps)
	{
		Assert.IsTrue(base.Enabled, "Can not write to queue when system isn't running");
		deps = m_EmergencyDependencies;
		return m_EmergencyQueue;
	}

	public void AddInputQueueWriter(JobHandle handle)
	{
		m_InputDependencies = JobHandle.CombineDependencies(m_InputDependencies, handle);
	}

	public void AddEmergencyInputQueueWriter(JobHandle handle)
	{
		m_EmergencyInputDependencies = JobHandle.CombineDependencies(m_EmergencyInputDependencies, handle);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		Clear();
	}

	private void Clear()
	{
		m_InputDependencies.Complete();
		m_EmergencyInputDependencies.Complete();
		m_EmergencyDependencies.Complete();
		m_InputQueue.Clear();
		m_EmergencyInputQueue.Clear();
		m_EmergencyQueue.Clear();
		m_RecentTags.Clear();
		m_Events.Clear();
	}

	[Preserve]
	public RadioTagSystem()
	{
	}
}
