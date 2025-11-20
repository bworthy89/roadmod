namespace Game.Simulation.Flow;

public struct CutElement
{
	public CutElementFlags m_Flags;

	public int m_StartNode;

	public int m_EndNode;

	public int m_Edge;

	public int m_Group;

	public int m_Version;

	public int m_LinkedElements;

	public int m_NextIndex;

	public bool isCreated
	{
		get
		{
			return GetFlag(CutElementFlags.Created);
		}
		set
		{
			SetFlag(CutElementFlags.Created, value);
		}
	}

	public bool isAdmissible
	{
		get
		{
			return GetFlag(CutElementFlags.Admissible);
		}
		set
		{
			SetFlag(CutElementFlags.Admissible, value);
		}
	}

	public bool isChanged
	{
		get
		{
			return GetFlag(CutElementFlags.Changed);
		}
		set
		{
			SetFlag(CutElementFlags.Changed, value);
		}
	}

	public bool isDeleted
	{
		get
		{
			return GetFlag(CutElementFlags.Deleted);
		}
		set
		{
			SetFlag(CutElementFlags.Deleted, value);
		}
	}

	private bool GetFlag(CutElementFlags flag)
	{
		return (m_Flags & flag) != 0;
	}

	private void SetFlag(CutElementFlags flag, bool value)
	{
		if (value)
		{
			m_Flags |= flag;
		}
		else
		{
			m_Flags &= ~flag;
		}
	}
}
