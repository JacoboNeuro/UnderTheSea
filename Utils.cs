using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderTheSea;
internal static class Utils
{
    public static bool TryGetDiver(Character character, out Diver diver)
    {
        if (!IsValidLocalPlayer(character, out Player player))
        {
            diver = null;
            return false;
        }
        return player.TryGetComponent(out diver);
    }

    public static bool TryGetDiver(Player player, out Diver diver)
    {
        if (!IsValidLocalPlayer(player))
        {
            diver = null;
            return false;
        }
        return player.TryGetComponent(out diver);
    }

    public static bool IsValidLocalPlayer(Character character, out Player player)
    {
        if (character && character.IsPlayer() && character == Player.m_localPlayer)
        {
            player = Player.m_localPlayer;
            return true;
        }
 
        player = null;
        return false;
    }

    public static bool IsValidLocalPlayer(Player player)
    {
        return player && player == Player.m_localPlayer;
    }
}
