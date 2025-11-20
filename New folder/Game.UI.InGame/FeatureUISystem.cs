using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class FeatureUISystem : UISystemBase
{
	private const string kGroup = "feature";

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private EntityQuery m_UnlockedFeatureQuery;

	private EntityQuery m_UnlocksQuery;

	private RawValueBinding m_FeaturesBinding;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_UnlockedFeatureQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<FeatureData>(), ComponentType.ReadOnly<Locked>());
		m_UnlocksQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		AddBinding(m_FeaturesBinding = new RawValueBinding("feature", "lockedFeatures", BindLockedFeatures));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (PrefabUtils.HasUnlockedPrefab<FeatureData>(base.EntityManager, m_UnlocksQuery))
		{
			m_FeaturesBinding.Update();
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_FeaturesBinding.Update();
	}

	private void BindLockedFeatures(IJsonWriter writer)
	{
		NativeArray<Entity> nativeArray = m_UnlockedFeatureQuery.ToEntityArray(Allocator.Temp);
		NativeArray<PrefabData> nativeArray2 = m_UnlockedFeatureQuery.ToComponentDataArray<PrefabData>(Allocator.Temp);
		writer.ArrayBegin(nativeArray2.Length);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity prefabEntity = nativeArray[i];
			PrefabData prefabData = nativeArray2[i];
			FeaturePrefab prefab = m_PrefabSystem.GetPrefab<FeaturePrefab>(prefabData);
			writer.TypeBegin("Game.UI.InGame.LockedFeature");
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("requirements");
			m_PrefabUISystem.BindPrefabRequirements(writer, prefabEntity);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public FeatureUISystem()
	{
	}
}
