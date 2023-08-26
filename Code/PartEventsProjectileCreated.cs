// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public static class PartEventsProjectileCreated
	{
		public const string OnPartProjectileCreated = "on_part_projectile_created";

		public static void OnPartEventProjectileCreated(
			this EquipmentEntity part,
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
			if (part == null)
			{
				return;
			}
			if (string.IsNullOrEmpty(context))
			{
				return;
			}

			if (DataShortcuts.sim.partEventsLog)
			{
				Debug.LogFormat(
					"Projectile event: {0} | Part: {1} | Action: {2} | Subaction: {3} | Current time: {4:F3}s | Owner: {5} | Projectile: {6}",
					context,
					part.ToLog(),
					action.ToLog(),
					subaction.ToLog(),
					currentTime,
					owner.ToLog(),
					projectile.ToLog());
			}

			var subsystemsInPart = EquipmentUtility.GetSubsystemsInPart(part);
			if (subsystemsInPart == null)
			{
				return;
			}

			foreach (var subsystem in subsystemsInPart)
			{
				var data = subsystem.hasDataLinkSubsystem
					? subsystem.dataLinkSubsystem.data
					: null;
				if (data == null)
				{
					continue;
				}
				if (data.customProcessed == null)
				{
					continue;
				}
				if (!data.customProcessed.TryGetString("projectileCreatedFunction", out var functionName))
				{
					continue;
				}

				switch (functionName)
				{
					case "BurstScatter":
						BurstScatter(
							subsystem,
							context,
							action,
							subaction,
							currentTime,
							owner,
							projectile,
							firingPoint,
							firingDirection,
							targetPoint,
							data.customProcessed);
						break;
					case "StrafeProjectileCreated":
						StrafeProjectileCreated(
							subsystem,
							context,
							action,
							subaction,
							currentTime,
							owner,
							projectile,
							firingPoint,
							firingDirection,
							targetPoint,
							data.customProcessed);
						break;
				}
			}
		}

		private static void BurstScatter(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity owner,
			CombatEntity projectile,
			Vector3 firingPoint,
			Vector3 firingDirection,
			Vector3 targetPoint,
			DataBlockPartCustom customProcessed)
		{
			var ok = customProcessed.TryGetInt("roundsPerBurst", out var rpb);
			if (!ok)
			{
				return;
			}

			var bs = new BurstScatter()
			{
				roundsPerBurst = rpb,
			};
			bs.OnPartProjectileCreated(
				subsystem,
				context,
				action,
				subaction,
				currentTime,
				owner,
				projectile,
				firingPoint,
				firingDirection,
				targetPoint);
		}


		private static void StrafeProjectileCreated(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			ActionEntity subaction,
			float currentTime,
			CombatEntity owner,
			CombatEntity projectile,
			Vector3 firingPoint,
			Vector3 firingDirection,
			Vector3 targetPoint,
			DataBlockPartCustom customProcessed)
		{
			var ok = customProcessed.TryGetFloat("turnrate", out var turnrate);
			if (!ok)
			{
				return;
			}

			var spc = new StrafeProjectileCreated()
			{
				turnrate = turnrate,
			};
			spc.OnPartProjectileCreated(
				subsystem,
				context,
				action,
				subaction,
				currentTime,
				owner,
				projectile,
				firingPoint,
				firingDirection,
				targetPoint);
		}
	}
}
