using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Simulation;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { })]
public class ClimatePrefab : PrefabBase, IJsonWritable
{
	public struct SeasonTempCurves
	{
		public AnimationCurve nightMin;

		public AnimationCurve nightMax;

		public AnimationCurve dayMin;

		public AnimationCurve dayMax;
	}

	public struct SeasonPrecipCurves
	{
		public AnimationCurve cloudChance;

		public AnimationCurve cloudAmountMin;

		public AnimationCurve cloudAmountMax;

		public AnimationCurve precipChance;

		public AnimationCurve precipAmountMin;

		public AnimationCurve precipAmountMax;

		public AnimationCurve turbulence;
	}

	public struct SeasonAuroraCurves
	{
		public AnimationCurve amount;

		public AnimationCurve chance;
	}

	[Range(-90f, 90f)]
	[EditorName("Editor.CLIMATE_LATITUDE")]
	public float m_Latitude = 61.49772f;

	[Range(-180f, 180f)]
	[EditorName("Editor.CLIMATE_LONGITUDE")]
	public float m_Longitude = 23.767042f;

	[EditorName("Editor.CLIMATE_FREEZING_TEMPERATURE")]
	public float m_FreezingTemperature;

	public AnimationCurve m_Temperature;

	public AnimationCurve m_Precipitation;

	public AnimationCurve m_Cloudiness;

	public AnimationCurve m_Aurora;

	public AnimationCurve m_Fog;

	[EditorName("Editor.CLIMATE_DEFAULT_WEATHER")]
	public WeatherPrefab m_DefaultWeather;

	[EditorName("Editor.CLIMATE_DEFAULT_WEATHERS")]
	public WeatherPrefab[] m_DefaultWeathers;

	[EditorName("Editor.CLIMATE_SEASONS")]
	public ClimateSystem.SeasonInfo[] m_Seasons;

	[HideInInspector]
	public int m_RandomSeed = 1;

	public const int kYearDuration = 12;

	private int[] m_SeasonsOrder;

	private const float k90PercentileToStdDev = 0.78003126f;

	public Bounds1 temperatureRange
	{
		get
		{
			Bounds1 result = new Bounds1(float.MaxValue, float.MinValue);
			for (int i = 0; i < 288; i++)
			{
				result |= m_Temperature.Evaluate((float)i / 288f * 12f);
			}
			return result;
		}
	}

	public float averageCloudiness
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < 288; i++)
			{
				float time = (float)i / 288f * 12f;
				num += m_Cloudiness.Evaluate(time);
			}
			return num / 288f;
		}
	}

	public float averagePrecipitation
	{
		get
		{
			float num = 0f;
			for (int i = 0; i < 288; i++)
			{
				float time = (float)i / 288f * 12f;
				num += m_Precipitation.Evaluate(time);
			}
			return num / 288f;
		}
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().Name);
		writer.PropertyName("latitude");
		writer.Write(m_Latitude);
		writer.PropertyName("longitude");
		writer.Write(m_Longitude);
		writer.PropertyName("freezingTemperature");
		writer.Write(m_FreezingTemperature);
		writer.PropertyName("seasons");
		writer.Write((IList<ClimateSystem.SeasonInfo>)m_Seasons);
		writer.TypeEnd();
	}

	public void RebuildCurves()
	{
		EnsureSeasonsOrder(force: true);
		uint num = (uint)m_RandomSeed;
		if (num == 0)
		{
			num = (uint)(Time.realtimeSinceStartup * 10f);
		}
		RebuildTemperatureCurves(num);
		RebuildPrecipitationCurves(num);
		RebuildAuroraCurves(num);
		RebuildFogCurves(num);
	}

	internal void EnsureSeasonsOrder(bool force = false)
	{
		if (force || m_SeasonsOrder == null || m_SeasonsOrder.Length != m_Seasons.Length)
		{
			m_SeasonsOrder = (from v in Enumerable.Range(0, m_Seasons.Length)
				orderby m_Seasons[v].m_StartTime
				select v).ToArray();
		}
	}

	private static AnimationCurve GenCurveFromMinMax(int keyCount, AnimationCurve cmin, AnimationCurve cmax, uint seed, float minValue, float maxValue)
	{
		Unity.Mathematics.Random rng = new Unity.Mathematics.Random(seed);
		Keyframe[] array = new Keyframe[keyCount];
		for (int i = 0; i < array.Length; i++)
		{
			float time = (float)i / (float)array.Length * 12f;
			float num = cmin.Evaluate(time);
			float num2 = cmax.Evaluate(time);
			float dev = (num2 - num) / 2f * 0.78003126f;
			array[i].time = time;
			array[i].value = GaussianRandom((num + num2) / 2f, dev, ref rng);
		}
		AnimationCurve animationCurve = new AnimationCurve(array);
		LoopCurve(animationCurve, minValue, maxValue);
		return animationCurve;
	}

	private void RebuildTemperatureCurves(uint seed)
	{
		SeasonTempCurves seasonTempCurves = CreateSeasonTemperatureCurves();
		AnimationCurve animationCurve = GenCurveFromMinMax(12, seasonTempCurves.nightMin, seasonTempCurves.nightMax, seed + 10000, -100f, 100f);
		AnimationCurve animationCurve2 = GenCurveFromMinMax(12, seasonTempCurves.dayMin, seasonTempCurves.dayMax, seed + 11000, -100f, 100f);
		Keyframe[] array = new Keyframe[288];
		for (int i = 0; i < 288; i++)
		{
			float num = (float)i / 288f * 12f;
			array[i].time = num;
			float start = animationCurve.Evaluate(num);
			float end = animationCurve2.Evaluate(num);
			float num2 = (float)(i % 24) / 24f;
			float num3 = noise.cnoise(new float2(num * 4f, 0f)) / 24f * 4f;
			float t = (0f - math.cos((num2 + num3) * MathF.PI * 2f)) * 0.5f + 0.5f;
			array[i].value = math.lerp(start, end, t);
		}
		m_Temperature = new AnimationCurve(array);
		LoopCurve(m_Temperature, -100f, 100f);
	}

	private void RebuildPrecipitationCurves(uint seed)
	{
		SeasonPrecipCurves seasonPrecipCurves = CreateSeasonPrecipCurves();
		AnimationCurve animationCurve = GenCurveFromMinMax(12, seasonPrecipCurves.cloudAmountMin, seasonPrecipCurves.cloudAmountMax, seed + 1000, 0f, 1f);
		Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed + 2000);
		Keyframe[] array = new Keyframe[1728];
		float num = random.NextFloat(0f, 100f);
		float y = random.NextFloat(0f, 100f);
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = (float)i / (float)array.Length * 12f;
			array[i].time = num2;
			float num3 = math.saturate(animationCurve.Evaluate(num2));
			float num4 = math.saturate(seasonPrecipCurves.turbulence.Evaluate(num2));
			float num5 = SmoothNoise(num2 * 4f, y);
			num5 *= num4;
			num5 *= num3;
			num3 = math.saturate(num3 + num5);
			float num6 = math.saturate(seasonPrecipCurves.cloudChance.Evaluate(num2));
			float num7 = math.saturate((SmoothNoise(num2, num) + SmoothNoise(num2 * 2f, num + 7f) * 0.5f) * 0.5f + 0.5f);
			if (num7 > num6)
			{
				num3 *= 1f - math.saturate((num7 - num6) * 2f);
			}
			array[i].value = num3;
		}
		m_Cloudiness = new AnimationCurve(array);
		LoopCurve(m_Cloudiness);
		AnimationCurve animationCurve2 = GenCurveFromMinMax(12, seasonPrecipCurves.precipAmountMin, seasonPrecipCurves.precipAmountMax, seed + 3000, 0f, 1f);
		random = new Unity.Mathematics.Random(seed + 4000);
		array = new Keyframe[1728];
		num = random.NextFloat(0f, 100f);
		y = random.NextFloat(0f, 100f);
		for (int j = 0; j < array.Length; j++)
		{
			float num8 = (float)j / (float)array.Length * 12f;
			array[j].time = num8;
			float num9 = math.saturate(animationCurve2.Evaluate(num8));
			float num10 = math.saturate(seasonPrecipCurves.turbulence.Evaluate(num8));
			float num11 = SmoothNoise(num8 * 4f, y);
			num11 *= num10;
			num11 *= num9;
			num9 = math.saturate(num9 + num11);
			float num12 = math.saturate(seasonPrecipCurves.precipChance.Evaluate(num8));
			float num13 = math.saturate((SmoothNoise(num8, num) + SmoothNoise(num8 * 2f, num + 7f) * 0.5f) * 0.5f + 0.5f);
			if (num13 > num12)
			{
				num9 *= 1f - math.saturate((num13 - num12) * 2f);
			}
			float num14 = m_Cloudiness.Evaluate(num8);
			if (num14 < 0.7f)
			{
				num9 *= num14 / 0.7f;
			}
			if (num14 < 0.4f)
			{
				num9 *= num14 / 0.4f;
			}
			if (num14 < 0.2f)
			{
				num9 = 0f;
			}
			array[j].value = num9;
		}
		m_Precipitation = new AnimationCurve(array);
		LoopCurve(m_Precipitation);
	}

	private static float SmoothNoise(float x, float y = 0f)
	{
		return noise.snoise(new float2(x, y));
	}

	private void RebuildAuroraCurves(uint seed)
	{
		SeasonAuroraCurves seasonAuroraCurves = CreateSeasonAuroraCurves();
		Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed + 5000);
		Keyframe[] array = new Keyframe[288];
		float num = random.NextFloat(0f, 100f);
		float y = random.NextFloat(0f, 100f);
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = (float)i / (float)array.Length * 12f;
			array[i].time = num2;
			float num3 = math.max(0f, seasonAuroraCurves.amount.Evaluate(num2));
			float num4 = 0.1f;
			float num5 = SmoothNoise(num2 * 4f, y);
			num5 *= num4;
			num5 *= num3;
			num3 = math.max(0f, num3 + num5);
			float num6 = math.saturate(seasonAuroraCurves.chance.Evaluate(num2));
			float num7 = math.saturate((SmoothNoise(num2, num) + SmoothNoise(num2 * 2f, num + 7f) * 0.5f) * 0.5f + 0.5f);
			if (num7 > num6)
			{
				num3 *= 1f - math.saturate((num7 - num6) * 8f);
			}
			array[i].value = num3;
		}
		m_Aurora = new AnimationCurve(array);
		LoopCurve(m_Aurora, 0f, 10f);
	}

	private void RebuildFogCurves(uint seed)
	{
		Keyframe[] array = new Keyframe[288];
		float num = 1f / 24f;
		float num2 = 2f;
		float num3 = 0.15f;
		float num4 = -1f;
		float num5 = 25f;
		float num6 = 0.5f;
		for (int i = 0; i < array.Length; i++)
		{
			float num7 = (float)i / (float)array.Length * 12f;
			array[i].time = num7;
			float num8 = m_Cloudiness.Evaluate(num7);
			float num9 = m_Precipitation.Evaluate(num7);
			float num10 = m_Temperature.Evaluate(num7);
			float num11 = 0f;
			if (num8 > num3 && num10 > num4 && num10 < num5 && num9 < num6)
			{
				float num12 = m_Temperature.Evaluate(num7 - 8f * num);
				float num13 = m_Temperature.Evaluate(num7 - 7f * num);
				float num14 = m_Temperature.Evaluate(num7 - 6f * num);
				float num15 = m_Temperature.Evaluate(num7 - 5f * num);
				float num16 = m_Temperature.Evaluate(num7 - 4f * num);
				float num17 = m_Temperature.Evaluate(num7 - 3f * num);
				float num18 = m_Temperature.Evaluate(num7 - 2f * num);
				float num19 = m_Temperature.Evaluate(num7 - 1f * num);
				if (num12 - num13 > num2)
				{
					num11 += 0.19f;
				}
				if (num13 - num14 > num2)
				{
					num11 += 0.17f;
				}
				if (num14 - num15 > num2)
				{
					num11 += 0.12f;
				}
				if (num15 - num16 > num2)
				{
					num11 += 0.09f;
				}
				if (num16 - num17 > num2)
				{
					num11 += 0.11f;
				}
				if (num17 - num18 > num2)
				{
					num11 += 0.13f;
				}
				if (num18 - num19 > num2)
				{
					num11 += 0.14f;
				}
				if (num19 - num10 > num2)
				{
					num11 += 0.15f;
				}
				if (num12 - num15 > num2)
				{
					num11 += 0.21f;
				}
				if (num13 - num16 > num2)
				{
					num11 += 0.18f;
				}
				if (num14 - num17 > num2)
				{
					num11 += 0.07f;
				}
			}
			array[i].value = math.saturate(num11);
		}
		m_Fog = new AnimationCurve(array);
		LoopCurve(m_Aurora);
	}

	private static float GaussianRandom(float mean, float dev, ref Unity.Mathematics.Random rng)
	{
		int num = 0;
		float num4;
		do
		{
			float x = rng.NextFloat();
			float num2 = rng.NextFloat();
			float num3 = math.sqrt(-2f * math.log(x)) * math.sin(MathF.PI * 2f * num2);
			num4 = mean + dev * num3;
		}
		while (math.abs(num4 - mean) > 2f * dev && num++ < 20);
		return num4;
	}

	public SeasonTempCurves CreateSeasonTemperatureCurves()
	{
		SeasonTempCurves result = new SeasonTempCurves
		{
			nightMin = new AnimationCurve(),
			nightMax = new AnimationCurve(),
			dayMin = new AnimationCurve(),
			dayMax = new AnimationCurve()
		};
		for (int i = 0; i < m_Seasons.Length; i++)
		{
			(ClimateSystem.SeasonInfo, float) seasonAndMidTime = GetSeasonAndMidTime(i);
			ClimateSystem.SeasonInfo item = seasonAndMidTime.Item1;
			float item2 = seasonAndMidTime.Item2;
			float2 tempNightDay = item.m_TempNightDay;
			float2 @float = math.abs(item.m_TempDeviationNightDay);
			result.nightMin.AddKey(item2, tempNightDay.x - @float.x);
			result.nightMax.AddKey(item2, tempNightDay.x + @float.x);
			result.dayMin.AddKey(item2, tempNightDay.y - @float.y);
			result.dayMax.AddKey(item2, tempNightDay.y + @float.y);
		}
		LoopCurve(result.nightMin, -100f, 100f);
		LoopCurve(result.nightMax, -100f, 100f);
		LoopCurve(result.dayMin, -100f, 100f);
		LoopCurve(result.dayMax, -100f, 100f);
		return result;
	}

	public (ClimateSystem.SeasonInfo, float) GetSeasonAndMidTime(int index)
	{
		EnsureSeasonsOrder();
		return (m_Seasons[m_SeasonsOrder[index]], GetSeasonMidTime(index));
	}

	public int CountElapsedSeasons(float startTime, float elapsedTime)
	{
		if (m_Seasons == null || m_Seasons.Length == 0)
		{
			return 0;
		}
		if (m_Seasons.Length == 1)
		{
			return 1;
		}
		int num = 0;
		for (int i = 0; i < m_SeasonsOrder.Length; i++)
		{
			ClimateSystem.SeasonInfo seasonInfo = m_Seasons[m_SeasonsOrder[i]];
			ClimateSystem.SeasonInfo obj = m_Seasons[m_SeasonsOrder[(i + 1) % m_Seasons.Length]];
			float startTime2 = seasonInfo.m_StartTime;
			float startTime3 = obj.m_StartTime;
			if (Intersect(startTime, elapsedTime, startTime2, startTime3))
			{
				num++;
			}
		}
		return num;
	}

	private bool Intersect(float startTime, float elapsedTime, float seasonStart, float seasonEnd)
	{
		if (seasonEnd < seasonStart)
		{
			if (startTime < seasonEnd)
			{
				startTime += 1f;
			}
			seasonEnd += 1f;
		}
		if (startTime > seasonEnd)
		{
			startTime -= 1f;
		}
		if (startTime < seasonEnd)
		{
			return startTime + elapsedTime > seasonStart;
		}
		return false;
	}

	public (ClimateSystem.SeasonInfo, float, float) FindSeasonByTime(float time)
	{
		if (m_Seasons == null || m_Seasons.Length == 0)
		{
			return (null, 0f, 1f);
		}
		if (m_Seasons.Length == 1)
		{
			return (m_Seasons[0], 0f, 1f);
		}
		for (int i = 0; i < m_SeasonsOrder.Length; i++)
		{
			ClimateSystem.SeasonInfo seasonInfo = m_Seasons[m_SeasonsOrder[i]];
			ClimateSystem.SeasonInfo obj = m_Seasons[m_SeasonsOrder[(i + 1) % m_Seasons.Length]];
			float startTime = seasonInfo.m_StartTime;
			float num = obj.m_StartTime;
			if (num < startTime)
			{
				num += 1f;
			}
			if (time >= startTime && time < num)
			{
				return (seasonInfo, startTime, num);
			}
			if (num > 1f && time < num - 1f)
			{
				return (seasonInfo, startTime, num);
			}
		}
		return (m_Seasons[0], 0f, 1f);
	}

	private float GetSeasonMidTime(int index)
	{
		ClimateSystem.SeasonInfo seasonInfo = m_Seasons[m_SeasonsOrder[index]];
		ClimateSystem.SeasonInfo obj = m_Seasons[m_SeasonsOrder[(index + 1) % m_Seasons.Length]];
		float startTime = seasonInfo.m_StartTime;
		float num = obj.m_StartTime;
		if (num < startTime)
		{
			num += 1f;
		}
		return (startTime + num) * 0.5f * 12f % 12f;
	}

	public SeasonPrecipCurves CreateSeasonPrecipCurves()
	{
		SeasonPrecipCurves result = new SeasonPrecipCurves
		{
			cloudChance = new AnimationCurve(),
			cloudAmountMin = new AnimationCurve(),
			cloudAmountMax = new AnimationCurve(),
			precipChance = new AnimationCurve(),
			precipAmountMin = new AnimationCurve(),
			precipAmountMax = new AnimationCurve(),
			turbulence = new AnimationCurve()
		};
		for (int i = 0; i < m_Seasons.Length; i++)
		{
			(ClimateSystem.SeasonInfo, float) seasonAndMidTime = GetSeasonAndMidTime(i);
			ClimateSystem.SeasonInfo item = seasonAndMidTime.Item1;
			float item2 = seasonAndMidTime.Item2;
			float num = item.m_CloudAmount * 0.01f;
			float num2 = math.abs(item.m_CloudAmountDeviation) * 0.01f;
			float value = item.m_CloudChance * 0.01f;
			float num3 = item.m_PrecipitationAmount * 0.01f;
			float num4 = math.abs(item.m_PrecipitationAmountDeviation) * 0.01f;
			float value2 = item.m_PrecipitationChance * 0.01f;
			result.cloudAmountMin.AddKey(item2, num - num2);
			result.cloudAmountMax.AddKey(item2, num + num2);
			result.cloudChance.AddKey(item2, value);
			result.precipAmountMin.AddKey(item2, num3 - num4);
			result.precipAmountMax.AddKey(item2, num3 + num4);
			result.precipChance.AddKey(item2, value2);
			result.turbulence.AddKey(item2, item.m_Turbulence);
		}
		LoopCurve(result.cloudChance);
		LoopCurve(result.cloudAmountMin);
		LoopCurve(result.cloudAmountMax);
		LoopCurve(result.precipChance);
		LoopCurve(result.precipAmountMin);
		LoopCurve(result.precipAmountMax);
		LoopCurve(result.turbulence);
		return result;
	}

	private static void LoopCurve(AnimationCurve curve, float minValue = 0f, float maxValue = 1f)
	{
		WrapMode preWrapMode = (curve.postWrapMode = WrapMode.Loop);
		curve.preWrapMode = preWrapMode;
		for (int i = 0; i < curve.length; i++)
		{
			curve.SmoothTangents(i, 1f / 3f);
		}
		Keyframe[] keys = curve.keys;
		bool flag = false;
		for (int j = 0; j < keys.Length; j++)
		{
			Keyframe keyframe = keys[j];
			if (keyframe.value <= minValue)
			{
				keyframe.value = minValue;
				float inTangent = (keyframe.outTangent = 0f);
				keyframe.inTangent = inTangent;
				keys[j] = keyframe;
				flag = true;
			}
			if (keyframe.value >= maxValue)
			{
				keyframe.value = maxValue;
				float inTangent = (keyframe.outTangent = 0f);
				keyframe.inTangent = inTangent;
				keys[j] = keyframe;
				flag = true;
			}
		}
		if (flag)
		{
			curve.keys = keys;
		}
		Keyframe key = keys[0];
		key.inTangent = 0f;
		key.outTangent = 0f;
		curve.MoveKey(0, key);
		key.time += 12f;
		curve.AddKey(key);
	}

	public SeasonAuroraCurves CreateSeasonAuroraCurves()
	{
		SeasonAuroraCurves result = new SeasonAuroraCurves
		{
			amount = new AnimationCurve(),
			chance = new AnimationCurve()
		};
		for (int i = 0; i < m_Seasons.Length; i++)
		{
			var (seasonInfo, time) = GetSeasonAndMidTime(i);
			result.amount.AddKey(time, seasonInfo.m_AuroraAmount);
			result.chance.AddKey(time, seasonInfo.m_AuroraChance * 0.01f);
		}
		LoopCurve(result.amount, 0f, 10f);
		LoopCurve(result.chance);
		return result;
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ClimateData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Seasons != null)
		{
			ClimateSystem.SeasonInfo[] seasons = m_Seasons;
			foreach (ClimateSystem.SeasonInfo seasonInfo in seasons)
			{
				prefabs.Add(seasonInfo.m_Prefab);
			}
		}
		if (m_DefaultWeather != null)
		{
			prefabs.Add(m_DefaultWeather);
		}
		if (m_DefaultWeathers == null)
		{
			return;
		}
		WeatherPrefab[] defaultWeathers = m_DefaultWeathers;
		foreach (WeatherPrefab weatherPrefab in defaultWeathers)
		{
			if (weatherPrefab.active)
			{
				prefabs.Add(weatherPrefab);
			}
		}
	}
}
