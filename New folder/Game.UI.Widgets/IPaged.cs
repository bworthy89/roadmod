namespace Game.UI.Widgets;

public interface IPaged
{
	int pageCount { get; }

	int currentPageIndex { get; set; }
}
