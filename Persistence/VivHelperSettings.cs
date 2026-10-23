using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Input.Bindings;
using Celeste.Input.Devices.Keyboard;
using Celeste.Input.Devices.GamePads;
using Celeste.Everest.Modules.Settings;
using Celeste.Everest.Settings.Metadata;

namespace Celeste.Mods.VivHelper.Persistence
{
    [RuntimeTypeAlias("VivHelper.VivHelperModuleSettings")]
    [SettingName("modoptions_VivHelperModule")]
    public sealed class VivHelperSettings : EverestModuleSettings
    {
        [DefaultButtonBinding(SourceGamePadButtonFlags.Back, SourceKeys.Tab)]
        public EverestButtonBinding DisplayPlaybacksInLookout { get; set; }

        [SettingName("modoptions_VivHelperModule_FollowerDist")]
        [SettingSubText("modoptions_VivHelperModule_FollowerDist_sub")]
        [SettingRange(1, 128)]
        public int FPDistance { get; set; } = 30;

        [SettingName("modoptions_VivHelperModule_FollowerDist2")]
        [SettingSubText("modoptions_VivHelperModule_FollowerDist2_sub")]
        [SettingRange(0, 30)]
        public int FFDistance { get; set; } = 5;

        public bool MakeClose { get; set; }

        public bool ShowOneUseOnCustomRefills { get; set; }

        public ColorRefillType DecreaseParticles { get; set; } = ColorRefillType.Normal;

        [SettingName("modoptions_VivHelper_RCSL")]
        [SettingSubText("modoptions_VivHelper_RCSL_sub")]
        public bool RCSLines { get; set; }

        [SettingName("modoptions_VivHelper_RCSBW")]
        [SettingSubText("modoptions_VivHelper_RCSBW_sub")]
        public bool ColorblindRCS { get; set; }
    }
}
