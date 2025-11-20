using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Routes;
using Game.Vehicles;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ParkingSection : InfoSectionBase
{
	private int parkingFee;

	private int parkedCars;

	private int parkingCapacity;

	protected override string group => "ParkingSection";

	protected override void Reset()
	{
		parkingFee = 0;
		parkedCars = 0;
		parkingCapacity = 0;
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<ParkingFacility>(selectedEntity))
		{
			return base.EntityManager.HasComponent<ParkingSpace>(selectedEntity);
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		int laneCount = 0;
		VehicleUtils.GetParkingData(this, selectedEntity, ref laneCount, ref parkingCapacity, ref parkedCars, ref parkingFee);
		if (laneCount != 0)
		{
			parkingFee /= laneCount;
		}
		if (parkingCapacity < 0)
		{
			parkingCapacity = 0;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("parkedCars");
		writer.Write(parkedCars);
		writer.PropertyName("parkingCapacity");
		writer.Write(parkingCapacity);
	}

	[Preserve]
	public ParkingSection()
	{
	}
}
