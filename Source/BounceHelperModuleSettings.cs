using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.BounceHelper {
    public class BounceHelperModuleSettings : EverestModuleSettings {
        [DefaultButtonBinding(Buttons.LeftShoulder, Keys.Space)]
        public ButtonBinding JellyfishDash { get; set; } = new ButtonBinding(Buttons.LeftShoulder, Keys.Space);

        public bool ForceBounceMode { get; set; }
    }
}