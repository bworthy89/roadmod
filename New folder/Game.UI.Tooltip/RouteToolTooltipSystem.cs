using Game.Common;
using Game.Routes;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

public class RouteToolTooltipSystem : TooltipSystemBase
{
	private ToolSystem m_ToolSystem;

	private RouteToolSystem m_RouteTool;

	private ImageSystem m_ImageSystem;

	private NameSystem m_NameSystem;

	private EntityQuery m_TempRouteQuery;

	private EntityQuery m_TempStopQuery;

	private NameTooltip m_StopName;

	private NameTooltip m_RouteName;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_RouteTool = base.World.GetOrCreateSystemManaged<RouteToolSystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_TempRouteQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Route>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_TempStopQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<TransportStop>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_StopName = new NameTooltip
		{
			path = "routeToolStopName",
			nameBinder = m_NameSystem
		};
		m_RouteName = new NameTooltip
		{
			path = "routeToolRouteName",
			nameBinder = m_NameSystem
		};
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool == m_RouteTool && m_RouteTool.tooltip != RouteToolSystem.Tooltip.None)
		{
			switch (m_RouteTool.tooltip)
			{
			case RouteToolSystem.Tooltip.CreateRoute:
			case RouteToolSystem.Tooltip.AddWaypoint:
			case RouteToolSystem.Tooltip.CompleteRoute:
				TryAddStopName();
				break;
			case RouteToolSystem.Tooltip.CreateOrModify:
				TryAddStopName();
				TryAddRouteName();
				break;
			case RouteToolSystem.Tooltip.InsertWaypoint:
			case RouteToolSystem.Tooltip.MoveWaypoint:
			case RouteToolSystem.Tooltip.MergeWaypoints:
			case RouteToolSystem.Tooltip.RemoveWaypoint:
				TryAddStopName();
				TryAddRouteName();
				break;
			default:
				TryAddRouteName();
				break;
			}
		}
	}

	public void TryAddStopName()
	{
		if (m_TempStopQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Temp> nativeArray = m_TempStopQuery.ToComponentDataArray<Temp>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Temp temp = nativeArray[i];
				if (temp.m_Original != Entity.Null)
				{
					AddMouseTooltip(m_StopName);
					m_StopName.icon = m_ImageSystem.GetInstanceIcon(temp.m_Original);
					m_StopName.entity = temp.m_Original;
					break;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	public void TryAddRouteName()
	{
		if (m_TempRouteQuery.IsEmptyIgnoreFilter)
		{
			return;
		}
		NativeArray<Temp> nativeArray = m_TempRouteQuery.ToComponentDataArray<Temp>(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Temp temp = nativeArray[i];
				if (temp.m_Original != Entity.Null)
				{
					AddMouseTooltip(m_RouteName);
					m_RouteName.icon = m_ImageSystem.GetInstanceIcon(temp.m_Original);
					m_RouteName.entity = temp.m_Original;
					break;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	[Preserve]
	public RouteToolTooltipSystem()
	{
	}
}
