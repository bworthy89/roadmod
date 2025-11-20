using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class DilationProperties : ComponentBase
{
	public float m_MinSize = 0.1f;

	public float m_InfoviewFactor = 1f;

	public bool m_InfoviewOnly;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}
}
