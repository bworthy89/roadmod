using System;
using System.Runtime.CompilerServices;
using Colossal.PSI.Common;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TimeUISystem : UISystemBase
{
	private struct TimeSettings : IJsonWritable, IEquatable<TimeSettings>
	{
		public int ticksPerDay;

		public int daysPerYear;

		public int epochTicks;

		public int epochYear;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("ticksPerDay");
			writer.Write(ticksPerDay);
			writer.PropertyName("daysPerYear");
			writer.Write(daysPerYear);
			writer.PropertyName("epochTicks");
			writer.Write(epochTicks);
			writer.PropertyName("epochYear");
			writer.Write(epochYear);
			writer.TypeEnd();
		}

		public bool Equals(TimeSettings other)
		{
			if (ticksPerDay == other.ticksPerDay && daysPerYear == other.daysPerYear && epochTicks == other.epochTicks)
			{
				return epochYear == other.epochYear;
			}
			return false;
		}
	}

	private const string kGroup = "time";

	private SimulationSystem m_SimulationSystem;

	private TimeSystem m_TimeSystem;

	private LightingSystem m_LightingSystem;

	private EntityQuery m_TimeSettingsQuery;

	private EntityQuery m_TimeDataQuery;

	private EventBinding<bool> m_SimulationPausedBarrierBinding;

	private float m_SpeedBeforePause = 1f;

	private bool m_UnpausedBeforeForcedPause;

	private bool m_HasFocus = true;

	private bool pausedBarrierActive => m_SimulationPausedBarrierBinding.observerCount > 0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		AddUpdateBinding(new GetterValueBinding<TimeSettings>("time", "timeSettings", GetTimeSettings, new ValueWriter<TimeSettings>()));
		AddUpdateBinding(new GetterValueBinding<int>("time", "ticks", GetTicks));
		AddUpdateBinding(new GetterValueBinding<int>("time", "day", GetDay));
		AddUpdateBinding(new GetterValueBinding<LightingSystem.State>("time", "lightingState", GetLightingState, new DelegateWriter<LightingSystem.State>(delegate(IJsonWriter writer, LightingSystem.State value)
		{
			writer.Write((int)value);
		})));
		AddUpdateBinding(new GetterValueBinding<bool>("time", "simulationPaused", IsPaused));
		AddUpdateBinding(new GetterValueBinding<int>("time", "simulationSpeed", GetSimulationSpeed));
		AddBinding(m_SimulationPausedBarrierBinding = new EventBinding<bool>("time", "simulationPausedBarrier"));
		AddBinding(new TriggerBinding<bool>("time", "setSimulationPaused", SetSimulationPaused));
		AddBinding(new TriggerBinding<int>("time", "setSimulationSpeed", SetSimulationSpeed));
		PlatformManager.instance.onAppStateChanged += HandleAppStateChanged;
	}

	private void HandleAppStateChanged(IPlatformServiceIntegration psi, AppState state)
	{
		switch (state)
		{
		case AppState.Default:
			m_HasFocus = true;
			break;
		case AppState.Constrained:
			m_HasFocus = false;
			break;
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_SpeedBeforePause = 1f;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (m_SimulationSystem.selectedSpeed > 0f)
		{
			m_SpeedBeforePause = m_SimulationSystem.selectedSpeed;
		}
		if (!m_HasFocus || m_SimulationPausedBarrierBinding.observerCount > 0)
		{
			if (!IsPaused())
			{
				m_UnpausedBeforeForcedPause = true;
			}
			m_SimulationSystem.selectedSpeed = 0f;
		}
		else
		{
			if (m_UnpausedBeforeForcedPause)
			{
				m_SimulationSystem.selectedSpeed = m_SpeedBeforePause;
			}
			m_UnpausedBeforeForcedPause = false;
		}
	}

	private TimeSettings GetTimeSettings()
	{
		TimeSettingsData timeSettingsData = GetTimeSettingsData();
		TimeData singleton = TimeData.GetSingleton(m_TimeDataQuery);
		return new TimeSettings
		{
			ticksPerDay = 262144,
			daysPerYear = timeSettingsData.m_DaysPerYear,
			epochTicks = Mathf.RoundToInt(singleton.TimeOffset * 262144f) + Mathf.RoundToInt(singleton.GetDateOffset(timeSettingsData.m_DaysPerYear) * 262144f * (float)timeSettingsData.m_DaysPerYear),
			epochYear = singleton.m_StartingYear
		};
	}

	public int GetTicks()
	{
		float num = 182.04445f;
		return Mathf.FloorToInt(Mathf.Floor((float)(m_SimulationSystem.frameIndex - TimeData.GetSingleton(m_TimeDataQuery).m_FirstFrame) / num) * num);
	}

	public int GetDay()
	{
		return TimeSystem.GetDay(m_SimulationSystem.frameIndex, TimeData.GetSingleton(m_TimeDataQuery));
	}

	public LightingSystem.State GetLightingState()
	{
		LightingSystem.State state = m_LightingSystem.state;
		if (state != LightingSystem.State.Invalid)
		{
			return state;
		}
		float normalizedTime = m_TimeSystem.normalizedTime;
		if (!(normalizedTime < 7f / 24f) && !(normalizedTime > 0.875f))
		{
			return LightingSystem.State.Day;
		}
		return LightingSystem.State.Night;
	}

	public bool IsPaused()
	{
		return m_SimulationSystem.selectedSpeed == 0f;
	}

	public int GetSimulationSpeed()
	{
		return SpeedToIndex(IsPaused() ? m_SpeedBeforePause : m_SimulationSystem.selectedSpeed);
	}

	private TimeSettingsData GetTimeSettingsData()
	{
		if (m_TimeSettingsQuery.IsEmptyIgnoreFilter)
		{
			return new TimeSettingsData
			{
				m_DaysPerYear = 12
			};
		}
		return m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
	}

	private void SetSimulationPaused(bool paused)
	{
		if (!pausedBarrierActive)
		{
			m_SimulationSystem.selectedSpeed = (paused ? 0f : m_SpeedBeforePause);
		}
		else
		{
			m_UnpausedBeforeForcedPause = !paused;
		}
	}

	private void SetSimulationSpeed(int speedIndex)
	{
		if (!pausedBarrierActive)
		{
			m_SimulationSystem.selectedSpeed = IndexToSpeed(speedIndex);
			return;
		}
		m_SpeedBeforePause = IndexToSpeed(speedIndex);
		m_UnpausedBeforeForcedPause = true;
	}

	private static float IndexToSpeed(int index)
	{
		return Mathf.Pow(2f, Mathf.Clamp(index, 0, 2));
	}

	private static int SpeedToIndex(float speed)
	{
		if (!(speed > 0f))
		{
			return 0;
		}
		return Mathf.Clamp((int)Mathf.Log(speed, 2f), 0, 2);
	}

	[Preserve]
	public TimeUISystem()
	{
	}
}
