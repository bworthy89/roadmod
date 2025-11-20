using System.Runtime.CompilerServices;
using Game.Prefabs;
using Game.Reflection;
using Game.Simulation;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

[CompilerGenerated]
public class DiversityPanelSystem : EditorPanelSystemBase
{
	private PrefabSystem m_PrefabSystem;

	private DiversitySystem m_DiversitySystem;

	private EntityQuery m_AtmosphereQuery;

	private EntityQuery m_BiomeQuery;

	private AtmospherePrefab m_Atmosphere;

	private BiomePrefab m_Biome;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_DiversitySystem = base.World.GetOrCreateSystemManaged<DiversitySystem>();
		m_AtmosphereQuery = GetEntityQuery(ComponentType.ReadOnly<AtmosphereData>());
		m_BiomeQuery = GetEntityQuery(ComponentType.ReadOnly<BiomeData>());
		title = LocalizedString.Value("Diversity");
		children = new IWidget[1] { Scrollable.WithChildren(new IWidget[1]
		{
			new EditorSection
			{
				displayName = "Diversity Settings",
				expanded = true,
				children = new IWidget[2]
				{
					new PopupValueField<PrefabBase>
					{
						displayName = "Atmosphere",
						accessor = new DelegateAccessor<PrefabBase>(() => m_Atmosphere, SetAtmosphere),
						popup = new PrefabPickerPopup(typeof(AtmospherePrefab))
					},
					new PopupValueField<PrefabBase>
					{
						displayName = "Biome",
						accessor = new DelegateAccessor<PrefabBase>(() => m_Biome, SetBiome),
						popup = new PrefabPickerPopup(typeof(BiomePrefab))
					}
				}
			}
		}) };
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		AtmosphereData singleton = m_AtmosphereQuery.GetSingleton<AtmosphereData>();
		m_PrefabSystem.TryGetPrefab<AtmospherePrefab>(singleton.m_AtmospherePrefab, out m_Atmosphere);
		BiomeData singleton2 = m_BiomeQuery.GetSingleton<BiomeData>();
		m_PrefabSystem.TryGetPrefab<BiomePrefab>(singleton2.m_BiomePrefab, out m_Biome);
	}

	private void SetAtmosphere(PrefabBase prefab)
	{
		m_Atmosphere = (AtmospherePrefab)prefab;
		Entity entity = m_PrefabSystem.GetEntity(m_Atmosphere);
		m_DiversitySystem.ApplyAtmospherePreset(entity);
	}

	private void SetBiome(PrefabBase prefab)
	{
		m_Biome = (BiomePrefab)prefab;
		Entity entity = m_PrefabSystem.GetEntity(m_Biome);
		m_DiversitySystem.ApplyBiomePreset(entity);
	}

	[Preserve]
	public DiversityPanelSystem()
	{
	}
}
