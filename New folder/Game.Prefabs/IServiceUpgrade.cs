using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public interface IServiceUpgrade
{
	void GetUpgradeComponents(HashSet<ComponentType> components);

	void DoActionWithOwnerAfterRemove(EntityManager entityManager, Entity ownerEntity)
	{
	}
}
