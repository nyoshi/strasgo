using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoClock
    {
        public bool BlackInfiniteTime;
        public bool WhiteInfiniteTime;
        public bool CanLooseOnTime;

        public TimeSettings BlackTime;
        public TimeSettings WhiteTime;
        public TimeSettings OrginialTime;

        private Stopwatch m_stopwatch;

        public GoClock(TimeSettings settings)
        {
            BlackInfiniteTime = false;
            WhiteInfiniteTime = false;
            CanLooseOnTime = true;

            BlackTime = new TimeSettings(settings);
            WhiteTime = new TimeSettings(settings);
            OrginialTime = new TimeSettings(settings);

            m_stopwatch = new Stopwatch();
        }
        
        public TimeSpan getWhiteTime()
        {
            return getRemainingTime(GoColor.WHITE);
        }

        public TimeSpan getBlackTime()
        {
            return getRemainingTime(GoColor.BLACK);
        }

        public TimeSpan getRemainingTime(GoColor color)
        {
            TimeSettings toUse = color == GoColor.BLACK ? BlackTime : WhiteTime;

            if (toUse.MainTime.TotalSeconds > 0)
            {
                return toUse.MainTime;
            }
            else
            {
                return toUse.Byoyomi.TotalMilliseconds > 0 ? toUse.Byoyomi : new TimeSpan();
            }
        }

        // get remaining byoyomi moves to play, 0 if not in byoyomi
        public int getByoyomiMove(GoColor color)
        {
            TimeSettings toUse = color == GoColor.BLACK ? BlackTime : WhiteTime;
            if (toUse.MainTime.TotalSeconds > 0)
            {
                return 0;
            }
            else
            {
                return toUse.NumberOfMovesPerByoyomi;
            }
        }

        // return true if the player run out of time!
        public bool update(GoColor color)
        {
            m_stopwatch.Stop();
            bool colorCanLooseOnTime = CanLooseOnTime;

            // check the color
            if (color == GoColor.BLACK)
            {
                colorCanLooseOnTime &= !BlackInfiniteTime;
            }
            else
            {
                colorCanLooseOnTime &= !WhiteInfiniteTime;
            }

            TimeSettings toUse = color == GoColor.BLACK ? BlackTime : WhiteTime;
            TimeSpan remaingTime = toUse.MainTime;            

            if (toUse.MainTime.TotalSeconds > 0)
            {
                toUse.MainTime -= m_stopwatch.Elapsed;
                remaingTime = toUse.MainTime;
                if (remaingTime.TotalMilliseconds < 0)
                {
                    //toUse.Byoyomi += toUse.MainTime; // report what we put too much on main time
                    toUse.Byoyomi = OrginialTime.Byoyomi;
                    remaingTime = toUse.Byoyomi;
                }
            }
            else
            {
                toUse.Byoyomi -= m_stopwatch.Elapsed;
                remaingTime = toUse.Byoyomi;
            }

            m_stopwatch.Reset();
            m_stopwatch.Start();

            if (remaingTime.TotalMilliseconds >= -1990) // more time for pachi...
            {
                 return false;
            }

            return colorCanLooseOnTime;
        }

        public void Start()
        {
            m_stopwatch.Reset();
            m_stopwatch.Start();
        }

        public void Pause()
        {
            m_stopwatch.Stop();
        }

        public void UnPause()
        {
            m_stopwatch.Start();
        }

        public void NotifyMovePlayed(GoColor color)
        {
            m_stopwatch.Stop();
            // check the color
            TimeSettings toUse = color == GoColor.BLACK ? BlackTime : WhiteTime;

            if (toUse.MainTime.TotalSeconds <= 0)
            {
                toUse.Byoyomi -= m_stopwatch.Elapsed;
                --toUse.NumberOfMovesPerByoyomi;

                if (toUse.NumberOfMovesPerByoyomi <= 0)
                {
                    // reset byoyomi time
                    toUse.Byoyomi = OrginialTime.Byoyomi;
                    toUse.NumberOfMovesPerByoyomi = OrginialTime.NumberOfMovesPerByoyomi;
                }
            }
            else
            {
                toUse.Byoyomi -= m_stopwatch.Elapsed;
            }

            m_stopwatch.Reset();
            m_stopwatch.Start();
        }
    }
}
