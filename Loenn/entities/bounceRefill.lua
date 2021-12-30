local BounceHelperBounceRefill = {}

BounceHelperBounceRefill.name = "BounceHelper/BounceRefill"
BounceHelperBounceRefill.depth = -100
BounceHelperBounceRefill.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            twoDash = false
        }
    },
    {
        name = "double",
        data = {
            oneUse = false,
            twoDash = true
        }
    }
}

function BounceHelperBounceRefill.texture(room, entity)
    return entity.twoDash and "objects/refillTwo/idle00" or "objects/refill/idle00"
end

return BounceHelperBounceRefill