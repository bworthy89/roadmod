using Unity.Entities;

namespace Game.Tutorials;

public struct InfoviewActivationData : IComponentData, IQueryTypeParameter
{
	public Entity m_Infoview;

	public InfoviewActivationData(Entity infoview)
	{
		m_Infoview = infoview;
	}
}
