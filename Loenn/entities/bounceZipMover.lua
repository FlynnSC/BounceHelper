local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local BounceHelperBounceZipMover = {}

local themeTextures = {
    normal = {
        nodeCog = "/cog",
        lights = "/light01",
        block = "/block",
        innerCogs = "/innercog"
    },
    moon = {
        nodeCog = "/moon/cog",
        lights = "/moon/light01",
        block = "/moon/block",
        innerCogs = "/moon/innercog"
    }
}

local blockNinePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

local defaultFillColor = "000000"
local defaultRopeColor = "663931"
local defaultRopeLightColor = "9b6157"

BounceHelperBounceZipMover.name = "BounceHelper/BounceZipMover"
BounceHelperBounceZipMover.depth = -9999
BounceHelperBounceZipMover.nodeVisibility = "never"
BounceHelperBounceZipMover.nodeLimits = {1, 1}
BounceHelperBounceZipMover.minimumSize = {16, 16}

BounceHelperBounceZipMover.placements = {
    {
        name = "normal",
        data = {
            width = 16,
            height = 16,
            moon = false,
            spritePath = "objects/zipmover",
            fillColor = defaultFillColor,
            ropeColor = defaultRopeColor,
            ropeLightColor = defaultRopeLightColor
        }
    },
    {
        name = "moon",
        data = {
            width = 16,
            height = 16,
            moon = true,
            spritePath = "objects/zipmover",
            fillColor = defaultFillColor,
            ropeColor = defaultRopeColor,
            ropeLightColor = defaultRopeLightColor
        }
    }
}

BounceHelperBounceZipMover.fieldInformation = {
    fillColor = {
        fieldType = "color"
    },
    ropeColor = {
        fieldType = "color"
    },
    ropeLightColor = {
        fieldType = "color"
    }
}

local function getTheme(entity)
    local moon = entity.moon

    if moon then
        return "moon"
    else
        return "normal"
    end
end

local function addNodeSprites(sprites, entity, cogTexture, centerX, centerY, centerNodeX, centerNodeY)
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture, entity)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local leftLine = drawableLine.fromPoints(points, entity.ropeColor or defaultRopeColor, 1)
    local rightLine = drawableLine.fromPoints(points, entity.ropeColor or defaultRopeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = 5000
    rightLine.depth = 5000

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

local function addBlockSprites(sprites, entity, blockTexture, lightsTexture, x, y, width, height)
    local rectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4,
        entity.fillColor or defaultFillColor)

    local frameNinePatch = drawableNinePatch.fromTexture(blockTexture, blockNinePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()

    local lightsSprite = drawableSprite.fromTexture(lightsTexture, entity)

    lightsSprite:addPosition(math.floor(width / 2), 0)
    lightsSprite:setJustification(0.5, 0.0)

    table.insert(sprites, rectangle:getDrawableSprite())

    for _, sprite in ipairs(frameSprites) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, lightsSprite)
end

function BounceHelperBounceZipMover.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y

    local centerX, centerY = x + halfWidth, y + halfHeight
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local theme = getTheme(entity)
    local themeData = themeTextures[theme] or themeTextures["normal"]
    local spritePath = entity.spritePath or "objects/zipmover"

    addNodeSprites(sprites, entity, spritePath .. themeData.nodeCog, centerX, centerY, centerNodeX, centerNodeY)
    addBlockSprites(sprites, entity, spritePath .. themeData.block, spritePath .. themeData.lights, x, y, width, height)

    return sprites
end

function BounceHelperBounceZipMover.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    local theme = getTheme(entity)
    local themeData = themeTextures[theme] or themeTextures["normal"]
    local spritePath = entity.spritePath or "objects/zipmover"

    local cogSprite = drawableSprite.fromTexture(spritePath .. themeData.nodeCog, entity)
    local cogWidth, cogHeight = cogSprite.meta.width, cogSprite.meta.height

    local mainRectangle = utils.rectangle(x, y, width, height)
    local nodeRectangle = utils.rectangle(centerNodeX - math.floor(cogWidth / 2), centerNodeY - math.floor(cogHeight / 2), cogWidth, cogHeight)

    return mainRectangle, {nodeRectangle}
end

return BounceHelperBounceZipMover