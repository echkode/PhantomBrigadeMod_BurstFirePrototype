using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public static class PartEventsFiring
	{
		public const string OnPartFiring = "on_part_firing";

		public static void OnPartEventFiring(
			this EquipmentEntity part,
			string context,
			ActionEntity action,
			float startTime,
			float duration,
			List<ActionEntity> subactions)
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
					"Firing event: {0} | Part: {1} | Action: {2} | Start time: {3:F3}s | Duration: {4:F3}s | Subaction count: {5}",
					context,
					part.ToLog(),
					action.ToLog(),
					startTime,
					duration,
					subactions?.Count ?? 0);
			}

			var subsystemsInPart = EquipmentUtility.GetSubsystemsInPart(part);
			if (subsystemsInPart == null)
			{
				return;
			}

			// Not sure that I like having subsystems other than the primary activation subsystem seeing the individual rounds.
			// Could easily lead to different subsystems clashing with each other. There's also no fixed order to the iteration
			// so it's possible the order could vary with every call to this function.
			foreach (var subsystem in subsystemsInPart)
			{
				var data = subsystem.hasDataLinkSubsystem
					? subsystem.dataLinkSubsystem.data
					: null;
				if (data == null)
				{
					continue;
				}

				// All of this is to work around the fact that I can't change the structure of the DataContainerSubsystem class.
				// This is supposed to look like the loop in PartEventsAction.OnPartEventAction().

				if (data.customProcessed == null)
				{
					continue;
				}

				if (!data.customProcessed.TryGetString("firingFunction", out var functionName))
				{
					continue;
				}

				switch (functionName)
				{
					case "ExponentialRoundTiming":
						ExponentialRoundTiming(
							subsystem,
							context,
							action,
							startTime,
							duration,
							subactions,
							data.customProcessed);
						break;
					case "BurstRoundTiming":
						BurstRoundTiming(
							subsystem,
							context,
							action,
							startTime,
							duration,
							subactions,
							data.customProcessed);
						break;
				}
			}
		}

		static void ExponentialRoundTiming(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			float startTime,
			float duration,
			List<ActionEntity> subactions,
			DataBlockPartCustom customProcessed)
		{
			var ok = customProcessed.TryGetFloat("timeFrom", out var timeFrom);
			ok &= customProcessed.TryGetFloat("timeTo", out var timeTo);
			ok &= customProcessed.TryGetFloat("exponent", out var exponent);
			if (!ok)
			{
				return;
			}

			var ert = new ExponentialRoundTiming()
			{
				timeFrom = timeFrom,
				timeTo = timeTo,
				exponent = exponent,
			};
			ert.OnPartEventFiring(subsystem, context, action, startTime, duration, subactions);
		}

		static void BurstRoundTiming(
			EquipmentEntity subsystem,
			string context,
			ActionEntity action,
			float startTime,
			float duration,
			List<ActionEntity> subactions,
			DataBlockPartCustom customProcessed)
		{
			var ok = customProcessed.TryGetFloat("burst0", out var burst0);
			ok &= customProcessed.TryGetFloat("burst1", out var burst1);
			ok &= customProcessed.TryGetFloat("burst2", out var burst2);
			ok &= customProcessed.TryGetFloat("burst3", out var burst3);
			ok &= customProcessed.TryGetFloat("burst4", out var burst4);
			ok &= customProcessed.TryGetFloat("burst5", out var burst5);
			if (!ok)
			{
				return;
			}
			var burstTimings = new List<float>() { burst0, burst1, burst2, burst3, burst4, burst5, };
			var brt = new BurstRoundTiming()
			{
				burstTimings = burstTimings,
			};
			brt.OnPartEventFiring(subsystem, context, action, startTime, duration, subactions);
		}
	}
}
