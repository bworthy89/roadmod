using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DescriptionSection : InfoSectionBase
{
	private PrefabUISystem m_PrefabUISystem;

	private NativeList<LeisureProviderData> m_LeisureDatas;

	private NativeList<LocalModifierData> m_LocalModifierDatas;

	private NativeList<CityModifierData> m_CityModifierDatas;

	protected override string group => "DescriptionSection";

	private string localeId { get; set; }

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForUpgrades => true;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_LeisureDatas = new NativeList<LeisureProviderData>(10, Allocator.Persistent);
		m_LocalModifierDatas = new NativeList<LocalModifierData>(10, Allocator.Persistent);
		m_CityModifierDatas = new NativeList<CityModifierData>(10, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_LeisureDatas.Dispose();
		m_LocalModifierDatas.Dispose();
		m_CityModifierDatas.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		m_LeisureDatas.Clear();
		m_LocalModifierDatas.Clear();
		m_CityModifierDatas.Clear();
		localeId = null;
	}

	private bool Visible()
	{
		if ((base.EntityManager.HasComponent<Route>(selectedEntity) || (base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity)) || !base.EntityManager.HasComponent<ServiceObjectData>(selectedPrefab)) && !base.EntityManager.HasComponent<SignatureBuildingData>(selectedPrefab) && !base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity);
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		m_PrefabUISystem.GetTitleAndDescription(selectedPrefab, out var _, out var descriptionId);
		localeId = descriptionId;
		if (base.EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<LocalModifierData> buffer))
		{
			m_LocalModifierDatas.AddRange(buffer.AsNativeArray());
		}
		if (base.EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<CityModifierData> buffer2))
		{
			m_CityModifierDatas.AddRange(buffer2.AsNativeArray());
		}
		if (base.EntityManager.TryGetComponent<LeisureProviderData>(selectedPrefab, out var component) && component.m_Efficiency > 0)
		{
			m_LeisureDatas.Add(in component);
		}
		if (!base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer3))
		{
			return;
		}
		for (int i = 0; i < buffer3.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<PrefabRef>(buffer3[i].m_Upgrade, out var component2))
			{
				if (base.EntityManager.TryGetBuffer(component2.m_Prefab, isReadOnly: true, out DynamicBuffer<LocalModifierData> buffer4))
				{
					LocalEffectSystem.AddToTempList(m_LocalModifierDatas, buffer4, disabled: false);
				}
				if (base.EntityManager.TryGetBuffer(component2.m_Prefab, isReadOnly: true, out DynamicBuffer<CityModifierData> buffer5))
				{
					CityModifierUpdateSystem.AddToTempList(m_CityModifierDatas, buffer5);
				}
				if (base.EntityManager.TryGetComponent<LeisureProviderData>(component2.m_Prefab, out var component3) && component3.m_Efficiency > 0)
				{
					LeisureSystem.AddToTempList(m_LeisureDatas, component3);
				}
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("localeId");
		writer.Write(localeId);
		writer.PropertyName("effects");
		int num = 0;
		if (m_CityModifierDatas.Length > 0)
		{
			num++;
		}
		if (m_LocalModifierDatas.Length > 0)
		{
			num++;
		}
		if (m_LeisureDatas.Length > 0)
		{
			num++;
		}
		writer.ArrayBegin(num);
		if (m_CityModifierDatas.Length > 0)
		{
			PrefabUISystem.CityModifierBinder.Bind(writer, m_CityModifierDatas);
		}
		if (m_LocalModifierDatas.Length > 0)
		{
			PrefabUISystem.LocalModifierBinder.Bind(writer, m_LocalModifierDatas);
		}
		if (m_LeisureDatas.Length > 0)
		{
			PrefabUISystem.LeisureProviderBinder.Bind(writer, m_LeisureDatas);
		}
		writer.ArrayEnd();
	}

	[Preserve]
	public DescriptionSection()
	{
	}
}
