module BounceHelperBounceRefill

using ..Ahorn, Maple

@mapdef Entity "BounceHelper/BounceRefill" BounceRefill(x::Integer, 
                                                        y::Integer,
                                                        twoDash::Bool=false,
                                                        oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Bounce Refill (BounceHelper)" => Ahorn.EntityPlacement(
        BounceRefill,
        "point"
    )
)

spriteOneDash = "objects/refill/idle00"
spriteTwoDash = "objects/refillTwo/idle00"

function getSprite(entity::BounceRefill)
    twoDash = get(entity.data, "twoDash", false)

    return twoDash ? spriteTwoDash : spriteOneDash
end

function Ahorn.selection(entity::BounceRefill)
    x, y = Ahorn.position(entity)
    sprite = getSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceRefill, room::Maple.Room)
    sprite = getSprite(entity)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end