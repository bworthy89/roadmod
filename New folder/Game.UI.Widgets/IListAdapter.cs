using System.Collections.Generic;

namespace Game.UI.Widgets;

public interface IListAdapter
{
	int length { get; }

	bool resizable { get; }

	bool sortable { get; }

	bool UpdateRange(int startIndex, int endIndex);

	IEnumerable<IWidget> BuildElementsInRange();

	int AddElement();

	void InsertElement(int index);

	int DuplicateElement(int index);

	void MoveElement(int fromIndex, int toIndex);

	void DeleteElement(int index);

	void Clear();
}
