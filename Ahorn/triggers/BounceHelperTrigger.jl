module BounceHelperBounceHelperTrigger

using ..Ahorn, Maple

@mapdef Trigger "BounceHelper/BounceHelperTrigger" BounceHelperTrigger(x::Integer, y::Integer,
                                                                      width::Integer=16, height::Integer=16,
	                                                                  enable::Bool=true,
                                                                      useVanillaThrowBehaviour::Bool=false,
                                                                      useVanillaPickupBehaviour::Bool=false,
                                                                      disableOnLeave::Bool=false)

const placements = Ahorn.PlacementDict(
    "Bounce Helper Trigger (Bounce Helper)" => Ahorn.EntityPlacement(
        BounceHelperTrigger,
        "rectangle"
    )
)

end