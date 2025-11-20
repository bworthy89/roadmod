using System;
using System.Collections.Generic;
using Game.Rendering;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[]
{
	typeof(RenderPrefab),
	typeof(CharacterOverlay)
})]
public class ColorProperties : ComponentBase
{
	[Serializable]
	public class VariationSet
	{
		[FixedLength]
		[ColorUsage(true)]
		public Color[] m_Colors = new Color[3];

		public string m_VariationGroup;
	}

	[Serializable]
	public class ColorChannelBinding
	{
		[ListElementLabel("Index {1} -> Channel {0}", false)]
		[HideInEditor]
		public sbyte m_ChannelId;

		public bool m_CanBeModifiedByExternal;
	}

	[Serializable]
	public class VariationGroup
	{
		public string m_Name;

		[Range(0f, 100f)]
		public int m_Probability = 100;

		public ColorSyncFlags m_MeshSyncMode;

		public bool m_OverrideRandomness;

		public int3 m_VariationRanges = new int3(5, 5, 5);

		public int3 m_AlphaRanges = new int3(0, 0, 0);
	}

	[ElementCustomField(typeof(ColorVariationField))]
	public List<VariationSet> m_ColorVariations = new List<VariationSet>();

	[FixedLength]
	public List<ColorChannelBinding> m_ChannelsBinding = new List<ColorChannelBinding>
	{
		new ColorChannelBinding
		{
			m_ChannelId = 0
		},
		new ColorChannelBinding
		{
			m_ChannelId = 1
		},
		new ColorChannelBinding
		{
			m_ChannelId = 2
		}
	};

	[ElementCustomField(typeof(VariationGroupField))]
	public List<VariationGroup> m_VariationGroups;

	[CustomField(typeof(ColorPropertiesVariationRangesField))]
	public int3 m_VariationRanges = new int3(5, 5, 5);

	[CustomField(typeof(ColorPropertiesAlphaRangesField))]
	public int3 m_AlphaRanges = new int3(0, 0, 0);

	public ColorSourceType m_ExternalColorSource;

	public bool SanityCheck(sbyte channel)
	{
		if (m_ChannelsBinding != null && channel >= 0)
		{
			return channel < m_ChannelsBinding.Count;
		}
		return false;
	}

	public bool CanBeModifiedByExternal(sbyte channel)
	{
		if (SanityCheck(channel))
		{
			return m_ChannelsBinding[channel].m_CanBeModifiedByExternal;
		}
		return true;
	}

	public Color GetColor(int index, sbyte channel)
	{
		if (SanityCheck(channel) && m_ColorVariations.Count > 0)
		{
			index %= m_ColorVariations.Count;
			return m_ColorVariations[index].m_Colors[m_ChannelsBinding[channel].m_ChannelId];
		}
		return Color.white;
	}

	public int GetAlpha(int3 alphas, sbyte channel, int def)
	{
		if (SanityCheck(channel))
		{
			return alphas[m_ChannelsBinding[channel].m_ChannelId];
		}
		return def;
	}

	public float GetAlpha(float3 alphas, sbyte channel, float def)
	{
		if (SanityCheck(channel))
		{
			return alphas[m_ChannelsBinding[channel].m_ChannelId];
		}
		return def;
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ColorVariation>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		MeshColorSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<MeshColorSystem>();
		ColorVariation colorVariation = new ColorVariation
		{
			m_GroupID = orCreateSystemManaged.GetColorGroupID(null),
			m_SyncFlags = ColorSyncFlags.None,
			m_ColorSourceType = m_ExternalColorSource,
			m_Probability = 100
		};
		for (int i = 0; i < 3; i++)
		{
			if (CanBeModifiedByExternal((sbyte)i))
			{
				colorVariation.SetExternalChannelIndex(i, m_ChannelsBinding[i].m_ChannelId);
			}
			else
			{
				colorVariation.SetExternalChannelIndex(i, -1);
			}
		}
		int3 @int = math.clamp(m_VariationRanges, 0, 100);
		int3 alphas = math.clamp(m_AlphaRanges, 0, 100);
		colorVariation.m_HueRange = (byte)@int.x;
		colorVariation.m_SaturationRange = (byte)@int.y;
		colorVariation.m_ValueRange = (byte)@int.z;
		colorVariation.m_AlphaRange0 = (byte)GetAlpha(alphas, 0, 0);
		colorVariation.m_AlphaRange1 = (byte)GetAlpha(alphas, 1, 0);
		colorVariation.m_AlphaRange2 = (byte)GetAlpha(alphas, 2, 0);
		DynamicBuffer<ColorVariation> buffer = entityManager.GetBuffer<ColorVariation>(entity);
		buffer.ResizeUninitialized(m_ColorVariations.Count);
		int num = 0;
		bool flag = false;
		if (m_VariationGroups != null)
		{
			for (int j = 0; j < m_VariationGroups.Count; j++)
			{
				VariationGroup variationGroup = m_VariationGroups[j];
				flag |= string.IsNullOrEmpty(variationGroup.m_Name);
				ColorVariation colorVariation2 = colorVariation;
				colorVariation2.m_GroupID = orCreateSystemManaged.GetColorGroupID(variationGroup.m_Name);
				colorVariation2.m_SyncFlags = variationGroup.m_MeshSyncMode;
				colorVariation2.m_Probability = (byte)math.clamp(variationGroup.m_Probability, 0, 100);
				if (variationGroup.m_OverrideRandomness)
				{
					@int = math.clamp(variationGroup.m_VariationRanges, 0, 100);
					alphas = math.clamp(variationGroup.m_AlphaRanges, 0, 100);
					colorVariation2.m_HueRange = (byte)@int.x;
					colorVariation2.m_SaturationRange = (byte)@int.y;
					colorVariation2.m_ValueRange = (byte)@int.z;
					colorVariation2.m_AlphaRange0 = (byte)GetAlpha(alphas, 0, 0);
					colorVariation2.m_AlphaRange1 = (byte)GetAlpha(alphas, 1, 0);
					colorVariation2.m_AlphaRange2 = (byte)GetAlpha(alphas, 2, 0);
				}
				for (int k = 0; k < m_ColorVariations.Count; k++)
				{
					if (m_ColorVariations[k].m_VariationGroup == variationGroup.m_Name)
					{
						ColorVariation value = colorVariation2;
						for (int l = 0; l < 3; l++)
						{
							value.m_ColorSet[l] = GetColor(k, (sbyte)l);
						}
						buffer[num++] = value;
					}
				}
			}
		}
		if (flag)
		{
			return;
		}
		for (int m = 0; m < m_ColorVariations.Count; m++)
		{
			if (string.IsNullOrEmpty(m_ColorVariations[m].m_VariationGroup))
			{
				ColorVariation value2 = colorVariation;
				for (int n = 0; n < 3; n++)
				{
					value2.m_ColorSet[n] = GetColor(m, (sbyte)n);
				}
				buffer[num++] = value2;
			}
		}
	}
}
