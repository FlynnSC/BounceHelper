local bounceHelperBounceTriggerPlacement = {}
bounceHelperBounceTriggerPlacement.name = "BounceHelper/BounceHelperTrigger"
bounceHelperBounceTriggerPlacement.placements = {
{
        name = "normal",
        data = {
            enable = true,
            useVanillaThrowBehaviour = false
        }
    },
    {
        name = "disable",
        data = {
            enable = false,
            useVanillaThrowBehaviour = false
        }
    }
}

return bounceHelperBounceTriggerPlacement