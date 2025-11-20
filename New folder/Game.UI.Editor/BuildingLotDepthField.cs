using System;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class BuildingLotDepthField : BuildingLotFieldBase
{
	public override FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return TryCreate(memberType, attributes, horizontal: false);
	}
}
