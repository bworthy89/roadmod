using System;
using System.Collections.Generic;
using Colossal;
using Unity.Profiling;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public class ProfilerMetricsDebugUI : IDisposable
{
	private struct StatInfo
	{
		public string categoryName;

		public ProfilerCategory category;

		public string name;

		public ProfilerRecorder profilerRecorder;

		public StatInfo(string categoryName, ProfilerCategory category, string name, int sampleCount = 0)
		{
			this.categoryName = categoryName;
			this.category = category;
			this.name = name;
			profilerRecorder = ((sampleCount > 0) ? ProfilerRecorder.StartNew(category, name, sampleCount) : ProfilerRecorder.StartNew(category, name));
		}
	}

	private List<StatInfo> m_AvailableStats = new List<StatInfo>();

	public ProfilerMetricsDebugUI()
	{
		CollectProfilerMetrics();
	}

	public void Dispose()
	{
		DisposeProfilerMetrics();
	}

	private unsafe static double GetRecorderFrameAverage(ProfilerRecorder recorder)
	{
		int capacity = recorder.Capacity;
		if (capacity == 0)
		{
			return 0.0;
		}
		double num = 0.0;
		ProfilerRecorderSample* ptr = stackalloc ProfilerRecorderSample[capacity];
		recorder.CopyTo(ptr, capacity);
		for (int i = 0; i < capacity; i++)
		{
			num += (double)ptr[i].Value;
		}
		return num / (double)capacity;
	}

	private void CollectProfilerMetrics()
	{
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Total Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Total Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "GC Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "GC Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Gfx Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Gfx Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Audio Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Audio Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Video Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Video Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Profiler Used Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "Profiler Reserved Memory"));
		m_AvailableStats.Add(new StatInfo("Memory", ProfilerCategory.Memory, "System Used Memory"));
	}

	private void DisposeProfilerMetrics()
	{
		foreach (StatInfo availableStat in m_AvailableStats)
		{
			availableStat.profilerRecorder.Dispose();
		}
		m_AvailableStats.Clear();
	}

	[DebugTab("Profiler Metrics", 0)]
	private List<DebugUI.Widget> BuildProfilerMetricsDebugUI()
	{
		List<DebugUI.Widget> list = new List<DebugUI.Widget>();
		Dictionary<string, DebugUI.Foldout> dictionary = new Dictionary<string, DebugUI.Foldout>();
		foreach (StatInfo stat in m_AvailableStats)
		{
			if (!dictionary.TryGetValue(stat.categoryName, out var value))
			{
				value = new DebugUI.Foldout
				{
					displayName = stat.categoryName
				};
				list.Add(value);
				dictionary.Add(stat.categoryName, value);
			}
			ProfilerRecorder profilerRecorder = stat.profilerRecorder;
			switch (profilerRecorder.UnitType)
			{
			case ProfilerMarkerDataUnit.Bytes:
				value.children.Add(new DebugUI.Value
				{
					displayName = stat.name,
					getter = delegate
					{
						ProfilerRecorder profilerRecorder2 = stat.profilerRecorder;
						return FormatUtils.FormatBytes(profilerRecorder2.LastValue);
					}
				});
				break;
			case ProfilerMarkerDataUnit.TimeNanoseconds:
				value.children.Add(new DebugUI.Value
				{
					displayName = stat.name,
					getter = () => $"{GetRecorderFrameAverage(stat.profilerRecorder) * 9.999999974752427E-07:F2}ms"
				});
				break;
			case ProfilerMarkerDataUnit.Count:
				value.children.Add(new DebugUI.Value
				{
					displayName = stat.name,
					getter = delegate
					{
						ProfilerRecorder profilerRecorder2 = stat.profilerRecorder;
						return profilerRecorder2.Count;
					}
				});
				break;
			case ProfilerMarkerDataUnit.FrequencyHz:
				value.children.Add(new DebugUI.Value
				{
					displayName = stat.name,
					getter = delegate
					{
						ProfilerRecorder profilerRecorder2 = stat.profilerRecorder;
						return $"{profilerRecorder2.LastValue}Hz";
					}
				});
				break;
			case ProfilerMarkerDataUnit.Percent:
				value.children.Add(new DebugUI.Value
				{
					displayName = stat.name,
					getter = delegate
					{
						ProfilerRecorder profilerRecorder2 = stat.profilerRecorder;
						return $"{profilerRecorder2.LastValue}%";
					}
				});
				break;
			}
		}
		return list;
	}
}
