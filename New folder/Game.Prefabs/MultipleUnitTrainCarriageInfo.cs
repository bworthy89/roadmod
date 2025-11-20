using System;

namespace Game.Prefabs;

[Serializable]
public class MultipleUnitTrainCarriageInfo
{
	public MultipleUnitTrainCarPrefab m_Carriage;

	public VehicleCarriageDirection m_Direction;

	public int m_MinCount;

	public int m_MaxCount;
}
