using System;

namespace Game.UI.Widgets;

public interface IDisableCallback
{
	Func<bool> disabled { get; set; }
}
