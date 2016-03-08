using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoTurn
    {
        public GoMove Move;
        public GoPoint OldKoPoint;
        public List<GoPoint> Killed;
        public int TurnNumber;
        public GoTurn PreviousTurn;
        public List<GoTurn> NextTurns;

        public GoTurn(GoMove move, int number)
        {
            Move = move;
            TurnNumber = number;
            PreviousTurn = null;
            NextTurns = new List<GoTurn>();
        }
    }
}
