using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Functions;

using UnityEngine;

namespace EchKode.PBMods.BurstFire
{
	public class ExponentialRoundTiming : ISubsystemFunctionGeneral
	{
		public float timeFrom;
		public float timeTo = 1f;
		public float exponent = 1f;

		public void OnPartEventGeneral(EquipmentEntity subsystem, string context)
		{
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) {2} -- enter | subsystem: {3} | context: {4}",
					ModLink.modIndex,
					ModLink.modID,
					nameof(ExponentialRoundTiming),
					subsystem.ToLog(),
					context);
			}

			if (context != PartEventsGeneral.OnPartActivation)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) {2} -- early exit, context guard | subsystem: {3} | context: {4}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog(),
						context);
				}
				return;
			}

			var activation = subsystem.dataLinkSubsystem.data?.activationProcessed;

			if (activation?.timing != null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) {2} -- early exit, detected activation timing | subsystem: {3}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog());
				}
				return;
			}

			var partID = subsystem.hasSubsystemParentPart
				? subsystem.subsystemParentPart.equipmentID
				: IDUtility.invalidID;
			var part = IDUtility.GetEquipmentEntity(partID);
			if (part == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) {2} -- early exit, subsystem is not in a part | subsystem: {3}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog());
				}
				return;
			}

			var unitID = part.hasPartParentUnit
				? part.partParentUnit.persistentID
				: IDUtility.invalidID;
			var unit = IDUtility.GetPersistentEntity(unitID);
			if (unit == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) {2} -- early exit, not part of a unit | subsystem: {3} | part: {4}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog(),
						part.ToLog());
				}
				return;
			}

			var actionOwner = IDUtility.GetLinkedCombatEntity(unit);
			if (actionOwner == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) {2} -- early exit, unit is not linked to a combat entity | subsystem: {3} | part: {4} | unit: {5}",
						ModLink.modIndex,
						ModLink.modID,
						nameof(ExponentialRoundTiming),
						subsystem.ToLog(),
						part.ToLog(),
						unit.ToLog());
				}
				return;
			}

			var clampedFrom = Mathf.Clamp01(timeFrom);
			var clampedTo = Mathf.Clamp01(timeTo);
			var clampedExponent = Mathf.Clamp(exponent, exponentLowerBound, exponentUpperBound);

			foreach (var action in Contexts.sharedInstance.action.GetEntitiesWithActionOwner(actionOwner.id.id))
			{
				if (!action.hasActiveEquipmentPart)
				{
					continue;
				}
				if (action.activeEquipmentPart.equipmentID != part.id.id)
				{
					continue;
				}

				subactions.Clear();
				foreach (var subaction in Contexts.sharedInstance.action.GetEntities())
				{
					if (!subaction.hasScheduledAttack)
					{
						continue;
					}
					if (subaction.scheduledAttack.parentActionID != action.id.id)
					{
						continue;
					}
					subactions.Add(subaction);
				}
				
				if (subactions.Count == 0)
				{
					continue;
				}
				if (subactions.Count == 1)
				{
					continue;
				}

				// Start time and duration have to be adjusted by a fudge factor for animation.
				var startTime = action.startTime.f + DataShortcuts.anim.targetedActionBuffer;
				var duration = action.duration.f - DataShortcuts.anim.targetedActionBuffer * 2;
				var endTime = startTime + duration;

				var timingStart = Mathf.Lerp(startTime, endTime, clampedFrom);
				var timingEnd = Mathf.Lerp(startTime, endTime, clampedTo);
				var exponential = !clampedExponent.RoughlyEqual(1f);

				foreach (var subaction in subactions)
				{
					var interpolant = subaction.subActionIndex.i / (subactions.Count - 1f);
					if (exponential)
					{
						interpolant = Mathf.Pow(interpolant, clampedExponent);
					}
					var newTiming = Mathf.Lerp(timingStart, timingEnd, interpolant);
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.ExecutionDetails))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) {2} -- timing change {6:F3}s → {7:F3}s | subsystem: {3} | action: {4} | subaction: {5}",
							ModLink.modIndex,
							ModLink.modID,
							nameof(ExponentialRoundTiming),
							subsystem.ToLog(),
							action.ToLog(),
							subaction.ToLog(),
							subaction.startTime.f,
							newTiming);
					}
					subaction.ReplaceStartTime(newTiming);
				}
			}
		}

		private static readonly List<ActionEntity> subactions = new List<ActionEntity>();

		// These bounds come from the recommendations in the wiki describing the new activation timing feature.
		// https://wiki.braceyourselfgames.com/en/PhantomBrigade/Modding/ModSystemChanges
		private const float exponentLowerBound = 0.5f;
		private const float exponentUpperBound = 2f;
	}
}
