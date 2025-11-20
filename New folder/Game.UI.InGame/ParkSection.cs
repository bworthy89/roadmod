using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ParkSection : InfoSectionBase
{
	protected override string group => "ParkSection";

	private int maintenance { get; set; }

	protected override void Reset()
	{
		maintenance = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.Park>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<ParkData>(selectedEntity, selectedPrefab, out var data))
		{
			maintenance = Mathf.CeilToInt(math.select((float)base.EntityManager.GetComponentData<Game.Buildings.Park>(selectedEntity).m_Maintenance / (float)data.m_MaintenancePool, 0f, data.m_MaintenancePool == 0) * 100f);
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("maintenance");
		writer.Write(maintenance);
	}

	[Preserve]
	public ParkSection()
	{
	}
}
