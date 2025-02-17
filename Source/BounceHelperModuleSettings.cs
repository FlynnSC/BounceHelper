using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.BounceHelper {
    public class BounceHelperModuleSettings : EverestModuleSettings {
        [DefaultButtonBinding(Buttons.LeftShoulder, Keys.Space)]
        public ButtonBinding JellyfishDash { get; set; } = new ButtonBinding(Buttons.LeftShoulder, Keys.Space);

        public bool ForceBounceMode { get; set; }

        [SettingSubText($"BounceHelper_{nameof(ReplaceVanillaEntities)}_Hint")]
        public BounceHelperEverywhereSettings ReplaceVanillaEntities { get; set; } = new BounceHelperEverywhereSettings();
    }

    [SettingSubMenu]
    public class BounceHelperEverywhereSettings
    {
        [SettingSubText($"BounceHelper_{nameof(ReplaceBumpers)}_Hint")]
        public bool ReplaceBumpers { get; set; }

        public bool ReplaceDreamBlocks { get; set; }

        public bool ReplaceFallingBlocks { get; set; }

        [SettingSubText($"BounceHelper_{nameof(ReplaceJellyfish)}_Hint")]
        public bool ReplaceJellyfish { get; set; }

        [SettingSubText($"BounceHelper_{nameof(SoulboundJellyfish)}_Hint")]
        public bool SoulboundJellyfish { get; set; }

        [SettingSubText($"BounceHelper_{nameof(ReplaceMoveBlocks)}_Hint")]
        public bool ReplaceMoveBlocks { get; set; }

        public bool ReplaceRefills { get; set; }

        public bool ReplaceSwapBlocks { get; set; }

        public bool ReplaceZipMovers { get; set; }
    }
}
