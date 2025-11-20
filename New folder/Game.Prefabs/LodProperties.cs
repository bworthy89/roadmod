using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class LodProperties : ComponentBase
{
	public float m_Bias;

	public float m_ShadowBias;

	public RenderPrefab[] m_LodMeshes;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_LodMeshes != null)
		{
			for (int i = 0; i < m_LodMeshes.Length; i++)
			{
				prefabs.Add(m_LodMeshes[i]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LodMesh>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
