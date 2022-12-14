local BounceHelperBounceRefill = {}

BounceHelperBounceRefill.name = "BounceHelper/BounceRefill"
BounceHelperBounceRefill.depth = -100
BounceHelperBounceRefill.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            twoDash = false,
            jellyfishOnly = false,
            respawnTime = 2.5
        }
    },
    {
        name = "double",
        data = {
            oneUse = false,
            twoDash = true,
            jellyfishOnly = false,
            respawnTime = 2.5
        }
    }
}

function BounceHelperBounceRefill.texture(room, entity)
    local prefix = entity.jellyfishOnly and "objects/BounceHelper/bounceRefillJellyfishOnly/" or "objects/"
    return prefix .. (entity.twoDash and "refillTwo/" or "refill/") .. "idle00"
end

return BounceHelperBounceRefill