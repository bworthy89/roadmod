using Colossal.UI.Binding;
using Unity.Entities;

namespace Game.UI.InGame;

public interface ISubsectionSource : IJsonWritable
{
	bool DisplayFor(Entity entity, Entity prefab);

	void OnRequestUpdate(Entity entity, Entity prefab);
}
