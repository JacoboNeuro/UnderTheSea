using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace UnderTheSea.Patches;

[HarmonyPatch]
internal static class UseEquipPatches
{

    // Call to check if IsSwimming() && !IsOnGround()
    // Call to check if !IsSwimming() || IsOnGround()
    // Missing the last line as it swaps between new CodeMatch(OpCodes.Brtrue) and new CodeMatch(OpCodes.Brfalse)
    private static readonly CodeMatch[] CodeMatches = [
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.IsSwimming))),
        new CodeMatch(OpCodes.Brfalse),
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.IsOnGround))), 
    ];

    // Account for the 1 missing line.
    private static readonly int InstructionMatchCount = CodeMatches.Length + 1;
    
    private static bool ShouldHideItem()
    {
        return !UnderTheSea.Instance.UseEquipInWater.Value;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
    private static IEnumerable<CodeInstruction> UpdateEquipment_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        string methodName = "Humanoid.UpdateEquipment";
        // Search Instructions for Call to IsSwimming() and IsOnGround() using CodeMatches
        CodeMatcher codeMatcher = new(instructions);

        codeMatcher.MatchStartForward(CodeMatches).ThrowIfNotMatch($"Failed to find match in {methodName}!");
        codeMatcher.Advance(InstructionMatchCount); // move to after the match (account for missing line at end)

        return codeMatcher.InsertAndAdvance(
            new List<CodeInstruction>()
            {
                Transpilers.EmitDelegate(ShouldHideItem),
                new(OpCodes.Brfalse, codeMatcher.InstructionAt(-1).operand) // Get label from previous instruction
            }
        )
        .ThrowIfInvalid($"Failed to patch {methodName}!")
        .InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static IEnumerable<CodeInstruction> EquipItem_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        string methodName = "Humanoid.EquipItem";
        // Search Instructions for Call to IsSwimming() and IsOnGround() using CodeMatches
        CodeMatcher codeMatcher = new(instructions);

        codeMatcher.MatchStartForward(CodeMatches).ThrowIfNotMatch($"Failed to find match in {methodName}!");
        codeMatcher.Advance(InstructionMatchCount); // move to after the match (account for missing line at end)

        return codeMatcher.InsertAndAdvance(
            new List<CodeInstruction>()
            {
                Transpilers.EmitDelegate(ShouldHideItem),
                new(OpCodes.Brfalse, codeMatcher.InstructionAt(-1).operand) // Get label from previous instruction
            }
        )
        .ThrowIfInvalid($"Failed to patch {methodName}!")
        .InstructionEnumeration();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static IEnumerable<CodeInstruction> UpdatePlayer_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Remove calls to !IsSwimming() || IsOnGround() so the show item shortcut can run.
        return new CodeMatcher(instructions)
            .MatchStartForward(CodeMatches)
            .Advance(1) // Move to first line of matching code
            .RemoveInstructions(InstructionMatchCount)
            .ThrowIfInvalid("Failed to patch Player.Update!")
            .InstructionEnumeration();
    }
}
