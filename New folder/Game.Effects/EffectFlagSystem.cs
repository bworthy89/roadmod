using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Effects;

public class EffectFlagSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct EffectFlagData
	{
		public bool m_IsNightTime;

		public uint m_LastTimeChange;

		public bool m_IsColdSeason;

		public uint m_LastSeasonChange;
	}

	public static readonly uint kNightRandomTicks = 15000u;

	public static readonly uint kDayRandomTicks = 10000u;

	public static readonly float kNightBegin = 0.75f;

	public static readonly float kDayBegin = 0.25f;

	public static readonly uint kSpringRandomTicks = 20000u;

	public static readonly uint kAutumnRandomTicks = 20000u;

	public static readonly float kSpringTemperature = 10f;

	public static readonly float kAutumnTemperature = 5f;

	private Entity m_CurrentSeason;

	private uint m_LastSeasonChange;

	private bool m_IsColdSeason;

	private bool m_IsNightTime;

	private uint m_LastTimeChange;

	private SimulationSystem m_SimulationSystem;

	private TimeSystem m_TimeSystem;

	private ClimateSystem m_ClimateSystem;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 2048;
	}

	public static bool IsEnabled(EffectConditionFlags flag, Random random, EffectFlagData data, uint frame)
	{
		if ((flag & EffectConditionFlags.Night) != EffectConditionFlags.None)
		{
			if (data.m_IsNightTime)
			{
				return data.m_LastTimeChange + random.NextUInt(kNightRandomTicks) < frame;
			}
			return data.m_LastTimeChange + random.NextUInt(kDayRandomTicks) >= frame;
		}
		if ((flag & EffectConditionFlags.Cold) != EffectConditionFlags.None)
		{
			if (data.m_IsColdSeason)
			{
				return data.m_LastSeasonChange + random.NextUInt(kAutumnRandomTicks) < frame;
			}
			return data.m_LastSeasonChange + random.NextUInt(kSpringRandomTicks) >= frame;
		}
		return true;
	}

	public EffectFlagData GetData()
	{
		return new EffectFlagData
		{
			m_IsColdSeason = m_IsColdSeason,
			m_IsNightTime = m_IsNightTime,
			m_LastSeasonChange = m_LastSeasonChange,
			m_LastTimeChange = m_LastTimeChange
		};
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint lastSeasonChange = ref m_LastSeasonChange;
		reader.Read(out lastSeasonChange);
		ref bool isColdSeason = ref m_IsColdSeason;
		reader.Read(out isColdSeason);
		ref uint lastTimeChange = ref m_LastTimeChange;
		reader.Read(out lastTimeChange);
		m_IsNightTime = false;
		if (reader.context.version >= Version.addNightForestAmbienceSound)
		{
			ref bool isNightTime = ref m_IsNightTime;
			reader.Read(out isNightTime);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		uint lastSeasonChange = m_LastSeasonChange;
		writer.Write(lastSeasonChange);
		bool isColdSeason = m_IsColdSeason;
		writer.Write(isColdSeason);
		uint lastTimeChange = m_LastTimeChange;
		writer.Write(lastTimeChange);
		bool isNightTime = m_IsNightTime;
		writer.Write(isNightTime);
	}

	public void SetDefaults(Context context)
	{
		m_LastSeasonChange = 0u;
		m_IsColdSeason = false;
		m_IsNightTime = false;
		m_LastTimeChange = 0u;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint frameIndex = m_SimulationSystem.frameIndex;
		if (m_CurrentSeason != m_ClimateSystem.currentSeason)
		{
			float num = m_ClimateSystem.temperature;
			if (m_IsColdSeason && num >= kSpringTemperature)
			{
				m_IsColdSeason = false;
				m_LastSeasonChange = frameIndex;
			}
			else if (!m_IsColdSeason && num < kAutumnTemperature)
			{
				m_IsColdSeason = true;
				m_LastSeasonChange = frameIndex;
			}
		}
		m_IsNightTime = m_TimeSystem.normalizedTime >= kNightBegin || m_TimeSystem.normalizedTime < kDayBegin;
		m_CurrentSeason = m_ClimateSystem.currentSeason;
	}

	[Preserve]
	public EffectFlagSystem()
	{
	}
}
