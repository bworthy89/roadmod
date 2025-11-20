using System;
using Colossal.UI.Binding;
using Game.Modding.Toolchain;

namespace Game.UI.Menu;

public class ModdingToolchainDependencyWriter : IWriter<IToolchainDependency>
{
	public void Write(IJsonWriter writer, IToolchainDependency value)
	{
		if (value != null)
		{
			writer.TypeBegin(value.GetType().FullName);
			writer.PropertyName("name");
			writer.Write(value.localizedName);
			writer.PropertyName("state");
			writer.Write((int)value.state.m_State);
			writer.PropertyName("progress");
			writer.Write(value.state.m_Progress ?? (-1));
			writer.PropertyName("details");
			writer.Write(value.GetLocalizedState(includeProgress: false));
			writer.PropertyName("version");
			writer.Write(value.GetLocalizedVersion());
			writer.PropertyName("icon");
			writer.Write(value.icon);
			writer.TypeEnd();
			return;
		}
		writer.WriteNull();
		throw new ArgumentNullException("value", "Null passed to non-nullable value writer");
	}
}
