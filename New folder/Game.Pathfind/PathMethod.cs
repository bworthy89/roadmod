using System;

namespace Game.Pathfind;

[Flags]
public enum PathMethod : ushort
{
	Pedestrian = 1,
	Road = 2,
	Parking = 4,
	PublicTransportDay = 8,
	Track = 0x10,
	Taxi = 0x20,
	CargoTransport = 0x40,
	CargoLoading = 0x80,
	Flying = 0x100,
	PublicTransportNight = 0x200,
	Boarding = 0x400,
	Offroad = 0x800,
	SpecialParking = 0x1000,
	MediumRoad = 0x2000,
	Bicycle = 0x4000,
	BicycleParking = 0x8000
}
