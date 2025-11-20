using System;
using Colossal.Annotations;

namespace Game.UI.Widgets;

public interface IFieldBuilderFactory
{
	[CanBeNull]
	FieldBuilder TryCreate([NotNull] Type memberType, [NotNull] object[] attributes);
}
