// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class BurstScatter : ISubsystemFunctionProjectileCreated
	{
		public int roundsPerBurst;

		public void OnPartProjectileCreated(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity owner,
			CombatEntity projectile,
			Vector3 firingPoint,
			Vector3 firingDirection,
			Vector3 targetPoint)
		{
			if (!scatterDirectionMap.TryGetValue(action.id.id, out var entry))
			{
				scatterDirectionMap.Add(action.id.id, (1, firingPoint, projectile.facing.v, currentTime));
				return;
			}
			if (entry.RoundCount % roundsPerBurst == 0)
			{
				scatterDirectionMap[action.id.id] = (1, firingPoint, projectile.facing.v, currentTime);
				return;
			}

			// Use scatter direction from the first round in the burst.
			projectile.ReplaceFacing(entry.Direction);
			projectile.ReplaceRotation(Quaternion.LookRotation(entry.Direction));

			var lag = currentTime - subaction.startTime.f;
			if (lag > lagEpsilon)
			{
				// There's some lag patch-up logic in ScheduledAttackSystem so we'll also patch up the projectile position.
				//
				// If there's lag, the current position of the muzzle may have moved from when the projectile was exiting.
				// Do a lerp between the prior firingPoint and the current one to get that position. We're going to
				// ignore rotation of the weapon's line of sight since the whole point of this function is to make
				// the rounds in a burst follow parallel trajectories.
				var afp = entry.FiringPoint;

				if (subaction.startTime.f > entry.Time)
				{
					var interpolant = (subaction.startTime.f - entry.Time) / (currentTime - entry.Time);
					afp = Vector3.Lerp(entry.FiringPoint, firingPoint, interpolant);
				}

				var part = IDUtility.GetEquipmentEntity(subsystem.subsystemParentPart.equipmentID);
				var wpnSpeed = Mathf.Max(1f, DataHelperStats.GetCachedStatForPart(UnitStats.weaponProjectileSpeed, part));
				var position = afp + entry.Direction * wpnSpeed * lag;
				projectile.ReplacePosition(position);
			}

			if (subaction.ScheduledAttackEnd)
			{
				// This is a bit risky since there's no guarantee about the order in which ScheduledAttackSystem will
				// process subactions. It may be possible to get the last subaction before actually seeing all of the
				// prior subactions associated with this action.
				scatterDirectionMap.Remove(action.id.id);
				return;
			}

			if (lag < lagEpsilon)
			{
				scatterDirectionMap[action.id.id] = (entry.RoundCount + 1, firingPoint, entry.Direction, currentTime);
				return;
			}

			scatterDirectionMap[action.id.id] = (entry.RoundCount + 1, entry.FiringPoint, entry.Direction, entry.Time);
		}

		private static Dictionary<int, (int RoundCount, Vector3 FiringPoint, Vector3 Direction, float Time)> scatterDirectionMap =
			new Dictionary<int, (int, Vector3, Vector3, float)>();

		private const float lagEpsilon = 0.005f;
	}
}
