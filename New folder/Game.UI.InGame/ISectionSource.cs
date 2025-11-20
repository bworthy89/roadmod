using Colossal.UI.Binding;

namespace Game.UI.InGame;

public interface ISectionSource : IJsonWritable
{
	void RequestUpdate();

	void PerformUpdate();
}
