using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class ContentPrerequisiteSection : InfoSectionBase
{
	private string contentPrefab { get; set; }

	protected override bool displayForUpgrades => true;

	protected override bool displayForUnderConstruction => true;

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override string group => "ContentPrerequisiteSection";

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.TryGetComponent<ContentPrerequisiteData>(selectedPrefab, out var component) && !base.EntityManager.HasEnabledComponent<PrefabData>(component.m_ContentPrerequisite);
	}

	protected override void Reset()
	{
		contentPrefab = string.Empty;
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetComponent<ContentPrerequisiteData>(selectedPrefab, out var component))
		{
			contentPrefab = m_PrefabSystem.GetPrefabName(component.m_ContentPrerequisite);
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("contentPrefab");
		writer.Write(contentPrefab);
	}

	[Preserve]
	public ContentPrerequisiteSection()
	{
	}
}
