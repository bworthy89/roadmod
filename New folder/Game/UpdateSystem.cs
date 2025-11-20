using System;
using System.Collections.Generic;
using Colossal.Entities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game;

public class UpdateSystem : GameSystemBase
{
	private struct SystemData : IComparable<SystemData>
	{
		public SystemUpdatePhase m_Phase;

		public int m_Interval;

		public int m_Offset;

		public int m_AddIndex;

		public int m_ResetInterval;

		public ComponentSystemBase m_System;

		public SystemData(SystemUpdatePhase phase, int interval, int offset, int addIndex, ComponentSystemBase system)
		{
			m_Phase = phase;
			m_Interval = interval;
			m_Offset = offset;
			m_AddIndex = addIndex;
			m_System = system;
			m_ResetInterval = ((system is GameSystemBase) ? interval : int.MaxValue);
		}

		public int CompareTo(SystemData other)
		{
			int num = m_Phase - other.m_Phase;
			if (num == 0)
			{
				num = m_AddIndex - other.m_AddIndex;
			}
			return num;
		}
	}

	private struct IntervalData : IComparable<IntervalData>
	{
		public int m_Interval;

		public int m_UpdateStart;

		public int m_UpdateIndex;

		public IntervalData(int interval, int updateStart, int updateIndex)
		{
			m_Interval = interval;
			m_UpdateStart = updateStart;
			m_UpdateIndex = updateIndex;
		}

		public int CompareTo(IntervalData other)
		{
			int num = m_Interval - other.m_Interval;
			if (num == 0)
			{
				num = m_UpdateIndex - other.m_UpdateIndex;
			}
			return num;
		}
	}

	private List<IGPUSystem> m_GPUSystems;

	private List<SystemData> m_Systems;

	private List<SystemData> m_Updates;

	private List<int2> m_UpdateRanges;

	private Dictionary<ComponentSystemBase, List<SystemData>> m_RefMap;

	private int m_AddIndex;

	private bool m_IsDirty;

	public SystemUpdatePhase currentPhase { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		RenderPipelineManager.beginFrameRendering += OnBeginFrame;
		m_GPUSystems = new List<IGPUSystem>();
		m_Systems = new List<SystemData>(1000);
		m_Updates = new List<SystemData>(1000);
		m_UpdateRanges = new List<int2>(100);
		m_RefMap = new Dictionary<ComponentSystemBase, List<SystemData>>(100);
		currentPhase = SystemUpdatePhase.Invalid;
	}

	protected virtual void OnBeginFrame(ScriptableRenderContext renderContext, Camera[] cameras)
	{
		foreach (IGPUSystem gPUSystem in m_GPUSystems)
		{
			if (gPUSystem.Enabled)
			{
				CommandBuffer commandBuffer = CommandBufferPool.Get("");
				if (gPUSystem.IsAsync)
				{
					commandBuffer.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
				}
				gPUSystem.OnSimulateGPU(commandBuffer);
				if (gPUSystem.IsAsync)
				{
					renderContext.ExecuteCommandBufferAsync(commandBuffer, ComputeQueueType.Default);
				}
				else
				{
					renderContext.ExecuteCommandBuffer(commandBuffer);
				}
				renderContext.Submit();
				commandBuffer.Clear();
				CommandBufferPool.Release(commandBuffer);
			}
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	[Preserve]
	protected override void OnDestroy()
	{
		RenderPipelineManager.beginFrameRendering -= OnBeginFrame;
		m_GPUSystems.Clear();
		base.OnDestroy();
	}

	public void RegisterGPUSystem<SystemType>() where SystemType : ComponentSystemBase, IGPUSystem
	{
		RegisterGPUSystem(base.World.GetOrCreateSystemManaged<SystemType>());
	}

	public void RegisterGPUSystem(IGPUSystem system)
	{
		if (!m_GPUSystems.Contains(system))
		{
			m_GPUSystems.Add(system);
		}
	}

	public void UpdateAt<SystemType>(SystemUpdatePhase phase) where SystemType : ComponentSystemBase
	{
		Register(++m_AddIndex, base.World.GetOrCreateSystemManaged<SystemType>(), phase);
	}

	public void UpdateBefore<SystemType>(SystemUpdatePhase phase) where SystemType : ComponentSystemBase
	{
		Register(++m_AddIndex - 1000000, base.World.GetOrCreateSystemManaged<SystemType>(), phase);
	}

	public void UpdateAfter<SystemType>(SystemUpdatePhase phase) where SystemType : ComponentSystemBase
	{
		Register(++m_AddIndex + 1000000, base.World.GetOrCreateSystemManaged<SystemType>(), phase);
	}

	public void UpdateBefore<SystemType, OtherType>(SystemUpdatePhase phase) where SystemType : ComponentSystemBase where OtherType : ComponentSystemBase
	{
		Register(++m_AddIndex - 1000000, base.World.GetOrCreateSystemManaged<SystemType>(), base.World.GetOrCreateSystemManaged<OtherType>(), phase);
	}

	public void UpdateAfter<SystemType, OtherType>(SystemUpdatePhase phase) where SystemType : ComponentSystemBase where OtherType : ComponentSystemBase
	{
		Register(++m_AddIndex + 1000000, base.World.GetOrCreateSystemManaged<SystemType>(), base.World.GetOrCreateSystemManaged<OtherType>(), phase);
	}

	public void Update(SystemUpdatePhase phase)
	{
		if (m_IsDirty)
		{
			Refresh();
		}
		if (m_UpdateRanges.Count <= (int)phase)
		{
			return;
		}
		SystemUpdatePhase systemUpdatePhase = currentPhase;
		try
		{
			currentPhase = phase;
			int2 @int = m_UpdateRanges[(int)phase];
			for (int i = @int.x; i < @int.y; i++)
			{
				SystemData systemData = m_Updates[i];
				try
				{
					systemData.m_System.Update();
				}
				catch (Exception exception)
				{
					COSystemBase.baseLog.CriticalFormat(exception, "System update error during {0}->{1}:", phase.ToString(), systemData.m_System.GetType().Name);
				}
			}
		}
		finally
		{
			currentPhase = systemUpdatePhase;
		}
	}

	public void Update(SystemUpdatePhase phase, uint updateIndex, int iterationIndex)
	{
		if (m_IsDirty)
		{
			Refresh();
		}
		if (m_UpdateRanges.Count <= (int)phase)
		{
			return;
		}
		SystemUpdatePhase systemUpdatePhase = currentPhase;
		try
		{
			currentPhase = phase;
			int2 @int = m_UpdateRanges[(int)phase];
			for (int i = @int.x; i < @int.y; i++)
			{
				SystemData systemData = m_Updates[i];
				if ((updateIndex & (uint)(systemData.m_Interval - 1)) != (uint)systemData.m_Offset)
				{
					continue;
				}
				try
				{
					if (systemData.m_ResetInterval <= iterationIndex)
					{
						((GameSystemBase)systemData.m_System).ResetDependency();
					}
					systemData.m_System.Update();
				}
				catch (Exception exception)
				{
					COSystemBase.baseLog.CriticalFormat(exception, "System update error during {0}->{1}:", phase.ToString(), systemData.m_System.GetType().Name);
				}
			}
		}
		finally
		{
			currentPhase = systemUpdatePhase;
		}
	}

	private void Register(int addIndex, ComponentSystemBase system, SystemUpdatePhase phase)
	{
		GetInterval(system, phase, out var interval, out var offset);
		m_Systems.Add(new SystemData(phase, interval, offset, addIndex, system));
		m_IsDirty = true;
	}

	private void Register(int addIndex, ComponentSystemBase system, ComponentSystemBase other, SystemUpdatePhase phase)
	{
		GetInterval(system, phase, out var interval, out var offset);
		if (m_RefMap.TryGetValue(other, out var value))
		{
			value.Add(new SystemData(phase, interval, offset, addIndex, system));
		}
		else
		{
			value = new List<SystemData>(10);
			value.Add(new SystemData(phase, interval, offset, addIndex, system));
			m_RefMap.Add(other, value);
		}
		m_IsDirty = true;
	}

	public static void GetInterval(ComponentSystemBase system, SystemUpdatePhase phase, out int interval, out int offset)
	{
		interval = 1;
		offset = -1;
		if (system is GameSystemBase gameSystemBase)
		{
			interval = gameSystemBase.GetUpdateInterval(phase);
			offset = gameSystemBase.GetUpdateOffset(phase);
		}
		if (!math.ispow2(interval))
		{
			throw new Exception("System update interval not power of 2");
		}
	}

	private void Refresh()
	{
		m_IsDirty = false;
		m_Updates.Clear();
		m_UpdateRanges.Clear();
		if (m_Systems.Count >= 2)
		{
			m_Systems.Sort();
		}
		List<IntervalData> list = new List<IntervalData>(1000);
		int num = 0;
		while (num < m_Systems.Count)
		{
			int count = m_Updates.Count;
			list.Clear();
			SystemData systemData = m_Systems[num];
			SystemUpdatePhase phase = systemData.m_Phase;
			AddSystemUpdate(list, systemData, inheritOffset: false, 0);
			int i;
			for (i = num + 1; i < m_Systems.Count; i++)
			{
				SystemData systemData2 = m_Systems[i];
				if (systemData2.m_Phase != systemData.m_Phase)
				{
					break;
				}
				AddSystemUpdate(list, systemData2, inheritOffset: false, 0);
			}
			if (list.Count != 0)
			{
				if (list.Count >= 2)
				{
					list.Sort();
				}
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int j = 0; j < list.Count; j++)
				{
					IntervalData intervalData = list[j];
					if (intervalData.m_Interval != num2)
					{
						num2 = intervalData.m_Interval;
						num4 = 0;
					}
					systemData = m_Updates[intervalData.m_UpdateIndex];
					systemData.m_Offset = num3;
					PatchSystemOffset(ref intervalData.m_UpdateStart, systemData, inheritOffset: true, 0);
					num4++;
					int num5 = 0;
					while ((num4 & (1 << num5++)) == 0)
					{
					}
					num3 += num2 >> num5;
					num3 &= num2 - 1;
					if (num4 << 1 >= num2)
					{
						num4 = 0;
					}
				}
			}
			if (m_Updates.Count > count)
			{
				while (m_UpdateRanges.Count < (int)phase)
				{
					m_UpdateRanges.Add(count);
				}
				m_UpdateRanges.Add(new int2(count, m_Updates.Count));
			}
			num = i;
		}
	}

	private void AddSystemUpdate(List<IntervalData> intervalList, SystemData systemData, bool inheritOffset, int safety)
	{
		if (++safety == 100)
		{
			throw new Exception("Too deep system order");
		}
		if (m_RefMap.TryGetValue(systemData.m_System, out var value))
		{
			if (value.Count >= 2)
			{
				value.Sort();
			}
			int count = m_Updates.Count;
			int num = 0;
			while (num < value.Count)
			{
				SystemData systemData2 = value[num++];
				if (systemData2.m_Phase == systemData.m_Phase)
				{
					if (systemData2.m_AddIndex >= 0)
					{
						num--;
						break;
					}
					bool flag = systemData2.m_Interval == systemData.m_Interval && systemData2.m_Offset < 0;
					if (flag)
					{
						systemData2.m_Offset = systemData.m_Offset;
					}
					AddSystemUpdate(intervalList, systemData2, flag, safety);
				}
			}
			if (systemData.m_Offset < 0)
			{
				if (systemData.m_Interval == 1)
				{
					systemData.m_Offset = 0;
				}
				else if (!inheritOffset)
				{
					intervalList.Add(new IntervalData(systemData.m_Interval, count, m_Updates.Count));
				}
			}
			m_Updates.Add(systemData);
			while (num < value.Count)
			{
				SystemData systemData3 = value[num++];
				if (systemData3.m_Phase == systemData.m_Phase)
				{
					bool flag2 = systemData3.m_Interval == systemData.m_Interval && systemData3.m_Offset < 0;
					if (flag2)
					{
						systemData3.m_Offset = systemData.m_Offset;
					}
					AddSystemUpdate(intervalList, systemData3, flag2, safety);
					continue;
				}
				break;
			}
			return;
		}
		if (systemData.m_Offset < 0)
		{
			if (systemData.m_Interval == 1)
			{
				systemData.m_Offset = 0;
			}
			else if (!inheritOffset)
			{
				intervalList.Add(new IntervalData(systemData.m_Interval, m_Updates.Count, m_Updates.Count));
			}
		}
		m_Updates.Add(systemData);
	}

	private void PatchSystemOffset(ref int updateIndex, SystemData systemData, bool inheritOffset, int safety)
	{
		if (++safety == 100)
		{
			throw new Exception("Too deep system order");
		}
		if (m_RefMap.TryGetValue(systemData.m_System, out var value))
		{
			int num = 0;
			while (num < value.Count)
			{
				SystemData systemData2 = value[num++];
				if (systemData2.m_Phase == systemData.m_Phase)
				{
					if (systemData2.m_AddIndex >= 0)
					{
						num--;
						break;
					}
					bool flag = systemData2.m_Interval == systemData.m_Interval && systemData2.m_Offset < 0;
					if (flag)
					{
						systemData2.m_Offset = systemData.m_Offset;
					}
					PatchSystemOffset(ref updateIndex, systemData2, flag, safety);
				}
			}
			if (inheritOffset)
			{
				SystemData value2 = m_Updates[updateIndex];
				value2.m_Offset = systemData.m_Offset;
				m_Updates[updateIndex] = value2;
			}
			updateIndex++;
			while (num < value.Count)
			{
				SystemData systemData3 = value[num++];
				if (systemData3.m_Phase == systemData.m_Phase)
				{
					bool flag2 = systemData3.m_Interval == systemData.m_Interval && systemData3.m_Offset < 0;
					if (flag2)
					{
						systemData3.m_Offset = systemData.m_Offset;
					}
					PatchSystemOffset(ref updateIndex, systemData3, flag2, safety);
					continue;
				}
				break;
			}
		}
		else
		{
			if (inheritOffset)
			{
				SystemData value3 = m_Updates[updateIndex];
				value3.m_Offset = systemData.m_Offset;
				m_Updates[updateIndex] = value3;
			}
			updateIndex++;
		}
	}

	[Preserve]
	public UpdateSystem()
	{
	}
}
