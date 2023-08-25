using PhantomBrigade.Data;
using PhantomBrigade.Functions;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class TracerTargeted : ISubsystemFunctionTargeted
	{
		public int modulus;
		public int startingOffset;
		public DataBlockColorInterpolated color;

		public void OnPartEventTargeted(
			EquipmentEntity subsystem,
			string context,
			Vector3 position,
			Vector3 direction,
			Vector3 targetPosition,
			CombatEntity targetUnitCombat,
			CombatEntity projectile)
		{
			if (context != PartEventsTargeted.OnWeaponFired)
			{
				return;
			}

			if (color == null)
			{
				return;
			}

			if (!projectile.hasAssetLink)
			{
				return;
			}

			if (projectile.assetLink.instance == null)
			{
				return;
			}

			if (!projectile.hasProjectileIndex)
			{
				return;
			}

			var isTracer = (projectile.projectileIndex.i + startingOffset) % modulus == 0;
			if (!isTracer)
			{
				return;
			}

			projectile.assetLink.instance.UpdateColors(null, color);
		}
	}
}
