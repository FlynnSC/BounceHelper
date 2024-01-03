using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.BounceHelper {
    [CustomEntity("BounceHelper/BounceHelperTrigger")]
    class BounceHelperTrigger : Trigger {
        private bool enable;
        private bool useVanillaThrowBehaviour;
        private bool disableOnLeave;

        public BounceHelperTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            enable = data.Bool("enable", true);
            useVanillaThrowBehaviour = data.Bool("useVanillaThrowBehaviour", false);
            disableOnLeave = data.Bool("disableOnLeave", false);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            SceneAs<Level>().Session.SetFlag("bounceModeEnabled", enable);
            SceneAs<Level>().Session.SetFlag("bounceModeUseVanillaThrowBehaviour", useVanillaThrowBehaviour);
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (disableOnLeave) {
                SceneAs<Level>().Session.SetFlag("bounceModeEnabled", false);
            }
        }
    }
}
