using System;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct DeliveryTruckSelectItem : IComparable<DeliveryTruckSelectItem>
{
	public int m_Capacity;

	public int m_Cost;

	public Resource m_Resources;

	public Entity m_Prefab1;

	public Entity m_Prefab2;

	public Entity m_Prefab3;

	public Entity m_Prefab4;

	public int CompareTo(DeliveryTruckSelectItem other)
	{
		return m_Capacity - other.m_Capacity;
	}
}
