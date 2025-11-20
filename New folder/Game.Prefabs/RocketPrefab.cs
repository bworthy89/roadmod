using System;
using Game.Vehicles;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class RocketPrefab : HelicopterPrefab
{
	protected override HelicopterType GetHelicopterType()
	{
		return HelicopterType.Rocket;
	}
}
