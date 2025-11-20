using System.Collections.Generic;
using Game.Rendering;
using Game.UI.Widgets;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

public class GuideLineTooltipSystem : TooltipSystemBase
{
	private GuideLinesSystem m_GuideLinesSystem;

	private List<TooltipGroup> m_Groups;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GuideLinesSystem = base.World.GetOrCreateSystemManaged<GuideLinesSystem>();
		m_Groups = new List<TooltipGroup>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		NativeList<GuideLinesSystem.TooltipInfo> tooltips = m_GuideLinesSystem.GetTooltips(out dependencies);
		dependencies.Complete();
		for (int i = 0; i < tooltips.Length; i++)
		{
			GuideLinesSystem.TooltipInfo tooltipInfo = tooltips[i];
			if (m_Groups.Count <= i)
			{
				m_Groups.Add(new TooltipGroup
				{
					path = $"guideLineTooltip{i}",
					horizontalAlignment = TooltipGroup.Alignment.Center,
					verticalAlignment = TooltipGroup.Alignment.Center,
					category = TooltipGroup.Category.Network,
					children = { (IWidget)new FloatTooltip() }
				});
			}
			TooltipGroup tooltipGroup = m_Groups[i];
			bool onScreen;
			float2 @float = TooltipSystemBase.WorldToTooltipPos(tooltipInfo.m_Position, out onScreen);
			if (!tooltipGroup.position.Equals(@float))
			{
				tooltipGroup.position = @float;
				tooltipGroup.SetChildrenChanged();
			}
			FloatTooltip floatTooltip = tooltipGroup.children[0] as FloatTooltip;
			switch (tooltipInfo.m_Type)
			{
			case GuideLinesSystem.TooltipType.Angle:
				floatTooltip.icon = "Media/Glyphs/Angle.svg";
				floatTooltip.value = tooltipInfo.m_Value;
				floatTooltip.unit = "angle";
				break;
			case GuideLinesSystem.TooltipType.Length:
				floatTooltip.icon = "Media/Glyphs/Length.svg";
				floatTooltip.value = tooltipInfo.m_Value;
				floatTooltip.unit = "length";
				break;
			}
			AddGroup(tooltipGroup);
		}
	}

	[Preserve]
	public GuideLineTooltipSystem()
	{
	}
}
