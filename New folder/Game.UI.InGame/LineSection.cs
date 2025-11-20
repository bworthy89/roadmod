using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Routes;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LineSection : InfoSectionBase
{
	protected override string group => "LineSection";

	private float length { get; set; }

	private int stops { get; set; }

	private int cargo { get; set; }

	private float usage { get; set; }

	protected override bool displayForUpgrades => true;

	protected override void Reset()
	{
		length = 0f;
		stops = 0;
		cargo = 0;
		usage = 0f;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity) && (base.EntityManager.HasComponent<TransportLine>(selectedEntity) || base.EntityManager.HasComponent<WorkRoute>(selectedEntity));
	}

	protected override void OnProcess()
	{
		int num = 0;
		int capacity = 0;
		TransportUIUtils.GetRouteVehiclesCount(base.EntityManager, selectedEntity, ref num, ref capacity);
		usage = ((capacity > 0) ? ((float)num / (float)capacity) : 0f);
		stops = TransportUIUtils.GetStopCount(base.EntityManager, selectedEntity);
		length = TransportUIUtils.GetRouteLength(base.EntityManager, selectedEntity);
		cargo = num;
		base.tooltipTags.Add("CargoRoute");
		base.tooltipTags.Add("TransportLine");
		base.tooltipTags.Add("WorkRoute");
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("length");
		writer.Write(length);
		writer.PropertyName("stops");
		writer.Write(stops);
		writer.PropertyName("usage");
		writer.Write(usage);
		writer.PropertyName("cargo");
		writer.Write(cargo);
	}

	[Preserve]
	public LineSection()
	{
	}
}
