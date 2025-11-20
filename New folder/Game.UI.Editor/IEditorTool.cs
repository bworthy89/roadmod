using Colossal.UI.Binding;

namespace Game.UI.Editor;

public interface IEditorTool : IJsonWritable
{
	string id { get; }

	string icon { get; }

	string uiTag { get; }

	bool disabled { get; }

	string shortcut { get; }

	bool active { get; set; }

	void IJsonWritable.Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(IEditorTool).FullName);
		writer.PropertyName("id");
		writer.Write(id);
		writer.PropertyName("icon");
		writer.Write(icon);
		writer.PropertyName("uiTag");
		writer.Write(uiTag);
		writer.PropertyName("shortcut");
		writer.Write(shortcut);
		writer.PropertyName("disabled");
		writer.Write(disabled);
		writer.TypeEnd();
	}
}
