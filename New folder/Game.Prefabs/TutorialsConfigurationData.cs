using Unity.Entities;

namespace Game.Prefabs;

public struct TutorialsConfigurationData : IComponentData, IQueryTypeParameter
{
	public Entity m_TutorialsIntroList;

	public Entity m_MapTilesFeature;
}
