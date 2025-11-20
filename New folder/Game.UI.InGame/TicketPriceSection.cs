using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Policies;
using Game.Prefabs;
using Game.Routes;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TicketPriceSection : InfoSectionBase
{
	private PoliciesUISystem m_PoliciesUISystem;

	private Entity m_TicketPricePolicy;

	private EntityQuery m_ConfigQuery;

	protected override string group => "TicketPriceSection";

	private UIPolicySlider sliderData { get; set; }

	protected override void Reset()
	{
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		AddBinding(new TriggerBinding<int>(group, "setTicketPrice", OnSetTicketPrice));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (!m_ConfigQuery.IsEmptyIgnoreFilter)
		{
			UITransportConfigurationPrefab singletonPrefab = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_ConfigQuery);
			m_TicketPricePolicy = m_PrefabSystem.GetEntity(singletonPrefab.m_TicketPricePolicy);
		}
	}

	private void OnSetTicketPrice(int newPrice)
	{
		m_PoliciesUISystem.SetPolicy(selectedEntity, m_TicketPricePolicy, newPrice > 0, Mathf.Clamp(newPrice, sliderData.range.min, sliderData.range.max));
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<TransportLine>(selectedEntity) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity) && base.EntityManager.HasComponent<Policy>(selectedEntity) && base.EntityManager.TryGetComponent<TransportLineData>(selectedPrefab, out var component))
		{
			return !component.m_CargoTransport;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		DynamicBuffer<Policy> buffer = base.EntityManager.GetBuffer<Policy>(selectedEntity, isReadOnly: true);
		PolicySliderData componentData = base.EntityManager.GetComponentData<PolicySliderData>(m_TicketPricePolicy);
		for (int i = 0; i < buffer.Length; i++)
		{
			if (!(buffer[i].m_Policy != m_TicketPricePolicy))
			{
				sliderData = new UIPolicySlider(((buffer[i].m_Flags & PolicyFlags.Active) != 0) ? buffer[i].m_Adjustment : 0f, componentData);
				return;
			}
		}
		sliderData = new UIPolicySlider(0f, componentData);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("sliderData");
		writer.Write(sliderData);
	}

	[Preserve]
	public TicketPriceSection()
	{
	}
}
