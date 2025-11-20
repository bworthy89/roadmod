using Game.UI.Localization;

namespace Game.UI.Widgets;

public interface INamed
{
	LocalizedString displayName { get; set; }

	LocalizedString description { get; set; }
}
