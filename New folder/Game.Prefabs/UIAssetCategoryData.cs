using Unity.Entities;

namespace Game.Prefabs;

public struct UIAssetCategoryData : IComponentData, IQueryTypeParameter
{
	public Entity m_Menu;

	public UIAssetCategoryData(Entity menu)
	{
		m_Menu = menu;
	}
}
