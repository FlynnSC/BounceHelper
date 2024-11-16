local fakeTilesHelper = require("helpers.fake_tiles")

local BounceFallingBlock = {}

BounceFallingBlock.name = "BounceHelper/BounceFallingBlock"
BounceFallingBlock.placements = {
    name = "normal",
    data = {
        tiletype = "3",
        climbFall = true,
        behind = false,
        width = 8,
        height = 8
    }
}

BounceFallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
BounceFallingBlock.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

function BounceFallingBlock.depth(room, entity)
    return entity.behind and 5000 or 0
end

return BounceFallingBlock