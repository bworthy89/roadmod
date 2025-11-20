using System;
using System.Collections.Generic;
using Colossal;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { })]
public class WeatherPrefab : PrefabBase
{
	public enum RandomizationLayer
	{
		None,
		Cloudiness,
		Aurora,
		Season
	}

	public RandomizationLayer m_RandomizationLayer;

	[MinMaxSlider(0f, 1f)]
	public float2 m_CloudinessRange;

	public ClimateSystem.WeatherClassification m_Classification;

	public IReadOnlyCollection<OverrideablePropertiesComponent> overrideableProperties { get; private set; }

	protected override void OnEnable()
	{
		base.OnEnable();
		List<OverrideablePropertiesComponent> list = new List<OverrideablePropertiesComponent>();
		GetComponents(list);
		overrideableProperties = list.AsReadOnly();
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<WeatherData>());
	}
}
