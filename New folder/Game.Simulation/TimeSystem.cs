using System;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Serialization;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TimeSystem : GameSystemBase, ITimeSystem, IPostDeserialize
{
	private SimulationSystem m_SimulationSystem;

	public const int kTicksPerDay = 262144;

	private float m_Time;

	private float m_Date;

	private int m_Year = 1;

	private int m_DaysPerYear = 1;

	private uint m_InitialFrame;

	private EntityQuery m_TimeSettingGroup;

	private EntityQuery m_TimeDataQuery;

	public int startingYear { get; set; }

	public float normalizedTime => m_Time;

	public float normalizedDate => m_Date;

	public int year => m_Year;

	public int daysPerYear
	{
		get
		{
			if (m_DaysPerYear == 0 && !m_TimeSettingGroup.IsEmptyIgnoreFilter)
			{
				m_DaysPerYear = m_TimeSettingGroup.GetSingleton<TimeSettingsData>().m_DaysPerYear;
				if (m_DaysPerYear == 0)
				{
					m_DaysPerYear = 1;
				}
			}
			return m_DaysPerYear;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TimeSettingGroup = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		RequireForUpdate(m_TimeSettingGroup);
		RequireForUpdate(m_TimeDataQuery);
	}

	public void PostDeserialize(Context context)
	{
		if (m_TimeDataQuery.IsEmpty)
		{
			Entity entity = base.EntityManager.CreateEntity();
			TimeData componentData = default(TimeData);
			componentData.SetDefaults(context);
			base.EntityManager.AddComponentData(entity, componentData);
		}
		if (context.purpose == Purpose.NewGame)
		{
			TimeData singleton = m_TimeDataQuery.GetSingleton<TimeData>();
			Entity singletonEntity = m_TimeDataQuery.GetSingletonEntity();
			singleton.m_FirstFrame = m_SimulationSystem.frameIndex;
			singleton.m_StartingYear = startingYear;
			base.EntityManager.SetComponentData(singletonEntity, singleton);
		}
		UpdateTime();
	}

	protected int GetTicks(uint frameIndex, TimeSettingsData settings, TimeData data)
	{
		return (int)(frameIndex - data.m_FirstFrame) + Mathf.RoundToInt(data.TimeOffset * 262144f) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * 262144f * (float)settings.m_DaysPerYear);
	}

	protected int GetTicks(TimeSettingsData settings, TimeData data)
	{
		return (int)(m_SimulationSystem.frameIndex - data.m_FirstFrame) + Mathf.RoundToInt(data.TimeOffset * 262144f) + Mathf.RoundToInt(data.GetDateOffset(settings.m_DaysPerYear) * 262144f * (float)settings.m_DaysPerYear);
	}

	protected double GetTimeWithOffset(TimeSettingsData settings, TimeData data, double renderingFrame)
	{
		return renderingFrame + (double)(data.TimeOffset * 262144f) + (double)(data.GetDateOffset(settings.m_DaysPerYear) * 262144f * (float)settings.m_DaysPerYear);
	}

	public float GetTimeOfDay(TimeSettingsData settings, TimeData data, double renderingFrame)
	{
		return (float)(GetTimeWithOffset(settings, data, renderingFrame) % 262144.0 / 262144.0);
	}

	protected float GetTimeOfDay(TimeSettingsData settings, TimeData data)
	{
		return (float)(GetTicks(settings, data) % 262144) / 262144f;
	}

	public float GetTimeOfYear(TimeSettingsData settings, TimeData data, double renderingFrame)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return (float)(GetTimeWithOffset(settings, data, renderingFrame % (double)num) / (double)num);
	}

	protected float GetTimeOfYear(TimeSettingsData settings, TimeData data)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return (float)(GetTicks(settings, data) % num) / (float)num;
	}

	public float GetElapsedYears(TimeSettingsData settings, TimeData data)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return (float)(m_SimulationSystem.frameIndex - data.m_FirstFrame) / (float)num;
	}

	public float GetStartingDate(TimeSettingsData settings, TimeData data)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return (float)(GetTicks(data.m_FirstFrame, settings, data) % num) / (float)num;
	}

	public int GetYear(TimeSettingsData settings, TimeData data, double renderingFrame)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return data.m_StartingYear + Mathf.FloorToInt((float)(GetTimeWithOffset(settings, data, renderingFrame) / (double)num));
	}

	public int GetYear(TimeSettingsData settings, TimeData data)
	{
		int num = 262144 * settings.m_DaysPerYear;
		return data.m_StartingYear + Mathf.FloorToInt(GetTicks(settings, data) / num);
	}

	public static int GetDay(uint frame, TimeData data)
	{
		return Mathf.FloorToInt((float)(frame - data.m_FirstFrame) / 262144f + data.TimeOffset);
	}

	public void DebugAdvanceTime(int minutes)
	{
		TimeData singleton = m_TimeDataQuery.GetSingleton<TimeData>();
		Entity singletonEntity = m_TimeDataQuery.GetSingletonEntity();
		singleton.m_FirstFrame -= (uint)(minutes * 262144) / 1440u;
		base.EntityManager.SetComponentData(singletonEntity, singleton);
	}

	private static DateTime CreateDateTime(int year, int day, int hour, int minute, float second)
	{
		DateTime result = new DateTime(0L, DateTimeKind.Utc).AddYears(year - 1).AddDays(day - 1).AddHours(hour)
			.AddMinutes(minute)
			.AddSeconds(second);
		if (result.IsDaylightSavingTime())
		{
			result = result.AddHours(1.0);
		}
		return result;
	}

	public DateTime GetDateTime(double renderingFrame)
	{
		TimeSettingsData singleton = m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
		TimeData singleton2 = m_TimeDataQuery.GetSingleton<TimeData>();
		float timeOfDay = GetTimeOfDay(singleton, singleton2, renderingFrame);
		float timeOfYear = GetTimeOfYear(singleton, singleton2, renderingFrame);
		int num = Mathf.FloorToInt(24f * timeOfDay);
		int minute = Mathf.FloorToInt(60f * (24f * timeOfDay - (float)num));
		int day = 1 + Mathf.FloorToInt((float)daysPerYear * timeOfYear) % daysPerYear;
		return CreateDateTime(year, day, num, minute, Mathf.Repeat(timeOfDay, 1f));
	}

	public DateTime GetCurrentDateTime()
	{
		float num = normalizedTime;
		float num2 = normalizedDate;
		int num3 = Mathf.FloorToInt(24f * num);
		int minute = Mathf.FloorToInt(60f * (24f * num - (float)num3));
		int day = 1 + Mathf.FloorToInt((float)daysPerYear * num2) % daysPerYear;
		return CreateDateTime(year, day, num3, minute, Mathf.Repeat(num, 1f));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateTime();
	}

	private void UpdateTime()
	{
		TimeSettingsData singleton = m_TimeSettingGroup.GetSingleton<TimeSettingsData>();
		TimeData singleton2 = m_TimeDataQuery.GetSingleton<TimeData>();
		m_Time = GetTimeOfDay(singleton, singleton2);
		m_Date = GetTimeOfYear(singleton, singleton2);
		m_Year = GetYear(singleton, singleton2);
		m_DaysPerYear = singleton.m_DaysPerYear;
	}

	[Preserve]
	public TimeSystem()
	{
	}
}
