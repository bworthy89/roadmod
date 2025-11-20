using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Objects;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ResourceSection : InfoSectionBase
{
	private enum ResourceKey
	{
		Wood
	}

	protected override string group => "ResourceSection";

	private float resourceAmount { get; set; }

	private ResourceKey resourceKey { get; set; }

	protected override bool displayForDestroyedObjects => true;

	protected override void Reset()
	{
		resourceAmount = 0f;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.HasComponent<Tree>(selectedEntity) && base.EntityManager.TryGetComponent<TreeData>(selectedPrefab, out var component) && component.m_WoodAmount > 0f;
	}

	protected override void OnProcess()
	{
		Tree componentData = base.EntityManager.GetComponentData<Tree>(selectedEntity);
		Plant componentData2 = base.EntityManager.GetComponentData<Plant>(selectedEntity);
		TreeData componentData3 = base.EntityManager.GetComponentData<TreeData>(selectedPrefab);
		base.EntityManager.TryGetComponent<Damaged>(selectedEntity, out var component);
		resourceAmount = math.round(ObjectUtils.CalculateWoodAmount(componentData, componentData2, component, componentData3));
		resourceKey = ResourceKey.Wood;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("resourceAmount");
		writer.Write(resourceAmount);
		writer.PropertyName("resourceKey");
		writer.Write(Enum.GetName(typeof(ResourceKey), resourceKey));
	}

	[Preserve]
	public ResourceSection()
	{
	}
}
