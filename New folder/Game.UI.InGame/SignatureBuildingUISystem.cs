using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Prefabs;
using Game.Serialization;
using Game.Settings;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class SignatureBuildingUISystem : UISystemBase, IPreDeserialize
{
	private const string kGroup = "signatureBuildings";

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_UnlockedSignatureBuildingQuery;

	private ValueBinding<List<Entity>> m_UnlockSignaturesBinding;

	private bool m_SkipUpdate = true;

	private int m_LastListCount;

	private bool m_NeedTriggerUpdate;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_UnlockedSignatureBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		AddBinding(m_UnlockSignaturesBinding = new ValueBinding<List<Entity>>("signatureBuildings", "unlockedSignatures", new List<Entity>(), new ListWriter<Entity>()));
		AddBinding(new TriggerBinding("signatureBuildings", "removeUnlockedSignature", RemoveUnlockedSignature));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_SkipUpdate)
		{
			m_SkipUpdate = false;
		}
		else
		{
			if (!SharedSettings.instance.userInterface.blockingPopupsEnabled || m_CityConfigurationSystem.unlockAll)
			{
				return;
			}
			if (!m_UnlockedSignatureBuildingQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Unlock> nativeArray = m_UnlockedSignatureBuildingQuery.ToComponentDataArray<Unlock>(Allocator.TempJob);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					if (base.EntityManager.HasComponent<SignatureBuildingData>(nativeArray[i].m_Prefab) && base.EntityManager.HasComponent<UIObjectData>(nativeArray[i].m_Prefab))
					{
						AddUnlockedSignature(nativeArray[i].m_Prefab);
					}
				}
				nativeArray.Dispose();
			}
			if (m_UnlockSignaturesBinding.value.Count != m_LastListCount || m_NeedTriggerUpdate)
			{
				m_UnlockSignaturesBinding.TriggerUpdate();
				m_LastListCount = m_UnlockSignaturesBinding.value.Count;
				m_NeedTriggerUpdate = false;
			}
		}
	}

	public void AddUnlockedSignature(Entity prefab)
	{
		if (!m_UnlockSignaturesBinding.value.Contains(prefab))
		{
			m_UnlockSignaturesBinding.value.Insert(0, prefab);
			m_NeedTriggerUpdate = true;
		}
	}

	private void RemoveUnlockedSignature()
	{
		if (m_UnlockSignaturesBinding.value.Count != 0)
		{
			m_UnlockSignaturesBinding.value.RemoveAt(0);
		}
		m_NeedTriggerUpdate = true;
	}

	public void ClearUnlockedSignature()
	{
		m_UnlockSignaturesBinding.value.Clear();
		m_NeedTriggerUpdate = true;
	}

	public void PreDeserialize(Context context)
	{
		m_UnlockSignaturesBinding.value.Clear();
		m_SkipUpdate = false;
	}

	public void SkipUpdate()
	{
		m_SkipUpdate = true;
	}

	[Preserve]
	public SignatureBuildingUISystem()
	{
	}
}
