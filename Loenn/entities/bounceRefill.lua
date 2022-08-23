local BounceHelperBounceRefill = {}

BounceHelperBounceRefill.name = "BounceHelper/BounceRefill"
BounceHelperBounceRefill.depth = -100
BounceHelperBounceRefill.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            twoDash = false,
            jellyfishOnly = false
        }
    },
    {
        name = "double",
        data = {
            oneUse = false,
            twoDash = true,
            jellyfishOnly = false
        }
    }
}

function BounceHelperBounceRefill.texture(room, entity)
    local prefix = entity.jellyfishOnly and "objects/BounceHelper/bounceRefillJellyfishOnly/" or "objects/"
    return prefix .. (entity.twoDash and "refillTwo/" or "refill/") .. "idle00"
end

return BounceHelperBounceRefill