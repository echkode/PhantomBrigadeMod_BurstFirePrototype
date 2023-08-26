// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class BurstRoundTiming : ISubsystemFunctionFiring
	{
		public List<float> burstTimings;

		public void OnPartEventFiring(EquipmentEntity subsystem, string context, ActionEntity action, float startTime, float duration, List<ActionEntity> subactions)
		{
			var endTime = startTime + duration;

			foreach (var subaction in subactions)
			{
				if (subaction.subActionIndex.i >= burstTimings.Count)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) {2} -- no timing for subaction | subsystem: {3} | action: {4} | subaction: {5} | index: {6}",
							ModLink.modIndex,
							ModLink.modID,
							nameof(BurstRoundTiming),
							subsystem.ToLog(),
							action.ToLog(),
							subaction.ToLog(),
							subaction.subActionIndex.i);
					}
					continue;
				}

				var interpolant = Mathf.Clamp01(burstTimings[subaction.subActionIndex.i]);
				var newTiming = Mathf.Lerp(startTime, endTime, interpolant);
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) {2} -- timing change {7:F3}s → {8:F3}s | subsystem: {3} | action: {4} | subaction: {5} | index: {6}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(BurstRoundTiming),
						subsystem.ToLog(),
						action.ToLog(),
						subaction.ToLog(),
						subaction.subActionIndex.i,
						subaction.startTime.f,
						newTiming);
				}
				subaction.ReplaceStartTime(newTiming);
			}
		}
	}
}
