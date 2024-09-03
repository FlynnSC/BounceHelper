local bounceHelperBounceTriggerPlacement = {}
bounceHelperBounceTriggerPlacement.name = "BounceHelper/BounceHelperTrigger"
bounceHelperBounceTriggerPlacement.placements = {
{
        name = "normal",
        data = {
            enable = true,
            useVanillaThrowBehaviour = false,
            useVanillaPickupBehaviour = false,
            disableOnLeave = false
        }
    },
    {
        name = "disable",
        data = {
            enable = false,
            useVanillaThrowBehaviour = false,
            useVanillaPickupBehaviour = false,
            disableOnLeave = false
        }
    }
}

return bounceHelperBounceTriggerPlacement