using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class RaycastNotificationTooltipSystem : TooltipSystemBase
{
	private ToolSystem m_ToolSystem;

	private DefaultToolSystem m_DefaultTool;

	private PrefabSystem m_PrefabSystem;

	private NameSystem m_NameSystem;

	private ImageSystem m_ImageSystem;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private EntityQuery m_ConfigurationQuery;

	private NotificationTooltip m_Tooltip;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_DefaultTool = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<IconConfigurationData>());
		RequireForUpdate(m_ConfigurationQuery);
		m_Tooltip = new NotificationTooltip
		{
			path = "raycastNotification",
			verbose = true
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool == m_DefaultTool && m_ToolRaycastSystem.GetRaycastResult(out var result) && base.EntityManager.TryGetComponent<Icon>(result.m_Owner, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(result.m_Owner, out var component2))
		{
			IconConfigurationData singleton = m_ConfigurationQuery.GetSingleton<IconConfigurationData>();
			if (!(component2.m_Prefab == singleton.m_SelectedMarker) && !(component2.m_Prefab == singleton.m_FollowedMarker))
			{
				m_Tooltip.name = (m_PrefabSystem.TryGetPrefab<NotificationIconPrefab>(component2, out var prefab) ? prefab.name : m_PrefabSystem.GetObsoleteID(component2.m_Prefab).GetName());
				m_Tooltip.color = NotificationTooltip.GetColor(component.m_Priority);
				AddMouseTooltip(m_Tooltip);
			}
		}
	}

	[Preserve]
	public RaycastNotificationTooltipSystem()
	{
	}
}
