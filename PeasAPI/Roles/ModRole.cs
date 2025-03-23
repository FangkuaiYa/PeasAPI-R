using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Text;
using PeasAPI;
using Reactor.Utilities.Attributes;

namespace PeasAPI.Roles;

[RegisterInIl2Cpp]
public class ModRole : RoleBehaviour
{
    public override bool IsDead => false;

    public override bool CanUse(IUsable usable)
    {
        var role = PlayerControl.LocalPlayer.GetRole();
        if (role != null && role.CanVent)
        {
            this.CanVent = role.CanVent;
            return usable.TryCast<Vent>() != null;
        }

        var console = usable.TryCast<Console>();
        
        if (!role.HasToDoTasks)
            return !(console != null) || console.AllowImpostor;
        
        return console != null;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        /*var customRole = PlayerControl.LocalPlayer.GetCustomRole();
        if (customRole != null)
            return customRole.DidWin(gameOverReason);*/
        PeasAPI.Logger.LogInfo(gameOverReason);
        return false;
    }

    public override void AppendTaskHint(StringBuilder taskStringBuilder)
    {
    }
    
    public override PlayerControl FindClosestTarget()
    {
        if (PlayerControl.LocalPlayer.GetRole() != null)
        {
            List<PlayerControl> playersInAbilityRangeSorted = this.GetPlayersInAbilityRangeSorted(RoleBehaviour.GetTempPlayerList());
            if (playersInAbilityRangeSorted.Count <= 0)
            {
                return null;
            }
            return playersInAbilityRangeSorted.ToArray()[0];
        }
        
        return null;
    }
}