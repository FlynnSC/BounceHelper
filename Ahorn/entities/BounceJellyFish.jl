module BounceHelperBounceJellyfish

using ..Ahorn, Maple

@mapdef Entity "BounceHelper/BounceJellyfish" BounceJellyfish(x::Integer, 
                                                              y::Integer,
                                                              platform::Bool=true,
                                                              soulBound::Bool=true,
                                                              baseDashCount::Integer=1,
                                                              ezelMode::Bool=false,
                                                              matchPlayerDash::Bool=false) 

const placements = Ahorn.PlacementDict(
    "Bounce Jellyfish (Bounce Helper)" => Ahorn.EntityPlacement(
        BounceJellyfish
    )
)

const sprites = Dict{Integer, String}(
    0 => "blue",
	1 => "red",
	2 => "pink",
)

function Ahorn.selection(entity::BounceJellyfish)
    sprite = "objects/BounceHelper/bounceJellyfish/blue/idle0"

    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceJellyfish, room::Maple.Room)
    baseDashCount = Int(get(entity.data, "baseDashCount", 1));
    suffix = (get(entity, "ezelMode", false) && baseDashCount > 0) ? "Head/idle0" : "/idle0"; 
    sprite = "objects/BounceHelper/bounceJellyfish/" * sprites[baseDashCount] * suffix;

    Ahorn.drawSprite(ctx, sprite, 0, 0)
    
    if get(entity, "platform", true)
        curve = Ahorn.SimpleCurve((-7, -1), (7, -1), (0, -6))
        Ahorn.drawSimpleCurve(ctx, curve, (1.0, 1.0, 1.0, 1.0), thickness=1)
    end
end

end