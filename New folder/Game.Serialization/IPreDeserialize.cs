using Colossal.Serialization.Entities;

namespace Game.Serialization;

public interface IPreDeserialize
{
	void PreDeserialize(Context context);
}
