using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class MovePieceVertices : ComponentBase
{
	public bool m_LowerBottomToTerrain = true;

	public bool m_RaiseTopToTerrain;

	public bool m_SmoothTopNormal;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
