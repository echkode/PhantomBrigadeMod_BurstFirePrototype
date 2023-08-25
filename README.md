# BurstFire

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) to prototype using subsystem functions for manipulating fired round effects like timing.

It is compatible with game version **1.1.2-b5993** (Epic/Steam).

I previously emulated the activation timing feature using an implementation of the `ISubsystemFunctionGeneral` interface function in the [existing_interface branch](https://github.com/echkode/PhantomBrigadeMod_BurstFirePrototype/tree/existing_interface). That interface function was too general for this purpose so I designed a new interface function that is passed more information from the calling function to reduce the amount of lookup code and inside knowledge that an implementation needs.

I tried to follow the pattern of `PartEventsGeneral` which adds an extension method to `EquipmentEntity`. The extension method iterates through the subsystems of the part to trigger the ISubsystemFunctions of those subsystems for the particular calling context. However, I cannot change the data layout of the `DataContainerSubsystem class` to include another list of functions in its `functions` property so I had to simulate the same thing the best I could using `custom` properties. That means the prototype code deviates from the pattern of `PartEventsGeneral` out of necessity rather than design intent.

Again, I emulated the activation timing feature so that the differences could be easily compared to my first iteration of this prototype. The implementation of the new function interface is much more streamlined than the `ISubsystemFunctionGeneral` implementation. I take that as confirmation that I'm heading in the right direction.

I had to inject code into `EquipmentActionSystem.Execute()` to call the new interface function. I made a few key changes.

- Added a list to hold the subactions created for an action so that they can be passed into the new interface function
- Replaced the call to `PartEventsGeneral.OnPartEventGeneral()` with the call to the new extension method
- Added a call to PartEventsAction.OnPartEventAction()`

Here's a condensed version of the revelant parts of the code that were changed.
```
if (activationProcessed != null)
{
    var roundCount = Mathf.RoundToInt(DataHelperStats.GetCachedStatForPart(UnitStats.activationCount, equipmentEntity));
    if (roundCount > 0)
    {
        var subactions = new List<ActionEntity>();
        ...
        for (var i = 0; i < roundCount; i++)
        {
            var subactionEntity = actionContext.CreateEntity();
            subactions.Add(subactionEntity);
            ...
        }
        if (DataShortcuts.sim.partEventsEnabled)
        {
            equipmentEntity.OnPartEventFiring("on_part_firing", actionEntity, startTime, duration, subactions);
        }
    }

    if (DataShortcuts.sim.partEventsEnabled)
    {
        equipmentEntity.OnPartEventAction("on_part_activation", actionEntity);
    }
}
```
I added a call to `PartEventsAction.OnPartEventAction()` at the end so that a primary activation subsystem that does not create rounds will still trigger the activation event. I also chose to use `PartEventsAction` since there may be multiple actions which activate the part during the turn and it might be helpful to distinguish which activation event belongs to which action.
