using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface IListWidget : IWidget, IJsonWritable
{
	int AddElement();

	void InsertElement(int index);

	int DuplicateElement(int index);

	void MoveElement(int fromIndex, int toIndex);

	void DeleteElement(int index);

	void Clear();
}
