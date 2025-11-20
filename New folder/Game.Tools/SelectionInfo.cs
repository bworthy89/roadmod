using Game.Areas;
using Unity.Entities;

namespace Game.Tools;

public struct SelectionInfo : IComponentData, IQueryTypeParameter
{
	public SelectionType m_SelectionType;

	public AreaType m_AreaType;
}
