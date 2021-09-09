using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.BounceHelper {
    public class BounceHelperSettings : EverestModuleSettings {
        [DefaultButtonBinding(Buttons.LeftShoulder, Keys.Space)]
        public ButtonBinding JellyfishDash { get; set; }
    }
}
