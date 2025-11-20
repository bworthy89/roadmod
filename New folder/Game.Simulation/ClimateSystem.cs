#define UNITY_ASSERTIONS
using System;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Effects;
using Game.Prefabs;
using Game.Prefabs.Climate;
using Game.Rendering;
using Game.Serialization;
using Game.Triggers;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

public class ClimateSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPreSerialize, IPostDeserialize
{
	[Serializable]
	public class SeasonInfo : IJsonWritable, IJsonReadable
	{
		public SeasonPrefab m_Prefab;

		public string m_NameID;

		public string m_IconPath;

		public float m_StartTime;

		public float2 m_TempNightDay = new float2(5f, 20f);

		public float2 m_TempDeviationNightDay = new float2(4f, 7f);

		public float m_CloudChance = 50f;

		public float m_CloudAmount = 40f;

		public float m_CloudAmountDeviation = 20f;

		public float m_PrecipitationChance = 30f;

		public float m_PrecipitationAmount = 40f;

		public float m_PrecipitationAmountDeviation = 30f;

		public float m_Turbulence = 0.2f;

		public float m_AuroraAmount = 1f;

		public float m_AuroraChance = 10f;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("Season");
			writer.PropertyName("name");
			writer.Write(m_NameID);
			writer.PropertyName("startTime");
			writer.Write(m_StartTime);
			writer.PropertyName("tempNightDay");
			writer.Write(m_TempNightDay);
			writer.PropertyName("tempDeviationNightDay");
			writer.Write(m_TempDeviationNightDay);
			writer.PropertyName("cloudChance");
			writer.Write(m_CloudChance);
			writer.PropertyName("cloudAmount");
			writer.Write(m_CloudAmount);
			writer.PropertyName("cloudAmountDeviation");
			writer.Write(m_CloudAmountDeviation);
			writer.PropertyName("precipitationChance");
			writer.Write(m_PrecipitationChance);
			writer.PropertyName("precipitationAmount");
			writer.Write(m_PrecipitationAmount);
			writer.PropertyName("precipitationAmountDeviation");
			writer.Write(m_PrecipitationAmountDeviation);
			writer.PropertyName("turbulence");
			writer.Write(m_Turbulence);
			writer.PropertyName("auroraAmount");
			writer.Write(m_AuroraAmount);
			writer.PropertyName("auroraChance");
			writer.Write(m_AuroraChance);
			writer.TypeEnd();
		}

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("name");
			reader.Read(out m_NameID);
			reader.ReadProperty("startTime");
			reader.Read(out m_StartTime);
			reader.ReadProperty("tempNightDay");
			reader.Read(out m_TempNightDay);
			reader.ReadProperty("tempDeviationNightDay");
			reader.Read(out m_TempDeviationNightDay);
			reader.ReadProperty("cloudChance");
			reader.Read(out m_CloudChance);
			reader.ReadProperty("cloudAmount");
			reader.Read(out m_CloudAmount);
			reader.ReadProperty("cloudAmountDeviation");
			reader.Read(out m_CloudAmountDeviation);
			reader.ReadProperty("precipitationChance");
			reader.Read(out m_PrecipitationChance);
			reader.ReadProperty("precipitationAmount");
			reader.Read(out m_PrecipitationAmount);
			reader.ReadProperty("precipitationAmountDeviation");
			reader.Read(out m_PrecipitationAmountDeviation);
			reader.ReadProperty("turbulence");
			reader.Read(out m_Turbulence);
			reader.ReadProperty("auroraAmount");
			reader.Read(out m_AuroraAmount);
			reader.ReadProperty("auroraChance");
			reader.Read(out m_AuroraChance);
			reader.ReadMapEnd();
		}
	}

	public enum WeatherClassification
	{
		Irrelevant,
		Clear,
		Few,
		Scattered,
		Broken,
		Overcast,
		Stormy
	}

	public struct ClimateSample
	{
		public float temperature;

		public float precipitation;

		public float cloudiness;

		public float aurora;

		public float fog;
	}

	private struct WeatherTempData : IComparable<WeatherTempData>
	{
		public Entity m_Entity;

		public float m_Priority;

		public int CompareTo(WeatherTempData other)
		{
			return m_Priority.CompareTo(other.m_Priority);
		}
	}

	public OverridableProperty<float> thunder = new OverridableProperty<float>();

	private TriggerSystem m_TriggerSystem;

	private PrefabSystem m_PrefabSystem;

	private TimeSystem m_TimeSystem;

	private ClimateRenderSystem m_ClimateRenderSystem;

	private PlanetarySystem m_PlanetarySystem;

	private EntityQuery m_ClimateQuery;

	private OverridableProperty<float> m_Date;

	private Entity m_CurrentClimate;

	private NativeList<Entity> m_CurrentWeatherEffects;

	private NativeList<Entity> m_NextWeatherEffects;

	private float m_TemperatureBaseHeight;

	private SeasonInfo m_CurrentSeason;

	private static readonly int[,] kLut = new int[12, 5]
	{
		{ 33, 15, 32, 10, 10 },
		{ 31, 18, 31, 10, 10 },
		{ 31, 21, 28, 10, 10 },
		{ 23, 18, 30, 10, 19 },
		{ 22, 20, 23, 10, 25 },
		{ 21, 19, 24, 10, 26 },
		{ 19, 18, 26, 10, 27 },
		{ 18, 22, 23, 10, 27 },
		{ 25, 23, 24, 10, 18 },
		{ 29, 19, 32, 10, 10 },
		{ 30, 16, 34, 10, 10 },
		{ 34, 15, 31, 10, 10 }
	};

	private static readonly int[] kSampleTimes = new int[3] { 7, 13, 19 };

	public float2 wind { get; private set; } = new float2(0.0275f, 0.0275f);

	public float hail { get; set; }

	public float rainbow { get; set; }

	public float aerosolDensity { get; private set; }

	public float seasonTemperature { get; private set; }

	public float seasonPrecipitation { get; private set; }

	public float seasonCloudiness { get; private set; }

	public OverridableProperty<float> currentDate => m_Date;

	public OverridableProperty<float> precipitation { get; } = new OverridableProperty<float>();

	public OverridableProperty<float> temperature { get; } = new OverridableProperty<float>();

	public OverridableProperty<float> cloudiness { get; } = new OverridableProperty<float>();

	public OverridableProperty<float> aurora { get; } = new OverridableProperty<float>();

	public OverridableProperty<float> fog { get; } = new OverridableProperty<float>();

	public Entity currentClimate
	{
		get
		{
			return m_CurrentClimate;
		}
		set
		{
			Assert.AreNotEqual(Entity.Null, value);
			m_CurrentClimate = value;
			ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_CurrentClimate);
			prefab.EnsureSeasonsOrder(force: true);
			averageTemperature = CalculateTemperatureAverage(prefab);
			UpdateSeason(prefab, m_Date);
			if (m_CurrentWeatherEffects.Length == 0 && m_NextWeatherEffects.Length == 0)
			{
				UpdateWeather(prefab);
			}
		}
	}

	public float temperatureBaseHeight => m_TemperatureBaseHeight;

	public float snowTemperatureHeightScale => 0.01f;

	public float averageTemperature { get; private set; }

	public float freezingTemperature { get; private set; }

	public bool isRaining
	{
		get
		{
			if ((float)precipitation > 0f)
			{
				return (float)temperature > freezingTemperature;
			}
			return false;
		}
	}

	public bool isSnowing
	{
		get
		{
			if ((float)precipitation > 0f)
			{
				return (float)temperature <= freezingTemperature;
			}
			return false;
		}
	}

	public bool isPrecipitating => (float)precipitation > 0f;

	public WeatherClassification classification { get; private set; }

	public Entity currentSeason
	{
		get
		{
			if (m_CurrentSeason == null)
			{
				return Entity.Null;
			}
			return m_PrefabSystem.GetEntity(m_CurrentSeason.m_Prefab);
		}
	}

	public string currentSeasonNameID => m_CurrentSeason?.m_NameID;

	public void PatchReferences(ref PrefabReferences references)
	{
		m_CurrentClimate = references.Check(base.EntityManager, m_CurrentClimate);
		for (int i = 0; i < m_CurrentWeatherEffects.Length; i++)
		{
			m_CurrentWeatherEffects[i] = references.Check(base.EntityManager, m_CurrentWeatherEffects[i]);
		}
		for (int j = 0; j < m_NextWeatherEffects.Length; j++)
		{
			m_NextWeatherEffects[j] = references.Check(base.EntityManager, m_NextWeatherEffects[j]);
		}
	}

	private float CalculateMeanTemperatureStandard(ClimatePrefab prefab, int resolutionPerDay, out float meanMin, out float meanMax)
	{
		if (prefab.m_Seasons != null)
		{
			int length = prefab.m_Temperature.length;
			int num = m_TimeSystem.daysPerYear * resolutionPerDay;
			float2 zero = float2.zero;
			for (int i = 0; i < m_TimeSystem.daysPerYear; i++)
			{
				float2 zero2 = float2.zero;
				for (int j = 0; j < resolutionPerDay; j++)
				{
					float time = (float)(i + j) / (float)num * (float)length;
					float y = prefab.m_Temperature.Evaluate(time);
					zero2.x = math.min(zero2.x, y);
					zero2.y = math.max(zero2.y, y);
				}
				zero += zero2;
			}
			float2 @float = zero / m_TimeSystem.daysPerYear;
			meanMin = @float.x;
			meanMax = @float.y;
			return (@float.x + @float.y) * 0.5f;
		}
		meanMin = 0f;
		meanMax = 0f;
		return 0f;
	}

	private float CalculateMeanTemperatureEkholmModen(ClimatePrefab prefab, int resolutionPerDay)
	{
		Assert.AreEqual(12, m_TimeSystem.daysPerYear);
		if (prefab.m_Seasons != null)
		{
			int daysPerYear = m_TimeSystem.daysPerYear;
			int num = daysPerYear + (kSampleTimes.Length + 2);
			float num2 = 0f;
			for (int i = 0; i < daysPerYear; i++)
			{
				float num3 = 0f;
				for (int j = 0; j < kSampleTimes.Length; j++)
				{
					float time = (float)(kSampleTimes[j] + i) / (float)num * (float)daysPerYear;
					float num4 = prefab.m_Temperature.Evaluate(time);
					num3 += num4 * (float)kLut[i, j];
				}
				float2 zero = float2.zero;
				for (int k = 0; k < resolutionPerDay; k++)
				{
					float time2 = (float)(i + k - 5) / (float)num * (float)daysPerYear;
					float y = prefab.m_Temperature.Evaluate(time2);
					zero.x = math.min(zero.x, y);
					zero.y = math.max(zero.y, y);
				}
				num3 += zero.x * (float)kLut[i, 3];
				num3 += zero.y * (float)kLut[i, 3];
				num2 += num3 / 100f;
			}
			return num2 / (float)m_TimeSystem.daysPerYear;
		}
		return 0f;
	}

	private float CalculateMeanPrecipitation(ClimatePrefab prefab, int resolutionPerDay = 48, float startRange = 0f, float endRange = 1f)
	{
		int daysPerYear = m_TimeSystem.daysPerYear;
		float num = startRange * (float)daysPerYear;
		float num2 = endRange * (float)daysPerYear;
		int num3 = (int)math.round((num2 - num) * (float)resolutionPerDay);
		float num4 = 0f;
		for (int i = 0; i < num3; i++)
		{
			float t = (float)i / (float)num3;
			num4 += prefab.m_Precipitation.Evaluate(math.lerp(num, num2, t));
		}
		return num4 / (float)num3;
	}

	private float CalculateMeanTemperature(ClimatePrefab prefab, int resolutionPerDay = 48, float startRange = 0f, float endRange = 1f)
	{
		int daysPerYear = m_TimeSystem.daysPerYear;
		float num = startRange * (float)daysPerYear;
		float num2 = endRange * (float)daysPerYear;
		int num3 = (int)math.round((num2 - num) * (float)resolutionPerDay);
		float2 zero = float2.zero;
		for (int i = 0; i < num3; i++)
		{
			float t = (float)i / (float)num3;
			float y = prefab.m_Temperature.Evaluate(math.lerp(num, num2, t));
			zero.x = math.min(zero.x, y);
			zero.y = math.max(zero.y, y);
		}
		return (zero.x + zero.y) * 0.5f;
	}

	private float CalculateMeanCloudiness(ClimatePrefab prefab, int resolutionPerDay = 48, float startRange = 0f, float endRange = 1f)
	{
		int daysPerYear = m_TimeSystem.daysPerYear;
		float num = startRange * (float)daysPerYear;
		float num2 = endRange * (float)daysPerYear;
		int num3 = (int)math.round((num2 - num) * (float)resolutionPerDay);
		float num4 = 0f;
		for (int i = 0; i < num3; i++)
		{
			float t = (float)i / (float)num3;
			num4 += prefab.m_Cloudiness.Evaluate(math.lerp(num, num2, t));
		}
		return num4 / (float)num3;
	}

	private float CalculateTemperatureAverage(ClimatePrefab prefab, int resolutionPerDay = 48)
	{
		freezingTemperature = prefab.m_FreezingTemperature;
		if (m_TimeSystem.daysPerYear == 12)
		{
			return CalculateMeanTemperatureEkholmModen(prefab, resolutionPerDay);
		}
		float meanMin;
		float meanMax;
		return CalculateMeanTemperatureStandard(prefab, resolutionPerDay, out meanMin, out meanMax);
	}

	private float CalculateTemperatureBaseHeight()
	{
		TerrainSystem orCreateSystemManaged = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		WaterSystem orCreateSystemManaged2 = base.World.GetOrCreateSystemManaged<WaterSystem>();
		MapTileSystem orCreateSystemManaged3 = base.World.GetOrCreateSystemManaged<MapTileSystem>();
		TerrainHeightData terrainData = orCreateSystemManaged.GetHeightData();
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> data = orCreateSystemManaged2.GetSurfaceData(out deps);
		deps.Complete();
		NativeList<Entity> startTiles = orCreateSystemManaged3.GetStartTiles();
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < startTiles.Length; i++)
		{
			if (base.EntityManager.TryGetBuffer(startTiles[i], isReadOnly: true, out DynamicBuffer<Node> buffer))
			{
				for (int j = 0; j < buffer.Length; j++)
				{
					num += WaterUtils.SampleHeight(ref data, ref terrainData, buffer[j].m_Position);
					num2 += 1f;
				}
			}
		}
		if (!(num2 > 0f))
		{
			return 0f;
		}
		return num / num2;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_ClimateRenderSystem = base.World.GetOrCreateSystemManaged<ClimateRenderSystem>();
		m_PlanetarySystem = base.World.GetOrCreateSystemManaged<PlanetarySystem>();
		m_CurrentWeatherEffects = new NativeList<Entity>(0, Allocator.Persistent);
		m_NextWeatherEffects = new NativeList<Entity>(0, Allocator.Persistent);
		m_ClimateQuery = GetEntityQuery(ComponentType.ReadOnly<ClimateData>());
		m_Date = new OverridableProperty<float>(() => m_TimeSystem.normalizedDate);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_CurrentWeatherEffects.Dispose();
		m_NextWeatherEffects.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (currentClimate != Entity.Null)
		{
			ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(currentClimate);
			ClimateSample climateSample = SampleClimate(prefab, m_Date);
			temperature.value = climateSample.temperature;
			precipitation.value = climateSample.precipitation;
			cloudiness.value = climateSample.cloudiness;
			aurora.value = climateSample.aurora;
			fog.value = climateSample.fog;
			UpdateSeason(prefab, m_Date);
			UpdateWeather(prefab);
		}
		if (m_TriggerSystem.Enabled)
		{
			HandleTriggers();
		}
	}

	private void HandleTriggers()
	{
		NativeQueue<TriggerAction> nativeQueue = m_TriggerSystem.CreateActionBuffer();
		nativeQueue.Enqueue(new TriggerAction(TriggerType.Temperature, Entity.Null, temperature));
		bool num = hail > 0.001f;
		bool flag = (float)cloudiness > 0.5f;
		bool flag2 = m_TimeSystem.normalizedTime >= EffectFlagSystem.kDayBegin && m_TimeSystem.normalizedTime < EffectFlagSystem.kNightBegin;
		bool flag3 = classification == WeatherClassification.Stormy;
		bool flag4 = (float)aurora > 0.5f;
		if (num || flag3)
		{
			nativeQueue.Enqueue(new TriggerAction(TriggerType.WeatherStormy, Entity.Null, 0f));
			return;
		}
		if (isRaining)
		{
			nativeQueue.Enqueue(new TriggerAction(TriggerType.WeatherRainy, Entity.Null, 0f));
		}
		if (isSnowing)
		{
			nativeQueue.Enqueue(new TriggerAction(TriggerType.WeatherSnowy, Entity.Null, 0f));
		}
		if (!isRaining && !isSnowing && !flag && flag2)
		{
			nativeQueue.Enqueue(new TriggerAction(((float)temperature > 15f) ? TriggerType.WeatherSunny : TriggerType.WeatherClear, Entity.Null, 0f));
		}
		if (flag2 && flag && !isRaining && !isSnowing)
		{
			nativeQueue.Enqueue(new TriggerAction(TriggerType.WeatherCloudy, Entity.Null, 0f));
		}
		if (!flag2 && !flag && !isRaining && !isSnowing && flag4)
		{
			nativeQueue.Enqueue(new TriggerAction(TriggerType.AuroraBorealis, Entity.Null, aurora));
		}
	}

	public void PreSerialize(Context context)
	{
		if (context.purpose == Purpose.SaveMap)
		{
			m_TemperatureBaseHeight = CalculateTemperatureBaseHeight();
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_CurrentClimate;
		writer.Write(value);
		NativeList<Entity> currentWeatherEffects = m_CurrentWeatherEffects;
		writer.Write(currentWeatherEffects);
		NativeList<Entity> nextWeatherEffects = m_NextWeatherEffects;
		writer.Write(nextWeatherEffects);
		float value2 = m_TemperatureBaseHeight;
		writer.Write(value2);
	}

	public void SetDefaults(Context context)
	{
		m_CurrentClimate = Entity.Null;
		m_CurrentWeatherEffects.ResizeUninitialized(0);
		m_NextWeatherEffects.ResizeUninitialized(0);
		m_TemperatureBaseHeight = 0f;
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_CurrentClimate;
		reader.Read(out value);
		NativeList<Entity> currentWeatherEffects = m_CurrentWeatherEffects;
		reader.Read(currentWeatherEffects);
		NativeList<Entity> nextWeatherEffects = m_NextWeatherEffects;
		reader.Read(nextWeatherEffects);
		ref float value2 = ref m_TemperatureBaseHeight;
		reader.Read(out value2);
	}

	public void PostDeserialize(Context context)
	{
		if (m_CurrentClimate == Entity.Null || !m_PrefabSystem.TryGetPrefab<ClimatePrefab>(m_CurrentClimate, out var _))
		{
			if (m_CurrentClimate != Entity.Null)
			{
				COSystemBase.baseLog.Error("Missing climate prefab, reverting to default climate");
			}
			using NativeArray<Entity> nativeArray = m_ClimateQuery.ToEntityArray(Allocator.TempJob);
			if (nativeArray.Length > 0)
			{
				m_CurrentClimate = nativeArray[0];
			}
		}
		if (m_CurrentClimate != Entity.Null)
		{
			ClimatePrefab prefab2 = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_CurrentClimate);
			prefab2.EnsureSeasonsOrder(force: true);
			averageTemperature = CalculateTemperatureAverage(prefab2);
			UpdateSeason(prefab2, m_Date);
			if (AreEffectsInvalid(m_CurrentWeatherEffects) || m_CurrentWeatherEffects.Length == 0 || AreEffectsInvalid(m_NextWeatherEffects) || m_NextWeatherEffects.Length == 0)
			{
				UpdateWeather(prefab2);
			}
			else
			{
				ApplyWeatherEffects();
			}
			m_PlanetarySystem.latitude = prefab2.m_Latitude;
			m_PlanetarySystem.longitude = prefab2.m_Longitude;
		}
	}

	private bool AreEffectsInvalid(NativeList<Entity> list)
	{
		for (int i = 0; i < list.Length; i++)
		{
			if (list[i] == Entity.Null)
			{
				list.ResizeUninitialized(0);
				return true;
			}
		}
		return false;
	}

	public ClimateSample SampleClimate(ClimatePrefab prefab, float t)
	{
		float time = t * (float)m_TimeSystem.daysPerYear;
		float num = prefab.m_Temperature.Evaluate(time);
		float num2 = prefab.m_Precipitation.Evaluate(time);
		float num3 = prefab.m_Cloudiness.Evaluate(time);
		float num4 = prefab.m_Aurora.Evaluate(time);
		float num5 = prefab.m_Aurora.Evaluate(time);
		return new ClimateSample
		{
			temperature = num,
			precipitation = num2,
			cloudiness = num3,
			aurora = num4,
			fog = num5
		};
	}

	public ClimateSample SampleClimate(float t)
	{
		if (m_CurrentClimate != Entity.Null)
		{
			ClimatePrefab prefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_CurrentClimate);
			ClimateSample result = SampleClimate(prefab, t);
			if (temperature.overrideState)
			{
				result.temperature = temperature.overrideValue;
			}
			if (precipitation.overrideState)
			{
				result.precipitation = precipitation.overrideValue;
			}
			if (cloudiness.overrideState)
			{
				result.cloudiness = cloudiness.overrideValue;
			}
			if (aurora.overrideState)
			{
				result.aurora = aurora.overrideValue;
			}
			if (fog.overrideState)
			{
				result.fog = fog.overrideValue;
			}
			return result;
		}
		return default(ClimateSample);
	}

	private void UpdateSeason(ClimatePrefab prefab, float normalizedDate)
	{
		SeasonInfo seasonInfo = m_CurrentSeason;
		float startRange;
		float endRange;
		(m_CurrentSeason, startRange, endRange) = prefab.FindSeasonByTime(normalizedDate);
		if (seasonInfo != m_CurrentSeason)
		{
			seasonTemperature = CalculateMeanTemperature(prefab, 48, startRange, endRange);
			seasonPrecipitation = CalculateMeanPrecipitation(prefab, 48, startRange, endRange);
			seasonCloudiness = CalculateMeanCloudiness(prefab, 48, startRange, endRange);
		}
	}

	private bool SelectDefaultWeather(ClimatePrefab prefab, ref NativeList<WeatherTempData> currentWeathers, ref NativeList<WeatherTempData> nextWeathers)
	{
		if (prefab.m_DefaultWeather != null)
		{
			WeatherTempData value = new WeatherTempData
			{
				m_Entity = m_PrefabSystem.GetEntity(prefab.m_DefaultWeather),
				m_Priority = -1001f
			};
			currentWeathers.Add(in value);
			nextWeathers.Add(in value);
			return true;
		}
		return false;
	}

	private bool SelectWeatherPlaceholder(ClimatePrefab prefab, out WeatherPrefab current, out WeatherPrefab next)
	{
		if (prefab.m_DefaultWeathers != null)
		{
			float num = float.MaxValue;
			int num2 = 0;
			for (int i = 0; i < prefab.m_DefaultWeathers.Length; i++)
			{
				WeatherPrefab weatherPrefab = prefab.m_DefaultWeathers[i];
				float num3 = math.max(weatherPrefab.m_CloudinessRange.x - (float)cloudiness, (float)cloudiness - weatherPrefab.m_CloudinessRange.y);
				if (num3 < num)
				{
					num2 = i;
					num = num3;
				}
			}
			current = prefab.m_DefaultWeathers[math.max(num2 - 1, 0)];
			next = prefab.m_DefaultWeathers[num2];
			return true;
		}
		current = null;
		next = null;
		return false;
	}

	private void SelectRandomWeather(WeatherPrefab weather, ref NativeList<WeatherTempData> weathers)
	{
		WeatherTempData value = default(WeatherTempData);
		value.m_Entity = m_PrefabSystem.GetEntity(weather);
		value.m_Priority = -1000f;
		weathers.Add(in value);
		if (!base.EntityManager.TryGetBuffer(value.m_Entity, isReadOnly: true, out DynamicBuffer<PlaceholderObjectElement> buffer))
		{
			return;
		}
		WeatherTempData value2 = default(WeatherTempData);
		for (int i = 0; i < buffer.Length; i++)
		{
			value2.m_Entity = buffer[i].m_Object;
			value2.m_Priority = 0f;
			if (base.EntityManager.TryGetBuffer(value2.m_Entity, isReadOnly: true, out DynamicBuffer<ObjectRequirementElement> buffer2))
			{
				int num = -1;
				bool flag = true;
				for (int j = 0; j < buffer2.Length; j++)
				{
					ObjectRequirementElement objectRequirementElement = buffer2[j];
					if ((objectRequirementElement.m_Type & ObjectRequirementType.SelectOnly) != 0)
					{
						continue;
					}
					if (objectRequirementElement.m_Group != num)
					{
						if (!flag)
						{
							break;
						}
						num = objectRequirementElement.m_Group;
						flag = false;
					}
					flag |= objectRequirementElement.m_Requirement == currentSeason;
					value2.m_Priority = 1000f;
				}
				if (!flag)
				{
					continue;
				}
			}
			WeatherPrefab prefab = m_PrefabSystem.GetPrefab<WeatherPrefab>(value2.m_Entity);
			if ((float)aurora > 0f && prefab.m_RandomizationLayer == WeatherPrefab.RandomizationLayer.Aurora)
			{
				value2.m_Priority = 500f;
				weathers.Add(in value2);
			}
			else if (prefab.m_RandomizationLayer == WeatherPrefab.RandomizationLayer.Cloudiness)
			{
				value2.m_Priority = 250f;
				weathers.Add(in value2);
			}
			else if (prefab.m_RandomizationLayer == WeatherPrefab.RandomizationLayer.Season)
			{
				value2.m_Priority = 300f;
				weathers.Add(in value2);
			}
		}
	}

	private bool ResetWeatherEffects(ref NativeList<Entity> weatherEffects)
	{
		bool result = weatherEffects.Length != 0;
		weatherEffects.Clear();
		return result;
	}

	private bool SortAndCheckUpdate(ref NativeList<WeatherTempData> weatherEffects, ref NativeList<Entity> reference)
	{
		bool flag = false;
		weatherEffects.Sort();
		if (weatherEffects.Length != reference.Length)
		{
			flag = true;
			reference.ResizeUninitialized(weatherEffects.Length);
			for (int i = 0; i < weatherEffects.Length; i++)
			{
				reference[i] = weatherEffects[i].m_Entity;
			}
		}
		else
		{
			for (int j = 0; j < weatherEffects.Length; j++)
			{
				flag |= reference[j] != weatherEffects[j].m_Entity;
				reference[j] = weatherEffects[j].m_Entity;
			}
		}
		return flag;
	}

	private void UpdateWeather(ClimatePrefab prefab)
	{
		bool flag = false;
		NativeList<WeatherTempData> currentWeathers = new NativeList<WeatherTempData>(10, Allocator.Temp);
		NativeList<WeatherTempData> nextWeathers = new NativeList<WeatherTempData>(10, Allocator.Temp);
		if (SelectDefaultWeather(prefab, ref currentWeathers, ref nextWeathers))
		{
			if (SelectWeatherPlaceholder(prefab, out var current, out var next))
			{
				SelectRandomWeather(current, ref currentWeathers);
				SelectRandomWeather(next, ref nextWeathers);
				flag |= SortAndCheckUpdate(ref currentWeathers, ref m_CurrentWeatherEffects);
				flag |= SortAndCheckUpdate(ref nextWeathers, ref m_NextWeatherEffects);
			}
		}
		else
		{
			flag |= ResetWeatherEffects(ref m_CurrentWeatherEffects);
			flag |= ResetWeatherEffects(ref m_NextWeatherEffects);
		}
		currentWeathers.Dispose();
		nextWeathers.Dispose();
		if (flag)
		{
			ApplyWeatherEffects();
		}
	}

	private void ApplyWeatherEffects()
	{
		m_ClimateRenderSystem.Clear();
		for (int i = 0; i < m_CurrentWeatherEffects.Length; i++)
		{
			WeatherPrefab prefab = m_PrefabSystem.GetPrefab<WeatherPrefab>(m_CurrentWeatherEffects[i]);
			if (prefab.m_Classification != WeatherClassification.Irrelevant)
			{
				classification = prefab.m_Classification;
			}
			m_ClimateRenderSystem.ScheduleFrom(prefab);
		}
		for (int j = 0; j < m_NextWeatherEffects.Length; j++)
		{
			WeatherPrefab prefab2 = m_PrefabSystem.GetPrefab<WeatherPrefab>(m_NextWeatherEffects[j]);
			if (prefab2.m_Classification != WeatherClassification.Irrelevant)
			{
				classification = prefab2.m_Classification;
			}
			m_ClimateRenderSystem.ScheduleTo(prefab2);
		}
	}

	[Preserve]
	public ClimateSystem()
	{
	}
}
