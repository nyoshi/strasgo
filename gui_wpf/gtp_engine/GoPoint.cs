using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    // this is used with goboard
    // coordianates are in [1, size]
    // Origin is bottom left like pachi
    [DebuggerDisplay("XY = {X},{Y}")]
    public class GoPoint
    {
        public int X;
        public int Y;

        public GoPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GoPoint GetUp()
        {
            GoPoint point = new GoPoint(this.X, this.Y + 1);
            return point;
        }

        public GoPoint GetDown()
        {
            GoPoint point = new GoPoint(this.X, this.Y - 1);
            return point;
        }

        public GoPoint GetLeft()
        {
            GoPoint point = new GoPoint(this.X - 1, this.Y);
            return point;
        }

        public GoPoint GetRight()
        {
            GoPoint point = new GoPoint(this.X + 1, this.Y);
            return point;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            GoPoint right = (GoPoint)obj;
            return this.X == right.X && this.Y == right.Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public static bool operator ==(GoPoint a, GoPoint b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(GoPoint a, GoPoint b)
        {
            return !(a == b);
        }

    }
}
