using System;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Editor;

namespace Game.UI.Widgets;

public class PopupValueFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (typeof(PrefabBase).IsAssignableFrom(memberType))
		{
			return delegate(IValueAccessor accessor)
			{
				CastAccessor<PrefabBase> accessor2 = new CastAccessor<PrefabBase>(accessor);
				return new PopupValueField<PrefabBase>
				{
					accessor = accessor2,
					popup = new PrefabPickerPopup(memberType)
				};
			};
		}
		return null;
	}
}
