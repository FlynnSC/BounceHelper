module BounceHelperBounceJellyfish

using ..Ahorn, Maple

@mapdef Entity "BounceHelper/BounceJellyfish" BounceJellyfish(x::Integer, 
                                                              y::Integer,
                                                              soulBound::Bool=true,
                                                              baseDashCount::Integer=1) 

const placements = Ahorn.PlacementDict(
    "Bounce Jellyfish (Bounce Helper)" => Ahorn.EntityPlacement(
        BounceJellyfish
    )
)

const sprites = Dict{Integer, String}(
    0 => "objects/BounceHelper/bounceJellyfish/blue/idle0",
	1 => "objects/BounceHelper/bounceJellyfish/red/idle0",
	2 => "objects/BounceHelper/bounceJellyfish/pink/idle0",
)

function Ahorn.selection(entity::BounceJellyfish)
    sprite = sprites[Int(get(entity.data, "baseDashCount", 1))]

    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceJellyfish, room::Maple.Room)
    sprite = sprites[Int(get(entity.data, "baseDashCount", 1))]

    Ahorn.drawSprite(ctx, sprite, 0, 0)
    curve = Ahorn.SimpleCurve((-7, -1), (7, -1), (0, -6))
    Ahorn.drawSimpleCurve(ctx, curve, (1.0, 1.0, 1.0, 1.0), thickness=1)
end

end