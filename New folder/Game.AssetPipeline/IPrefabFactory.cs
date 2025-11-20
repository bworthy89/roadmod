using Game.Prefabs;

namespace Game.AssetPipeline;

public interface IPrefabFactory
{
	T CreatePrefab<T>(string sourcePath, string name, int lodLevel) where T : PrefabBase;
}
