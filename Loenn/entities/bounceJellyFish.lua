local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawing = require("utils.drawing")

local BounceHelperBounceJellyfish = {}

BounceHelperBounceJellyfish.name = "BounceHelper/BounceJellyfish"
BounceHelperBounceJellyfish.depth = -5
local BounceHelperBounceJellyfishDashCounts = {
    0, 1, 2
}

BounceHelperBounceJellyfish.fieldInformation = {
    baseDashCount = {
        options = BounceHelperBounceJellyfishDashCounts,
        editable = false
    }
}
BounceHelperBounceJellyfish.placements = {
    {
        name = "normal",
        data = {
            platform = true,
            soulBound = false,
            baseDashCount = 0
        }
    },
    {
        name = "single",
        data = {
            platform = true,
            soulBound = false,
            baseDashCount = 1
        }
    },
    {
        name = "double",
        data = {
            platform = true,
            soulBound = false,
            baseDashCount = 2
        }
    }
}

local function getColor(entity)
    local dash = entity.baseDashCount

    if dash == 0 then
        return "objects/BounceHelper/bounceJellyfish/blue/idle0"

    elseif dash == 1 then
        return "objects/BounceHelper/bounceJellyfish/red/idle0"

    else
        return "objects/BounceHelper/bounceJellyfish/pink/idle0"
    end
end

function BounceHelperBounceJellyfish.sprite(room, entity)
    local platform = entity.platform

    if entity.platform then
        local x, y = entity.x or 0, entity.y or 0
        local points = drawing.getSimpleCurve({x - 11, y - 1}, {x + 11, y - 1}, {x - 0, y - 6})
        local lineSprites = drawableLine.fromPoints(points):getDrawableSprite()
        local texture = getColor(entity)
        local jellySprite = drawableSprite.fromTexture(texture, entity)

        table.insert(lineSprites, 1, jellySprite)

        return lineSprites

    else
        local texture = getColor(entity)
        return drawableSprite.fromTexture(texture, entity)
    end
end

function BounceHelperBounceJellyfish.rectangle(room, entity)
    local texture = "objects/glider/idle0"
    local sprite = drawableSprite.fromTexture(texture, entity)

    return sprite:getRectangle()
end

return BounceHelperBounceJellyfish