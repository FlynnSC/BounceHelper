module BounceHelperBounceMoveBlock

using ..Ahorn, Maple

@mapdef Entity "BounceHelper/BounceMoveBlock" BounceMoveBlock(x::Integer, 
                                                              y::Integer, 
                                                              width::Integer=Maple.defaultBlockWidth, 
                                                              height::Integer=Maple.defaultBlockHeight,
                                                              direction::String="Right", 
                                                              speed::Number=60.0,
                                                              oneUse::Bool=false,
                                                              activationFlag::String="",
                                                              spritePath::String="objects/BounceHelper/bounceMoveBlock") 

directions = String[
    "Right",
	"UpRight",
	"Up",
	"UpLeft",
	"Left",
	"DownLeft",
	"Down",
	"DownRight",
	"Unknown"
]

const placements = Ahorn.PlacementDict(
    "Bounce Move Block (BounceHelper)" => Ahorn.EntityPlacement(
        BounceMoveBlock,
        "rectangle",
        Dict{String, Any}(
            "direction" => "Right"
        )
    )
)

Ahorn.editingOptions(entity::BounceMoveBlock) = Dict{String, Any}(
    "direction" => directions
)
Ahorn.minimumSize(entity::BounceMoveBlock) = 16, 16
Ahorn.resizable(entity::BounceMoveBlock) = true, true

Ahorn.selection(entity::BounceMoveBlock) = Ahorn.getEntityRectangle(entity)

midColor = (4, 3, 23) ./ 255
highlightColor = (59, 50, 101) ./ 255

const arrows = Dict{String, String}(
    "Right" => "/arrow00",
	"UpRight" => "/arrow01",
	"Up" => "/arrow02",
	"UpLeft" => "/arrow03",
	"Left" => "/arrow04",
	"DownLeft" => "/arrow05",
	"Down" => "/arrow06",
	"DownRight" => "/arrow07",
	"Unknown" => "/unknown"
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BounceMoveBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))


    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    direction = get(entity.data, "direction", "Right")
    spritePath = get(entity.data, "spritePath", "objects/BounceHelper/bounceMoveBlock")
    arrowSprite = Ahorn.getSprite(spritePath * arrows[direction], "Gameplay")

    frame = spritePath * "/base"

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, highlightColor, highlightColor)
    Ahorn.drawRectangle(ctx, 8, 8, width - 16, height - 16, midColor)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, 0, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, 0, (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, width - 8, (i - 1) * 8, 16, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, 0, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, 0, height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, height - 8, 16, 16, 8, 8)
    Ahorn.drawRectangle(ctx, div(width - arrowSprite.width, 2) + 1, div(height - arrowSprite.height, 2) + 1, 8, 8, highlightColor, highlightColor)
    Ahorn.drawImage(ctx, arrowSprite, div(width - arrowSprite.width, 2), div(height - arrowSprite.height, 2))
end

end