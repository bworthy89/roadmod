using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs.Climate;

[ComponentMenu("Weather/", new Type[] { typeof(WeatherPrefab) })]
public class WeatherPlaceholder : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceholderObjectElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (base.prefab.Has<WeatherOverride>())
		{
			ComponentBase.baseLog.WarnFormat(base.prefab, "WeatherPlaceholder is WeatherOverride: {0}", base.prefab.name);
		}
	}
}
