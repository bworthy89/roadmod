using Unity.Entities;

namespace Game.Prefabs;

public struct BuildingModule : IBufferElementData
{
	public Entity m_Module;

	public BuildingModule(Entity module)
	{
		m_Module = module;
	}
}
