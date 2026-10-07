using System.Reflection;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Config;
using Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode.Patches;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;

namespace Defect_s_Data_Expansion.Defect_s_Data_ExpansionCode
{
    //You're recommended but not required to keep all your code in this package and all your assets in the Defect_s_Data_Expansion folder.
    [ModInitializer(nameof(Initialize))]
    public partial class MainFile : Node
    {
        public const string ModId = "Defect_s_Data_Expansion";

        public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = RitsuLibFramework.CreateLogger(ModId);

        public static void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();

            ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

            RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

            ExpansionConfig.Register(ModId);

            var patcher = RitsuLibFramework.CreatePatcher(ModId, "main");
            patcher.RegisterPatch<ContentGateCardsPatch>();
            patcher.RegisterPatch<ExpansionDiskGrantPatch>();
            patcher.RegisterPatch<CharacterSelectToggleInjectPatch>();
            patcher.RegisterPatch<CharacterSelectToggleVisibilityPatch>();
            RitsuLibFramework.ApplyRequiredPatcher(patcher, DisableMod, "模组“鸡煲：数据扩展”的关键补丁无法应用。" + ModId + " 已被禁用。");
        }

        private static void DisableMod()
        {
            ExpansionConfig.EnableExpansion = false;
            Logger.Error("Failed to apply required patches; the expansion content has been disabled for this session.");
        }
    }
}
