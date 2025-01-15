using HarmonyLib;
using UnityEngine;

namespace UnderTheSea.Patches;

[HarmonyPatch]
internal static class DivingPatches
{
    //TODO: make sure stamina is drained slowly whenever underwater

    /// <summary>
    ///     Modify swim depth of local player and add Diver componenet.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static void Player_Awake_Prefix(Player __instance)
    {
        __instance.gameObject.AddComponent<Diver>();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
    private static void Prefix(Character __instance, float dt)
    {
        if (!Utils.TryGetDiver(__instance, out Diver diver) || !UnderTheSea.Instance.IsEnvAllowed())
        {
            return;
        }

        diver.ResetSwimDepthIfNotInWater();
        

        if (ZInput.GetButton("Jump") && diver.IsDiving())
        {
            diver.Ascend(dt);
        }
        else if (ZInput.GetButton("Crouch") && diver.CanDive())
        {
            diver.UpdateDivingDepth(dt);
        }
        else if ((__instance.IsOnGround() || !diver.IsDiving()) && !diver.IsRestingInWater())
        {
            diver.ResetSwimDepthToDefault();
        }       
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
    private static void Character_UpdateMotion_Prefix(Character __instance)
    {
        if (!Utils.TryGetDiver(__instance, out Diver diver) || !UnderTheSea.Instance.IsEnvAllowed())
        {
            return;
        }

        // Bug fix for swimming on land glitch - originally __instance.m_swimDepth > 2.5f
        if (diver.IsDiving() && Mathf.Max(0f, __instance.GetLiquidLevel() - __instance.transform.position.y) > Diver.DivingSwimDepth)
        {
            diver.player.m_lastGroundTouch = 0.3f;
            diver.player.m_swimTimer = 0f;
        }
    }


    /// <summary>
    ///     Update swim speed based on how long sprint key has been held.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="dt"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateSwimming))]
    public static void UpdateSwimming_Prefix(Character __instance, float dt)
    {
        if (!Utils.TryGetDiver(__instance, out Diver diver))
        {
            return;
        }

        float multiplier = ZInput.GetButton("Run") ? dt : -dt;
        float swimSpeed = diver.player.m_swimSpeed + (Diver.SwimSpeedDelta * multiplier);
        diver.player.m_swimSpeed = Mathf.Clamp(swimSpeed, diver.BaseSwimSpeed, UnderTheSea.Instance.MaxSwimSpeed.Value);     
    }


    /// <summary>
    ///     Stamina regenerates at half speed while swimming.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="targetVel"></param>
    /// <param name="dt"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnSwimming))]
    public static void Player_OnSwimming_Prefix(Player __instance, Vector3 targetVel, float dt)
    {
        if (!Utils.TryGetDiver(__instance, out Diver diver))
        {
            return;
        }

        if (diver.IsDiving() && targetVel.magnitude < 0.1f)
        {
            diver.DrainDivingStamina(dt);
            diver.UpdateSwimSkill(dt);
        }
        else if (diver.IsRestingInWater() && targetVel.magnitude < 0.1f)
        {
            __instance.m_staminaRegenTimer -= dt;
            
            if (
                __instance.GetStamina() < __instance.GetMaxStamina() && 
                __instance.m_staminaRegenTimer <= -UnderTheSea.Instance.RestingStaminaRegenDelay.Value * __instance.m_staminaRegenDelay
            )
            {
                float skillFactor = __instance.m_skills.GetSkillFactor(Skills.SkillType.Swim);
                float regenSpeed = (1f + skillFactor) * UnderTheSea.Instance.RestingStaminaRegenRate.Value * __instance.m_staminaRegen;
                __instance.m_stamina = Mathf.Min(__instance.GetMaxStamina(), __instance.m_stamina + regenSpeed * dt * Game.m_staminaRegenRate);
            }
        }
    }		
}
