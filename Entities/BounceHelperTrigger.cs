using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.BounceHelper {
    [CustomEntity("BounceHelper/BounceHelperTrigger")]
    class BounceHelperTrigger : Trigger {
        private bool enable;

        public BounceHelperTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            enable = data.Bool("enable", true);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            SceneAs<Level>().Session.SetFlag("bounceModeEnabled", enable);
        }
    }
}
