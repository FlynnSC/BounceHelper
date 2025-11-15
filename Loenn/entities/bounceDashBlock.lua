local fakeTilesHelper = require("helpers.fake_tiles")

local BounceDashBlock = {}

BounceDashBlock.name = "BounceHelper/BounceDashBlock"
BounceDashBlock.placements = {
    name = "normal",
    data = {
        tiletype = "3",
        permanent = true,
        canGraze = true,
        blendin = true,
        width = 8,
        height = 8
    }
}

BounceDashBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
BounceDashBlock.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

return BounceDashBlock