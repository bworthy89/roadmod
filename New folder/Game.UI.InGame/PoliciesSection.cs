using System;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Policies;
using Game.Routes;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PoliciesSection : InfoSectionBase
{
	private enum PoliciesKey
	{
		Building,
		District
	}

	private PoliciesUISystem m_PoliciesUISystem;

	protected override string group => "PoliciesSection";

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PoliciesUISystem = base.World.GetOrCreateSystemManaged<PoliciesUISystem>();
		PoliciesUISystem policiesUISystem = m_PoliciesUISystem;
		policiesUISystem.EventPolicyUnlocked = (Action)Delegate.Combine(policiesUISystem.EventPolicyUnlocked, new Action(m_InfoUISystem.RequestUpdate));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		PoliciesUISystem policiesUISystem = m_PoliciesUISystem;
		policiesUISystem.EventPolicyUnlocked = (Action)Delegate.Remove(policiesUISystem.EventPolicyUnlocked, new Action(m_InfoUISystem.RequestUpdate));
	}

	protected override void Reset()
	{
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.HasComponent<Policy>(selectedEntity) && m_PoliciesUISystem.GatherSelectedInfoPolicies(selectedEntity);
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.HasComponent<Building>(selectedEntity))
		{
			base.tooltipKeys.Add(PoliciesKey.Building.ToString());
		}
		else if (base.EntityManager.HasComponent<District>(selectedEntity))
		{
			base.tooltipKeys.Add(PoliciesKey.District.ToString());
		}
		else if (base.EntityManager.HasComponent<Route>(selectedEntity))
		{
			base.tooltipTags.Add(SelectedInfoTags.CargoRoute.ToString());
			base.tooltipTags.Add(SelectedInfoTags.TransportLine.ToString());
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("policies");
		if (base.EntityManager.HasComponent<Building>(selectedEntity))
		{
			m_PoliciesUISystem.BindBuildingPolicies(writer);
		}
		else if (base.EntityManager.HasComponent<District>(selectedEntity))
		{
			m_PoliciesUISystem.BindDistrictPolicies(writer);
		}
		else if (base.EntityManager.HasComponent<Route>(selectedEntity))
		{
			m_PoliciesUISystem.BindRoutePolicies(writer);
		}
		else
		{
			writer.WriteNull();
		}
	}

	[Preserve]
	public PoliciesSection()
	{
	}
}
