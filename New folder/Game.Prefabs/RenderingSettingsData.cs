using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

public struct RenderingSettingsData : IComponentData, IQueryTypeParameter
{
	public Color m_HoveredColor;

	public Color m_OverrideColor;

	public Color m_WarningColor;

	public Color m_ErrorColor;

	public Color m_OwnerColor;
}
