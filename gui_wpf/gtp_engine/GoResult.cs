using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoResult
    {
        public GoColor WinnerColor; // empty color means a draw
        public int score;           // negativ score means win by resign
    }
}
