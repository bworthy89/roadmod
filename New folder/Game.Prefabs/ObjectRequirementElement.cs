using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct ObjectRequirementElement : IBufferElementData
{
	public Entity m_Requirement;

	public ushort m_Group;

	public ObjectRequirementType m_Type;

	public ObjectRequirementFlags m_RequireFlags;

	public ObjectRequirementFlags m_ForbidFlags;

	public ObjectRequirementElement(Entity requirement, int group, ObjectRequirementType type = (ObjectRequirementType)0)
	{
		m_Requirement = requirement;
		m_Group = (ushort)group;
		m_Type = type;
		m_RequireFlags = (ObjectRequirementFlags)0;
		m_ForbidFlags = (ObjectRequirementFlags)0;
	}

	public ObjectRequirementElement(ObjectRequirementFlags require, ObjectRequirementFlags forbid, int group, ObjectRequirementType type = (ObjectRequirementType)0)
	{
		m_Requirement = Entity.Null;
		m_RequireFlags = require;
		m_ForbidFlags = forbid;
		m_Group = (ushort)group;
		m_Type = type;
	}
}
