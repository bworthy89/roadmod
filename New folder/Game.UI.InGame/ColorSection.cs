using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Routes;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ColorSection : InfoSectionBase
{
	private EntityArchetype m_ColorUpdateArchetype;

	protected override string group => "ColorSection";

	private Color32 color { get; set; }

	protected override bool displayForUpgrades => true;

	protected override void Reset()
	{
		color = default(Color32);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(new TriggerBinding<UnityEngine.Color>(group, "setColor", OnSetColor));
		m_ColorUpdateArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ColorUpdated>());
	}

	private void OnSetColor(UnityEngine.Color uiColor)
	{
		if (!base.EntityManager.HasComponent<Route>(selectedEntity) || !base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity) || !base.EntityManager.HasComponent<Game.Routes.Color>(selectedEntity) || (!base.EntityManager.HasComponent<TransportLine>(selectedEntity) && !base.EntityManager.HasComponent<WorkRoute>(selectedEntity)))
		{
			return;
		}
		EntityCommandBuffer entityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer();
		entityCommandBuffer.SetComponent(selectedEntity, new Game.Routes.Color(uiColor));
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<RouteVehicle> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				entityCommandBuffer.AddComponent(buffer[i].m_Vehicle, new Game.Routes.Color(uiColor));
			}
		}
		Entity e = entityCommandBuffer.CreateEntity(m_ColorUpdateArchetype);
		entityCommandBuffer.SetComponent(e, new ColorUpdated(selectedEntity));
		m_InfoUISystem.RequestUpdate();
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Route>(selectedEntity) && base.EntityManager.HasComponent<RouteWaypoint>(selectedEntity) && base.EntityManager.HasComponent<Game.Routes.Color>(selectedEntity))
		{
			if (!base.EntityManager.HasComponent<TransportLine>(selectedEntity))
			{
				return base.EntityManager.HasComponent<WorkRoute>(selectedEntity);
			}
			return true;
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
		color = base.EntityManager.GetComponentData<Game.Routes.Color>(selectedEntity).m_Color;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("color");
		writer.Write(color);
	}

	[Preserve]
	public ColorSection()
	{
	}
}
