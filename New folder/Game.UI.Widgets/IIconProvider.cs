using System;

namespace Game.UI.Widgets;

public interface IIconProvider
{
	Func<string> iconSrc { get; set; }
}
