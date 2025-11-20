using Game.UI.Localization;

namespace Game.UI.Widgets;

public interface ITooltipTarget
{
	LocalizedString? tooltip { get; set; }
}
