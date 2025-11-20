namespace Game.Reflection;

public interface ITypedValueAccessor<T> : IValueAccessor
{
	T GetTypedValue();

	void SetTypedValue(T value);
}
