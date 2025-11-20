using Colossal.Serialization.Entities;

namespace Game.Serialization;

public interface IPreSerialize
{
	void PreSerialize(Context context);
}
