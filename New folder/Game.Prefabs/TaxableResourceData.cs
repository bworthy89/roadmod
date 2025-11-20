using System.Collections.Generic;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

public struct TaxableResourceData : IComponentData, IQueryTypeParameter
{
	public byte m_TaxAreas;

	public bool Contains(TaxAreaType areaType)
	{
		return (m_TaxAreas & GetBit(areaType)) != 0;
	}

	public TaxableResourceData(IEnumerable<TaxAreaType> taxAreas)
	{
		m_TaxAreas = 0;
		foreach (TaxAreaType taxArea in taxAreas)
		{
			m_TaxAreas |= (byte)GetBit(taxArea);
		}
	}

	private static int GetBit(TaxAreaType areaType)
	{
		return 1 << (int)(areaType - 1);
	}
}
