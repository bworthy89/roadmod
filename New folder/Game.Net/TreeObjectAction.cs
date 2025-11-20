using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Net;

public struct TreeObjectAction
{
	public Entity m_Remove;

	public Entity m_Add;

	public Bounds3 m_Bounds;

	public TreeObjectAction(Entity remove)
	{
		m_Remove = remove;
		m_Add = Entity.Null;
		m_Bounds = default(Bounds3);
	}

	public TreeObjectAction(Entity add, Bounds3 bounds)
	{
		m_Remove = Entity.Null;
		m_Add = add;
		m_Bounds = bounds;
	}

	public TreeObjectAction(Entity remove, Entity add, Bounds3 bounds)
	{
		m_Remove = remove;
		m_Add = add;
		m_Bounds = bounds;
	}
}
