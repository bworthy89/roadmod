using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct AreaColorData : IComponentData, IQueryTypeParameter
{
	public Color32 m_FillColor;

	public Color32 m_EdgeColor;

	public Color32 m_SelectionFillColor;

	public Color32 m_SelectionEdgeColor;

	public static AreaColorData GetDefaults()
	{
		return new AreaColorData
		{
			m_FillColor = new Color32(128, 128, 128, 64),
			m_EdgeColor = new Color32(128, 128, 128, 128),
			m_SelectionFillColor = new Color32(128, 128, 128, 128),
			m_SelectionEdgeColor = new Color32(128, 128, 128, byte.MaxValue)
		};
	}
}
