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
    public const float DefaultSwimDepth = 1.5f;
    public const float DivingSwimDepth = 2.2f;
    public const float SprintSwimSpeed = 3f;
    public const float SwimSpeedDelta = 1f;  // m/s

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


    public void DrainDivingStamina(float dt)
    {
        float skillFactor = player.m_skills.GetSkillFactor(Skills.SkillType.Swim);
        float num = Mathf.Lerp(player.m_swimStaminaDrainMinSkill, player.m_swimStaminaDrainMaxSkill, skillFactor);
        num += num * player.GetEquipmentSwimStaminaModifier();
        player.m_seman.ModifySwimStaminaUsage(num, ref num);
        player.UseStamina(dt * num * Game.m_moveStaminaRate * UnderTheSea.Instance.UnderwaterRestingStaminaDrainRate.Value);
    }

    /// <summary>
    ///     Accumulate swimming XP if diving
    /// </summary>
    public void UpdateSwimSkill(float dt)
    {
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
        float newDepth = player.m_swimDepth - (SwimSpeedDelta * dt);
        player.m_swimDepth = Mathf.Max(newDepth, DefaultSwimDepth);
    }

    /// <summary>
    ///     Updates depth underwater while swimming/diving based on look dir.
    /// </summary>
    public void UpdateDivingDepth(float dt)
    {
        DiveDirection diveDir = GetDiveDirection();

        if (diveDir == DiveDirection.NEUTRAL)
        {
            return;
        }

        float multiplier = player.m_lookDir.y * player.m_lookDir.y * SwimSpeedDelta * dt;
                    
        if (diveDir == DiveDirection.DOWN)
        {
            player.m_swimDepth += multiplier;
        }
        else if (diveDir == DiveDirection.UP)
        {
            float newDepth = player.m_swimDepth - multiplier;
            player.m_swimDepth = Mathf.Max(newDepth, DefaultSwimDepth);
        }

        if (IsDiving())
        {
            UpdateDiveDirection();
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
