using System.Collections.Generic;

using Content.Code.Utility;

namespace EchKode.PBMods.BurstFire
{
	[TypeHinted]
	public interface ISubsystemFunctionFiring
	{
		void OnPartEventFiring(EquipmentEntity subsystem, string context, ActionEntity action, float startTime, float duration, List<ActionEntity> subactions);
	}
}
