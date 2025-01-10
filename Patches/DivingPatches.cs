using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace UnderTheSea.Patches;

[HarmonyPatch]
internal static class DivingPatches
{
    private static readonly Dictionary<Player, Diver> Divers = [];

    internal static bool CanDive(Player player)
    {
        return player.InWater() && !player.IsOnGround() && player.IsSwimming();
    }

    private static bool IsValidLocalPlayer(Character character, out Player player)
    {
        if (character && Player.m_localPlayer && ReferenceEquals(character, Player.m_localPlayer))
        {
            player = Player.m_localPlayer;
            return true;
        }

        player = null;
        return false;
    }

    private static bool IsValidLocalPlayer(Player player)
    {
        return player && Player.m_localPlayer && ReferenceEquals(player, Player.m_localPlayer);
    }

    

    /// <summary>
    ///     Modify swim depth of local player and add Diver componenet.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static void Player_Awake_Prefix(Player __instance)
    {
        if (!IsValidLocalPlayer(__instance))
        {
            return;
        }
        __instance.gameObject.AddComponent<Diver>();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
    private static void Prefix(Character __instance, float dt)
    {
        if (!Diver.TryGetDiver(__instance, out Diver diver) || !UnderTheSea.Instance.IsEnvAllowed())
        {
            return;
        }

        diver.ResetSwimDepthIfNotInWater();

        if (ZInput.GetButtonDown("Jump") && diver.IsDiving())
        {
            diver.Ascend(dt);
        }
        else if (ZInput.GetButtonDown("Crouch") && diver.CanDive())
        {
            diver.UpdateSwimSkill(dt);
            diver.UpdateDivingDepth(dt);
            diver.UpdateDiveDirection();
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
        if (!Diver.TryGetDiver(__instance, out Diver diver) || !UnderTheSea.Instance.IsEnvAllowed())
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
        if (!Diver.TryGetDiver(__instance, out Diver diver))
        {
            return;
        }

        float multiplier = ZInput.GetButton("Run") ? dt : -dt;
        float swimSpeed = diver.player.m_swimSpeed + (Diver.SwimSpeedDelta * multiplier);
        diver.player.m_swimSpeed = Mathf.Clamp(swimSpeed, diver.BaseSwimSpeed, Diver.SprintSwimSpeed);     
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
        if (!__instance || targetVel.magnitude > 0.1f)
        {
            return; 
        }

        __instance.m_staminaRegenTimer -= dt;
        if (__instance.GetStamina() < __instance.GetMaxStamina() && __instance.m_staminaRegenTimer <= -0.2f)
        {
            float skillFactor = __instance.m_skills.GetSkillFactor(Skills.SkillType.Swim);
            __instance.m_stamina = Mathf.Min(__instance.GetMaxStamina(), __instance.m_stamina + (1f + skillFactor) * 0.5f * __instance.m_staminaRegen * dt * Game.m_staminaRegenRate);
        }
        
    }
}
