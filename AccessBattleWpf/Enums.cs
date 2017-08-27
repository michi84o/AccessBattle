using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Wpf
{
    /// <summary>
    /// All visual states that a board field might have. Used to toggle visibility with the converter.
    /// </summary>
    [Flags]
    public enum BoardFieldVisualState
    {
        Empty      = 0x000,
        Link       = 0x002,
        Virus      = 0x004,
        LineBoost  = 0x008,
        VirusCheck = 0x010,
        ServerArea = 0x020, // Not used
        Error404   = 0x040,
        Exit       = 0x080,
        Firewall   = 0x100,
        Flipped    = 0x200,
        LBoostStat = 0x400, // Static LineBoost display (for menu)
    }

    /// <summary>
    /// Visual state for fields that contain cards.
    /// </summary>
    public enum BoardFieldCardVisualState
    {
        Empty  = 0x00,
        Blue   = 0x01,
        Orange = 0x02,
    }

    public enum MenuType
    {
        None,
        Welcome,
        NetworkGame,
        WaitForJoin,
        WaitForAccept,
        AcceptJoin,
        Deployment,
        OpponentTurn,
        SwitchCards, // Question for Error 404
        GameOver
    }

    public enum ActionItem
    {
        VirusCheck,
        Firewall,
        LineBoost,
        Error404
    }
}
