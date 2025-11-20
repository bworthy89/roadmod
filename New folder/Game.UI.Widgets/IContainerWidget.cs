using System.Collections.Generic;

namespace Game.UI.Widgets;

public interface IContainerWidget
{
	IList<IWidget> children { get; }
}
