using System.Collections.Generic;
using Colossal.Annotations;
using Game.Reflection;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public interface IValueFieldPopup<TValue>
{
	IList<IWidget> children { get; }

	void Attach(ITypedValueAccessor<TValue> accessor);

	void Detach();

	bool Update();

	[NotNull]
	LocalizedString GetDisplayValue(TValue value);
}
