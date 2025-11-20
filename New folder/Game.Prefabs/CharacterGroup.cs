using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

public class CharacterGroup : RenderPrefabBase
{
	[Serializable]
	public class Character
	{
		public CharacterStyle m_Style;

		public Meta m_Meta;

		public RenderPrefab[] m_MeshPrefabs;
	}

	[Serializable]
	public class OverrideInfo
	{
		public CharacterGroup m_Group;

		public ObjectState m_RequireState;

		public CharacterProperties.BodyPart m_OverrideBodyParts;

		public bool m_overrideMaskWeights;

		public bool m_OverrideShapeWeights;
	}

	[Serializable]
	public struct IndexWeight
	{
		public int index;

		public float weight;

		public IndexWeight(int i, float w)
		{
			index = i;
			weight = w;
		}
	}

	[Serializable]
	public struct IndexWeight8
	{
		public IndexWeight w0;

		public IndexWeight w1;

		public IndexWeight w2;

		public IndexWeight w3;

		public IndexWeight w4;

		public IndexWeight w5;

		public IndexWeight w6;

		public IndexWeight w7;
	}

	[Serializable]
	public struct Meta
	{
		public IndexWeight8 shapeWeights;

		public IndexWeight8 textureWeights;

		public IndexWeight8 overlayWeights;

		public IndexWeight8 maskWeights;
	}

	public Character[] m_Characters;

	public OverrideInfo[] m_Overrides;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		Character[] characters = m_Characters;
		foreach (Character character in characters)
		{
			prefabs.Add(character.m_Style);
			RenderPrefab[] meshPrefabs = character.m_MeshPrefabs;
			foreach (RenderPrefab item in meshPrefabs)
			{
				prefabs.Add(item);
			}
		}
		if (m_Overrides != null)
		{
			OverrideInfo[] overrides = m_Overrides;
			foreach (OverrideInfo overrideInfo in overrides)
			{
				prefabs.Add(overrideInfo.m_Group);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CharacterGroupData>());
	}
}
