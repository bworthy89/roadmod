using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct StatisticParameterData : IBufferElementData
{
	public int m_Value;

	public Color m_Color;

	public StatisticParameterData(int value, Color color)
	{
		m_Value = value;
		m_Color = color;
	}
}
