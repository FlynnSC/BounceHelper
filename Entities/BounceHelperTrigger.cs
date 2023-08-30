using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.BounceHelper {
    [CustomEntity("BounceHelper/BounceHelperTrigger")]
    class BounceHelperTrigger : Trigger {
        private bool enable;
        private bool useVanillaThrowBehaviour;

        public BounceHelperTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            enable = data.Bool("enable", true);
            useVanillaThrowBehaviour = data.Bool("useVanillaThrowBehaviour", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            SceneAs<Level>().Session.SetFlag("bounceModeEnabled", enable);
            SceneAs<Level>().Session.SetFlag("bounceModeUseVanillaThrowBehaviour", useVanillaThrowBehaviour);
        }
    }
}
