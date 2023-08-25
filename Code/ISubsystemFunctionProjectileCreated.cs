using Content.Code.Utility;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	[TypeHinted]
	public interface ISubsystemFunctionProjectileCreated
	{
		void OnPartProjectileCreated(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity owner,
			CombatEntity projectile,
			Vector3 firingPoint,
			Vector3 firingDirection,
			Vector3 targetPoint);
	}
}
