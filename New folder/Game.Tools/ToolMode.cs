namespace Game.Tools;

public readonly struct ToolMode
{
	public string name { get; }

	public int index { get; }

	public ToolMode(string name, int index)
	{
		this.name = name;
		this.index = index;
	}
}
