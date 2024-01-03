local bounceHelperBounceTriggerPlacement = {}
bounceHelperBounceTriggerPlacement.name = "BounceHelper/BounceHelperTrigger"
bounceHelperBounceTriggerPlacement.placements = {
{
        name = "normal",
        data = {
            enable = true,
            useVanillaThrowBehaviour = false,
            disableOnLeave = false
        }
    },
    {
        name = "disable",
        data = {
            enable = false,
            useVanillaThrowBehaviour = false,
            disableOnLeave = false
        }
    }
}

return bounceHelperBounceTriggerPlacement