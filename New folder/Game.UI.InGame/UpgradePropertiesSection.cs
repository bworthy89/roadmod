using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class UpgradePropertiesSection : InfoSectionBase
{
	private enum UpgradeType
	{
		SubBuilding,
		Extension
	}

	private static readonly string kMainBuildingName = "mainBuildingName";

	protected override string group => "UpgradePropertiesSection";

	protected override bool displayForUpgrades => true;

	private Entity mainBuilding { get; set; }

	private Entity upgrade { get; set; }

	private UpgradeType type { get; set; }

	protected override void Reset()
	{
		mainBuilding = Entity.Null;
		upgrade = Entity.Null;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<ServiceUpgradeData>(selectedPrefab);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		upgrade = selectedPrefab;
		if (base.EntityManager.TryGetComponent<Owner>(selectedEntity, out var component))
		{
			mainBuilding = component.m_Owner;
			if (base.EntityManager.TryGetComponent<Attachment>(mainBuilding, out var component2) && component2.m_Attached != Entity.Null)
			{
				mainBuilding = component2.m_Attached;
			}
		}
		type = (base.EntityManager.HasComponent<BuildingExtensionData>(selectedPrefab) ? UpgradeType.Extension : UpgradeType.SubBuilding);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("mainBuilding");
		writer.Write(mainBuilding);
		writer.PropertyName(kMainBuildingName);
		m_NameSystem.BindName(writer, mainBuilding);
		writer.PropertyName("upgrade");
		writer.Write(upgrade);
		writer.PropertyName("type");
		writer.Write(type.ToString());
	}

	[Preserve]
	public UpgradePropertiesSection()
	{
	}
}
