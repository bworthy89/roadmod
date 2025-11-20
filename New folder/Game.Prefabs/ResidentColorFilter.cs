using System;
using System.Collections.Generic;
using Game.Rendering;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Rendering/", new Type[]
{
	typeof(RenderPrefab),
	typeof(CharacterOverlay)
})]
public class ResidentColorFilter : ComponentBase
{
	[Serializable]
	public class ColorFilter
	{
		public AgeMask m_AgeFilter;

		public GenderMask m_GenderFilter;

		[ElementCustomField(typeof(ColorFilterVariationGroupField))]
		public string[] m_VariationGroups;

		public int m_OverrideProbability = -1;

		public float3 m_OverrideAlpha = -1f;
	}

	public ColorFilter[] m_ColorFilters;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Prefabs.ColorFilter>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		MeshColorSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<MeshColorSystem>();
		DynamicBuffer<Game.Prefabs.ColorFilter> buffer = entityManager.GetBuffer<Game.Prefabs.ColorFilter>(entity);
		int num = 0;
		for (int i = 0; i < m_ColorFilters.Length; i++)
		{
			num += m_ColorFilters[i].m_VariationGroups.Length;
		}
		buffer.ResizeUninitialized(num);
		num = 0;
		ColorProperties component = GetComponent<ColorProperties>();
		for (int j = 0; j < m_ColorFilters.Length; j++)
		{
			ColorFilter colorFilter = m_ColorFilters[j];
			Game.Prefabs.ColorFilter value = new Game.Prefabs.ColorFilter
			{
				m_AgeFilter = colorFilter.m_AgeFilter,
				m_GenderFilter = colorFilter.m_GenderFilter,
				m_OverrideProbability = (sbyte)math.clamp(colorFilter.m_OverrideProbability, -1, 100),
				m_OverrideAlpha = -1f
			};
			if (component != null)
			{
				float3 alphas = math.select(math.saturate(colorFilter.m_OverrideAlpha), -1f, colorFilter.m_OverrideAlpha < 0f);
				value.m_OverrideAlpha.x = component.GetAlpha(alphas, 0, -1f);
				value.m_OverrideAlpha.y = component.GetAlpha(alphas, 1, -1f);
				value.m_OverrideAlpha.z = component.GetAlpha(alphas, 2, -1f);
			}
			for (int k = 0; k < colorFilter.m_VariationGroups.Length; k++)
			{
				value.m_GroupID = orCreateSystemManaged.GetColorGroupID(colorFilter.m_VariationGroups[k]);
				buffer[num++] = value;
			}
		}
	}
}
