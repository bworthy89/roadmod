using System;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.UI;
using Game.Reflection;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Editor;

public class UIIconField : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return delegate(IValueAccessor accessor)
		{
			CastAccessor<string> castAccessor = new CastAccessor<string>(accessor);
			StringInputField stringInputField = new StringInputField
			{
				displayName = "URI",
				accessor = castAccessor
			};
			IconButton iconPicker = new IconButton
			{
				icon = ((accessor.GetValue() as string) ?? string.Empty)
			};
			iconPicker.action = delegate
			{
				World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<InspectorPanelSystem>().ShowThumbnailPicker(delegate(Colossal.Hash128 hash)
				{
					string text = string.Empty;
					if (AssetDatabase.global.TryGetAsset(hash, out ImageAsset asset))
					{
						text = asset.ToGlobalUri();
					}
					castAccessor.SetValue(text);
					iconPicker.icon = text;
				});
			};
			return new Group
			{
				displayName = "Icon",
				children = new IWidget[2] { stringInputField, iconPicker }
			};
		};
	}
}
