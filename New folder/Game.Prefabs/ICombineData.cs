using Unity.Entities;

namespace Game.Prefabs;

public interface ICombineData<T> where T : IComponentData
{
	void Combine(T other);
}
