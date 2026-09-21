namespace Game.Operations.Contracts
{
    public readonly struct OperationsRequiredAssemblyReference
    {
        public OperationsRequiredAssemblyReference(string assemblyName, string reason)
        {
            AssemblyName = assemblyName;
            Reason = reason;
        }

        public string AssemblyName { get; }
        public string Reason { get; }
    }

    public static class OperationsAssemblyManifest
    {
        public const string ContractsAssemblyName = "Game.Operations.Contracts";
        public const string TestsAssemblyName = "Game.Operations.Tests.Editor";

        public static readonly string[] ForbiddenExistingAsmdefEdits =
        {
            "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
            "Assets/Game/Scripts/Components/Game.Components.asmdef",
            "Assets/Game/Scripts/Game.Runtime.asmdef",
            "Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef",
            "Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef",
            "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
            "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
            "Assets/Tests/Editor/Game.Tests.Editor.asmdef"
        };

        public static readonly string[] ForbiddenSharedSeams =
        {
            "Assets/Game/Scripts/Persistence/SaveDataModel.cs",
            "Assets/Game/Scripts/Composition/MatchSceneView.OperationMapLaunch.cs",
            "Assets/Game/Scripts/Configs/OperationMapIdentityRules.cs",
            "Assets/Game/Scripts/UI/Contracts/UIRoute.cs"
        };

        public static readonly OperationsRequiredAssemblyReference[] PendingGamePmReferences =
        {
            new("Game.Configs", "Config ScriptableObjects must share Operations enums without a cycle."),
            new("Game.Components", "Named Operations components need contract enums and IDs."),
            new("Game.Runtime", "Strategic/tactical Operations systems consume contract payloads."),
            new("Game.UI.Contracts", "Shipping IUiOperationsGateway belongs here after the reference lands."),
            new("Game.UI.Shell.Ecs", "Operations projection/ARIA intents read contract models."),
            new("Game.Composition", "Catalog and save composition helpers project contract schemas."),
            new("Game.Editor", "Catalog/mission builders validate contract IDs."),
            new("Game.Tests.Editor", "Optional if Game.Operations.Tests.Editor remains the P0 host.")
        };

        public static bool IsContractsAssemblySelfContained => true;
    }
}
