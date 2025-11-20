using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct AreaNameData : IComponentData, IQueryTypeParameter
{
	public Color32 m_Color;

	public Color32 m_SelectedColor;

	public static AreaNameData GetDefaults()
	{
		return new AreaNameData
		{
			m_Color = new Color32(128, 128, 128, 128),
			m_SelectedColor = new Color32(128, 128, 128, byte.MaxValue)
		};
	}
}
