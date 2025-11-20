using System;
using System.Collections.Generic;
using Game.Prefabs.Climate;
using Game.Rendering;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[] { typeof(RenderPrefab) })]
public class SeasonColorFilter : ComponentBase
{
	[Serializable]
	public class ColorFilter
	{
		public SeasonPrefab m_SeasonFilter;

		[ElementCustomField(typeof(ColorFilterVariationGroupField))]
		public string[] m_VariationGroups;

		public int m_OverrideProbability = -1;
	}

	public enum SeasonBlendMode
	{
		None = -1,
		Colors,
		Probability
	}

	public SeasonBlendMode m_SeasonBlendMode;

	public ColorFilter[] m_ColorFilters;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_ColorFilters.Length; i++)
		{
			SeasonPrefab seasonFilter = m_ColorFilters[i].m_SeasonFilter;
			if (seasonFilter != null)
			{
				prefabs.Add(seasonFilter);
			}
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Prefabs.ColorFilter>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		MeshColorSystem orCreateSystemManaged2 = entityManager.World.GetOrCreateSystemManaged<MeshColorSystem>();
		DynamicBuffer<Game.Prefabs.ColorFilter> buffer = entityManager.GetBuffer<Game.Prefabs.ColorFilter>(entity);
		int num = 0;
		for (int i = 0; i < m_ColorFilters.Length; i++)
		{
			num += m_ColorFilters[i].m_VariationGroups.Length;
		}
		buffer.ResizeUninitialized(num);
		num = 0;
		ColorFilterFlags colorFilterFlags = ColorFilterFlags.SeasonFilter;
		switch (m_SeasonBlendMode)
		{
		case SeasonBlendMode.Colors:
			colorFilterFlags |= ColorFilterFlags.BlendColor;
			break;
		case SeasonBlendMode.Probability:
			colorFilterFlags |= ColorFilterFlags.BlendProbability;
			break;
		}
		for (int j = 0; j < m_ColorFilters.Length; j++)
		{
			ColorFilter colorFilter = m_ColorFilters[j];
			Game.Prefabs.ColorFilter value = new Game.Prefabs.ColorFilter
			{
				m_AgeFilter = AgeMask.Any,
				m_GenderFilter = GenderMask.Any,
				m_OverrideProbability = (sbyte)math.clamp(colorFilter.m_OverrideProbability, -1, 100),
				m_OverrideAlpha = -1f
			};
			if (colorFilter.m_SeasonFilter != null)
			{
				value.m_EntityFilter = orCreateSystemManaged.GetEntity(colorFilter.m_SeasonFilter);
				value.m_Flags |= colorFilterFlags;
			}
			if (value.m_OverrideProbability < 0)
			{
				value.m_Flags &= ~ColorFilterFlags.BlendProbability;
			}
			for (int k = 0; k < colorFilter.m_VariationGroups.Length; k++)
			{
				value.m_GroupID = orCreateSystemManaged2.GetColorGroupID(colorFilter.m_VariationGroups[k]);
				buffer[num++] = value;
			}
		}
	}
}
