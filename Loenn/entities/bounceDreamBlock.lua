local BounceHelperBounceDreamBlock = {}

BounceHelperBounceDreamBlock.name = "BounceHelper/BounceDreamBlock"
BounceHelperBounceDreamBlock.fillColor = {0.0, 0.0, 0.0}
BounceHelperBounceDreamBlock.borderColor = {1.0, 1.0, 1.0}
BounceHelperBounceDreamBlock.depth = 5000
BounceHelperBounceDreamBlock.nodeLineRenderType = "line"
BounceHelperBounceDreamBlock.nodeLimits = {0, 1}
BounceHelperBounceDreamBlock.placements = {
    name = "normal",
    data = {
        internalAccelX = 0,
        internalAccelY = 0,
        oscillationDuration = 1.0,
        width = 8,
        height = 8
    }
}

return BounceHelperBounceDreamBlock