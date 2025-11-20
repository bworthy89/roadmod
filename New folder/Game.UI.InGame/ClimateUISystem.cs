using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Simulation;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class ClimateUISystem : UISystemBase
{
	public const string kGroup = "climate";

	private ClimateSystem m_ClimateSystem;

	private EntityQuery m_ClimateQuery;

	private EntityQuery m_ClimateSeasonQuery;

	private EntityQuery m_SeasonChangedQuery;

	private GetterValueBinding<float> m_TemperatureBinding;

	private GetterValueBinding<WeatherType> m_WeatherBinding;

	private GetterValueBinding<string> m_SeasonBinding;

	private Entity m_CurrentSeason;

	private float m_TemperatureBindingValue => MathUtils.Snap(m_ClimateSystem.temperature, 0.1f);

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		AddBinding(m_TemperatureBinding = new GetterValueBinding<float>("climate", "temperature", () => m_TemperatureBindingValue));
		AddBinding(m_WeatherBinding = new GetterValueBinding<WeatherType>("climate", "weather", GetWeather, new DelegateWriter<WeatherType>(WriteWeatherType)));
		AddBinding(m_SeasonBinding = new GetterValueBinding<string>("climate", "seasonNameId", GetCurrentSeasonNameID, ValueWriters.Nullable(new StringWriter())));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TemperatureBinding.Update();
		m_WeatherBinding.Update();
		if (!m_SeasonBinding.Update() && m_CurrentSeason != m_ClimateSystem.currentSeason)
		{
			m_SeasonBinding.TriggerUpdate();
		}
		m_CurrentSeason = m_ClimateSystem.currentSeason;
	}

	public WeatherType GetWeather()
	{
		if (m_ClimateSystem.isPrecipitating)
		{
			if (m_ClimateSystem.isRaining)
			{
				return WeatherType.Rain;
			}
			if (m_ClimateSystem.isSnowing)
			{
				return WeatherType.Snow;
			}
			return WeatherType.Clear;
		}
		return FromWeatherClassification(m_ClimateSystem.classification);
	}

	private static WeatherType FromWeatherClassification(ClimateSystem.WeatherClassification classification)
	{
		return classification switch
		{
			ClimateSystem.WeatherClassification.Clear => WeatherType.Clear, 
			ClimateSystem.WeatherClassification.Few => WeatherType.Few, 
			ClimateSystem.WeatherClassification.Scattered => WeatherType.Scattered, 
			ClimateSystem.WeatherClassification.Broken => WeatherType.Broken, 
			ClimateSystem.WeatherClassification.Overcast => WeatherType.Overcast, 
			ClimateSystem.WeatherClassification.Stormy => WeatherType.Storm, 
			_ => WeatherType.Clear, 
		};
	}

	private string GetCurrentSeasonNameID()
	{
		return m_ClimateSystem.currentSeasonNameID;
	}

	private static void WriteWeatherType(IJsonWriter writer, WeatherType type)
	{
		writer.Write((int)type);
	}

	[Preserve]
	public ClimateUISystem()
	{
	}
}
