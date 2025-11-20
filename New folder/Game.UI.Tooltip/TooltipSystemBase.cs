using System.Linq;
using Game.UI.Widgets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

public abstract class TooltipSystemBase : GameSystemBase
{
	private TooltipUISystem m_TooltipUISystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TooltipUISystem = base.World.GetOrCreateSystemManaged<TooltipUISystem>();
	}

	protected void AddGroup(TooltipGroup group)
	{
		if (group.path != PathSegment.Empty && m_TooltipUISystem.groups.Any((TooltipGroup g) => g.path == group.path))
		{
			UnityEngine.Debug.LogError($"Trying to add tooltip group with duplicate path '{group.path}'");
		}
		else
		{
			m_TooltipUISystem.groups.Add(group);
		}
	}

	protected void AddMouseTooltip(IWidget tooltip)
	{
		if (tooltip.path != PathSegment.Empty && m_TooltipUISystem.mouseGroup.children.Any((IWidget t) => t.path == tooltip.path))
		{
			UnityEngine.Debug.LogError($"Trying to add mouse tooltip with duplicate path '{tooltip.path}'");
		}
		else
		{
			m_TooltipUISystem.mouseGroup.children.Add(tooltip);
		}
	}

	protected static float2 WorldToTooltipPos(Vector3 worldPos, out bool onScreen)
	{
		float2 xy = ((float3)Camera.main.WorldToScreenPoint(worldPos)).xy;
		xy.y = (float)Screen.height - xy.y;
		onScreen = xy.x >= 0f && xy.y >= 0f && xy.x <= (float)Screen.width && xy.y <= (float)Screen.height;
		return xy;
	}

	[Preserve]
	protected TooltipSystemBase()
	{
	}
}
