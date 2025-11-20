using System;
using System.Collections.Generic;
using Colossal;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class ZoneAmbienceValueDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct ZoneAmbienceValueGizmoJob : IJob
	{
		public GroupAmbienceType m_Type;

		[ReadOnly]
		public NativeArray<ZoneAmbienceCell> m_AmbienceMap;

		[ReadOnly]
		public NativeArray<Color> m_DistinctColors;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			for (int i = 0; i < m_AmbienceMap.Length; i++)
			{
				ZoneAmbienceCell zoneAmbienceCell = m_AmbienceMap[i];
				float3 cellCenter = ZoneAmbienceSystem.GetCellCenter(i);
				cellCenter.y += 385.7151f;
				float ambience = zoneAmbienceCell.m_Value.GetAmbience(m_Type);
				float num = (int)m_Type * 5;
				float3 @float = new float3(num, 0f, num);
				m_GizmoBatcher.DrawLine(cellCenter + @float, cellCenter + new float3(0f, ambience, 0f) + @float, m_DistinctColors[(int)m_Type]);
			}
		}
	}

	private ZoneAmbienceSystem m_ZoneAmbienceSystem;

	private GizmosSystem m_GizmosSystem;

	private Dictionary<GroupAmbienceType, Option> m_CoverageOptions;

	private NativeArray<Color> m_DistinctColors;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_CoverageOptions = new Dictionary<GroupAmbienceType, Option>();
		string[] names = Enum.GetNames(typeof(GroupAmbienceType));
		Array values = Enum.GetValues(typeof(GroupAmbienceType));
		for (int i = 0; i < names.Length; i++)
		{
			GroupAmbienceType groupAmbienceType = (GroupAmbienceType)values.GetValue(i);
			if (groupAmbienceType != GroupAmbienceType.Count)
			{
				m_CoverageOptions.Add(groupAmbienceType, AddOption(names[i], i == 0));
			}
		}
		m_DistinctColors = new NativeArray<Color>(24, Allocator.Persistent);
		for (int j = 0; j < 24; j++)
		{
			float h = (float)j / 24f % 1f;
			m_DistinctColors[j] = Color.HSVToRGB(h, 1f, 1f);
		}
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_DistinctColors.Dispose();
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle jobHandle = inputDeps;
		foreach (KeyValuePair<GroupAmbienceType, Option> coverageOption in m_CoverageOptions)
		{
			if (coverageOption.Value.enabled)
			{
				JobHandle dependencies;
				JobHandle dependencies2;
				JobHandle jobHandle2 = new ZoneAmbienceValueGizmoJob
				{
					m_Type = coverageOption.Key,
					m_AmbienceMap = m_ZoneAmbienceSystem.GetMap(readOnly: true, out dependencies),
					m_DistinctColors = m_DistinctColors,
					m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2)
				}.Schedule(JobHandle.CombineDependencies(dependencies, dependencies2, base.Dependency));
				m_GizmosSystem.AddGizmosBatcherWriter(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
		}
		return jobHandle;
	}

	[Preserve]
	public ZoneAmbienceValueDebugSystem()
	{
	}
}
