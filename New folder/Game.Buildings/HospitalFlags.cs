using System;

namespace Game.Buildings;

[Flags]
public enum HospitalFlags : byte
{
	HasAvailableAmbulances = 1,
	HasAvailableMedicalHelicopters = 2,
	CanCureDisease = 4,
	HasRoomForPatients = 0x10,
	CanProcessCorpses = 0x20,
	CanCureInjury = 0x40
}
