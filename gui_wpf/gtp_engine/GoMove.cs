using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoMove : EventArgs
    {
        public GoPoint Point;
        public GoColor Color;
        public bool IsPass;     // if point is null

        public GoMove(GoColor color, GoPoint point, bool isPass)
        {
            Color = color;
            Point = point;
            IsPass = isPass;
        }
    }
}
