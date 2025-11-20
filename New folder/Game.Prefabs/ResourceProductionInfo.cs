using System;
using Game.Economy;

namespace Game.Prefabs;

[Serializable]
public class ResourceProductionInfo
{
	public ResourceInEditor m_Resource;

	public int m_ProductionRate;

	public int m_StorageCapacity;
}
