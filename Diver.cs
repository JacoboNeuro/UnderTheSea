using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderTheSea;
internal class Diver : MonoBehaviour
{
    private static readonly Dictionary<Player, Diver> Divers = [];

    public enum DiveDirection
    {
        NEUTRAL = 0,
        UP = 1,
        DOWN = 2
    }

    
    public Player player;
    public float BaseSwimSpeed { get; private set; }    
    public const float DefaultSwimDepth = 1.6f;
    public const float DivingSwimDepth = 2.2f;
    public const float SprintSwimSpeed = 3f;
    public const float SwimSpeedDelta = 0.25f;

    /// <summary>
    ///     Check if character is a Player and try get cached diver component for them.
    /// </summary>
    /// <param name="character"></param>
    /// <param name="diver"></param>
    /// <returns></returns>
    public static bool TryGetDiver(Character character, out Diver diver)
    {
        return TryGetDiver((Player)character, out diver);
    }

    /// <summary>
    ///     Try get cached diver component for player.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="diver"></param>
    /// <returns></returns>
    public static bool TryGetDiver(Player player, out Diver diver)
    {
        if (player && Divers.TryGetValue(player, out diver))
        {
            return true;
        }
        diver = null;
        return false;
    }


    public void Awake()
    {
        player = GetComponent<Player>();    
        player.m_swimDepth = DefaultSwimDepth;
        BaseSwimSpeed = player.m_swimSpeed;
        Divers.Add(player, this);
    }

    public void ResetSwimDepthIfNotInWater()
    {
        if (!player.InWater())
        {
            ResetSwimDepthToDefault();
        }
    }

    public void ResetSwimDepthToDefault()
    {
        player.m_swimDepth = DefaultSwimDepth; 
    }

    /// <summary>
    ///     In water and able to dive.
    /// </summary>
    /// <returns></returns>
    public bool CanDive()
    {
        return player.InWater() && !player.IsOnGround() && player.IsSwimming();
    }

    /// <summary>
    ///     Player is deep enough underwater to be diving.
    /// </summary>
    /// <returns></returns>
    public bool IsDiving()
    {
        return player.m_swimDepth > DivingSwimDepth;
    }

    /// <summary>
    ///     Is Swimming in water without diving or moving.
    /// </summary>
    /// <returns></returns>
    public bool IsRestingInWater()
    {
        return !IsDiving() && player.IsSwimming() && player.GetVelocity().magnitude < 1.0f;
    }

    /// <summary>
    ///     Accumulate swimming XP if diving
    /// </summary>
    public void UpdateSwimSkill(float dt)
    {
        if (!IsDiving())
        {
            return;
        }
  
        player.m_swimSkillImproveTimer += dt;
        if (player.m_swimSkillImproveTimer > 1f)
        {
            player.m_swimSkillImproveTimer = 0f;
            player.RaiseSkill(Skills.SkillType.Swim);
        }
    }    

    /// <summary>
    ///     Set MoveDir to be m_lookDir if diving.
    /// </summary>
    public void UpdateDiveDirection()
    {
        if (IsDiving())
        {
            player.SetMoveDir(player.m_lookDir);
        }
    }

    /// <summary>
    ///     Move up at a fixed rate.
    /// </summary>
    /// <param name="dt"></param>
    public void Ascend(float dt)
    {
        player.SetMoveDir(player.transform.forward);
        float newDepth = player.m_swimDepth - SwimSpeedDelta * dt;
        player.m_swimDepth = Mathf.Max(newDepth, DefaultSwimDepth);
    }

    /// <summary>
    ///     Updates depth underwater while swimming/diving based on look dir.
    /// </summary>
    public void UpdateDivingDepth(float dt)
    {
        DiveDirection diveDir = GetDiveDirection();
     
        if (diveDir != DiveDirection.NEUTRAL)
        {
            float multiplier = player.m_lookDir.y * player.m_lookDir.y * SwimSpeedDelta;
            multiplier = Mathf.Min(multiplier, 0.025f) * dt;
        
            if (diveDir == DiveDirection.DOWN)
            {
                player.m_swimDepth += multiplier;
            }
            else if (diveDir == DiveDirection.UP)
            {
                float newDepth = player.m_swimDepth - multiplier;
                player.m_swimDepth = Mathf.Max(newDepth, DefaultSwimDepth);
            }
        }
    }

    public DiveDirection GetDiveDirection()
    {
        float y = player.m_lookDir.y;
        if (y < -0.25f)
        {
            return DiveDirection.DOWN;
        }

        if (y > 0.15f)
        {
            return DiveDirection.UP;
        }

        return DiveDirection.NEUTRAL;
    }
}
