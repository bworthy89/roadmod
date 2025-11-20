using Game.Notifications;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public struct IconDefinition : IComponentData, IQueryTypeParameter
{
	public float3 m_Location;

	public IconPriority m_Priority;

	public IconClusterLayer m_ClusterLayer;

	public IconFlags m_Flags;

	public IconDefinition(Icon icon)
	{
		m_Location = icon.m_Location;
		m_Priority = icon.m_Priority;
		m_ClusterLayer = icon.m_ClusterLayer;
		m_Flags = icon.m_Flags;
	}
}
