using Colossal.UI.Binding;

namespace Game.UI.InGame;

public abstract class GamePanel : IJsonWritable
{
	public enum LayoutPosition
	{
		Undefined,
		Left,
		Center,
		Right
	}

	public virtual bool blocking => false;

	public virtual bool retainSelection => false;

	public virtual bool retainProperties => false;

	public virtual LayoutPosition position => LayoutPosition.Undefined;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		BindProperties(writer);
		writer.TypeEnd();
	}

	protected virtual void BindProperties(IJsonWriter writer)
	{
	}
}
