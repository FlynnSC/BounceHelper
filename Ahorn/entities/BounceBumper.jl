module BounceHelperBounceBumper

using ..Ahorn, Maple

@mapdef Entity "BounceHelper/BounceBumper" BounceBumper(x::Integer, 
                                                        y::Integer)

const placements = Ahorn.PlacementDict(
    "Bounce Bumper (Bounce Helper)" => Ahorn.EntityPlacement(
        BounceBumper
    )
)

sprite = "objects/Bumper/Idle22.png"

function Ahorn.selection(entity::BounceBumper)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceBumper, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end