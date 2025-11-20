using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Routes;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ComfortSection : InfoSectionBase
{
	protected override string group => "ComfortSection";

	private int comfort { get; set; }

	protected override void Reset()
	{
		comfort = 0;
	}

	private bool Visible()
	{
		if ((base.EntityManager.HasComponent<MailBox>(selectedEntity) || base.EntityManager.HasComponent<WorkStop>(selectedEntity) || !base.EntityManager.HasComponent<TransportStop>(selectedEntity)) && (!base.EntityManager.HasComponent<TransportStation>(selectedEntity) || !base.EntityManager.HasComponent<PublicTransportStation>(selectedEntity)))
		{
			return base.EntityManager.HasComponent<ParkingFacility>(selectedEntity);
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
		float num = 0f;
		TransportStation component2;
		ParkingFacility component3;
		if (base.EntityManager.TryGetComponent<TransportStop>(selectedEntity, out var component))
		{
			num = component.m_ComfortFactor;
			if (base.EntityManager.HasComponent<ParkingSpace>(selectedEntity))
			{
				base.tooltipKeys.Add("Parking");
			}
			else
			{
				base.tooltipKeys.Add("TransportStop");
			}
		}
		else if (base.EntityManager.TryGetComponent<TransportStation>(selectedEntity, out component2))
		{
			num = component2.m_ComfortFactor;
			base.tooltipKeys.Add("TransportStation");
		}
		else if (base.EntityManager.TryGetComponent<ParkingFacility>(selectedEntity, out component3))
		{
			num = component3.m_ComfortFactor;
			base.tooltipKeys.Add("Parking");
		}
		comfort = (int)math.round(100f * num);
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("comfort");
		writer.Write(comfort);
	}

	[Preserve]
	public ComfortSection()
	{
	}
}
