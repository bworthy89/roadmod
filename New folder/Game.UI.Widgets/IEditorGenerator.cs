using Colossal.Annotations;
using Game.Reflection;

namespace Game.UI.Widgets;

public interface IEditorGenerator
{
	IWidget Build([NotNull] IValueAccessor accessor, [NotNull] object[] attributes, int level, string path);
}
