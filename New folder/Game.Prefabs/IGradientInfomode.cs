using Game.UI.Localization;
using UnityEngine;

namespace Game.Prefabs;

public interface IGradientInfomode
{
	GradientLegendType legendType { get; }

	LocalizedString? lowLabel { get; }

	LocalizedString? mediumLabel { get; }

	LocalizedString? highLabel { get; }

	Color lowColor { get; }

	Color mediumColor { get; }

	Color highColor { get; }
}
