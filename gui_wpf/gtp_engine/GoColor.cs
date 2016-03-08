using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GTPEngine
{
    public enum GoColor
    {
        BLACK = 0,
        WHITE = 1,
        EMPTY = 2
    }

    public static partial class Helper
    {
        public static GoColor GetOppositeColor(GoColor color)
        {
            if (color == GoColor.EMPTY)
            {
                throw new Exception("how EMPTY is not a valid color to ask the opposite!");
            }
            return color ^ GoColor.WHITE;
        }

        public static string GetColorName(GoColor color)
        {
            switch( color)
            {
                case GoColor.BLACK:
                    return "Black";
                case GoColor.WHITE:
                    return "White";
                case GoColor.EMPTY:
                    return "Empty";
                default:
                    throw new Exception("Unknow color");
            }
        }
    }
}
