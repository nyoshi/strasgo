using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    // Coordinates should be in [1, size]
    // Origin is bottom left like for pachi
    public class GoBoard
    {
        public Dictionary<GoPoint, GoColor> Stones;
        int m_size;

        public GoBoard(int size, int handicap)
        {
            Stones = new Dictionary<GoPoint, GoColor>();
            m_size = size;

            if (handicap > 0)
            {
                if (m_size == 9)
                {
                    if (handicap >= 2)
                    {
                        Stones.Add(new GoPoint(3, 3), GoColor.BLACK);
                        Stones.Add(new GoPoint(7, 7), GoColor.BLACK);
                    }
                    if (handicap >= 3)
                    {
                        Stones.Add(new GoPoint(7, 3), GoColor.BLACK);
                    }
                    if (handicap >= 4)
                    {
                        Stones.Add(new GoPoint(3, 7), GoColor.BLACK);
                    }
                    if (handicap == 5)
                    {
                        Stones.Add(new GoPoint(5, 5), GoColor.BLACK);
                    }
                    if (handicap >= 6)
                    {
                        Stones.Add(new GoPoint(7, 5), GoColor.BLACK);
                        Stones.Add(new GoPoint(3, 5), GoColor.BLACK);
                    }
                    if (handicap == 7)
                    {
                        Stones.Add(new GoPoint(5, 5), GoColor.BLACK);
                    }
                    if (handicap >= 8)
                    {
                        Stones.Add(new GoPoint(5, 3), GoColor.BLACK);
                        Stones.Add(new GoPoint(5, 7), GoColor.BLACK);
                    }
                    if (handicap == 9)
                    {
                        Stones.Add(new GoPoint(5, 5), GoColor.BLACK);
                    }
                }
                else if (m_size == 13)
                {
                    if (handicap >= 2)
                    {
                        Stones.Add(new GoPoint(4, 4), GoColor.BLACK);
                        Stones.Add(new GoPoint(10, 10), GoColor.BLACK);
                    }
                    if (handicap >= 3)
                    {
                        Stones.Add(new GoPoint(10, 4), GoColor.BLACK);
                    }
                    if (handicap >= 4)
                    {
                        Stones.Add(new GoPoint(4, 10), GoColor.BLACK);
                    }
                    if (handicap == 5)
                    {
                        Stones.Add(new GoPoint(7, 7), GoColor.BLACK);
                    }
                    if (handicap >= 6)
                    {
                        Stones.Add(new GoPoint(10, 7), GoColor.BLACK);
                        Stones.Add(new GoPoint(4, 7), GoColor.BLACK);
                    }
                    if (handicap == 7)
                    {
                        Stones.Add(new GoPoint(7, 7), GoColor.BLACK);
                    }
                    if (handicap >= 8)
                    {
                        Stones.Add(new GoPoint(7, 4), GoColor.BLACK);
                        Stones.Add(new GoPoint(7, 10), GoColor.BLACK);
                    }
                    if (handicap == 9)
                    {
                        Stones.Add(new GoPoint(7, 7), GoColor.BLACK);
                    }
                }
                else
                {
                    throw new Exception(String.Format("Dunno how to put {0} handicap stones on that {1} size!", handicap, size));
                }
            }
        }

        public bool isPointFree(GoPoint point)
        {
            return !Stones.ContainsKey(point);
        }

        public List<GoPoint> WillCapture(GoMove move)
        {
            List<GoPoint> captured = new List<GoPoint>();
            GoColor capturedColor = Helper.GetOppositeColor(move.Color);

            // check adjacents
            foreach (GoPoint potentialCaptured in getAdjacents(move.Point))
            {
                if (Stones.ContainsKey(potentialCaptured))
                {
                    if (Stones[potentialCaptured] == capturedColor
                        && !captured.Contains(potentialCaptured))
                    {
                        // check liberties
                        // note: this checking is done before the move is executed
                        // means that actually if this adjacent group has only one liberty
                        // we will play on it, so yes it will be captured
                        List<GoPoint> group, liberties;
                        CheckGroupLiberties(potentialCaptured, capturedColor, out group, out liberties);
                        if (liberties.Count < 2)
                        {
                            // it is captured
                            captured.AddRange(group);
                        }
                    }
                }
            }

            return captured;
        }

        public bool IsSuicide(GoMove move)
        {
            List<GoPoint> group, liberties;
            CheckGroupLiberties(move.Point, move.Color, out group, out liberties);

            return liberties.Count == 0;
        }

        public void CheckGroupLiberties(GoPoint point, GoColor color, out List<GoPoint> group, out List<GoPoint> liberties)
        {
            group = new List<GoPoint>();
            liberties = new List<GoPoint>();
            
            if (color != GoColor.EMPTY)
            {
                CheckGroupLiberties_rec(point, color, ref group, ref liberties);
            }
        }

        private void CheckGroupLiberties_rec(GoPoint point, GoColor color, ref List<GoPoint> alreadyParsed, ref List<GoPoint> liberties)
        {
            alreadyParsed.Add(point);

            foreach (GoPoint adjacent in getAdjacents(point))
            {
                if( !alreadyParsed.Contains(adjacent))
                {
                    if (Stones.ContainsKey(adjacent))
                    {
                        if (Stones[adjacent] == color)
                        {
                            CheckGroupLiberties_rec(adjacent, color, ref alreadyParsed, ref liberties);
                        }
                    }
                    else if( !liberties.Contains(adjacent))
                    {
                        liberties.Add(adjacent);
                    }
                }
            }
        }

        public void Execute(GoTurn turn)
        {
            if (!turn.Move.IsPass)
            {
                // add played stone
                Stones.Add(turn.Move.Point, turn.Move.Color);
                // remove captured
                if (turn.Killed != null)
                {
                    foreach (GoPoint point in turn.Killed)
                    {
                        Stones.Remove(point);
                    }
                }
            }
        }

        public List<GoPoint> getAdjacents(GoPoint point)
        {
            if (!isOnBoard(point))
            {
                throw new Exception("There is no point checking a move which is not on board!");
            }

            List<GoPoint> adjacents = new List<GoPoint>();

            if (isOnBoard(point.GetUp()))
            {
                adjacents.Add(point.GetUp());
            }
            if (isOnBoard(point.GetRight()))
            {
                adjacents.Add(point.GetRight());
            }
            if (isOnBoard(point.GetDown()))
            {
                adjacents.Add(point.GetDown());
            }
            if (isOnBoard(point.GetLeft()))
            {
                adjacents.Add(point.GetLeft());
            }

            return adjacents;
        }

        // Coordinates should be in [1, size]
        // Origin is bottom left like for pachi
        public bool isOnBoard(GoPoint point)
        {
            if (point.X < 1 ||
                point.Y < 1 ||
                point.X > m_size ||
                point.Y > m_size)
            {
                return false;
            }

            return true;
        }
    }
}
