using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public class CharacterCull : PrefabBase
{
	public int m_maskIndex;

	public int m_meshIndex;

	public int[] m_culledVertices;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CharacterCullData>());
	}
}
