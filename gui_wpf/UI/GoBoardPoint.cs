using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gui_wpf.UI
{
    public struct GoBoardPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public GoBoardPoint(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return (((GoBoardPoint)obj).X == X && ((GoBoardPoint)obj).Y == Y);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public static GoBoardPoint Empty = new GoBoardPoint(-1, -1);
    }
}
