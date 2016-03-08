using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class TimeSettings
    {
        private TimeSpan m_mainTime;
        public TimeSpan MainTime
        {
            get { return m_mainTime; }
            set { m_mainTime = value; }
        }

        private TimeSpan m_byoyomi;
        public TimeSpan Byoyomi
        {
            get { return m_byoyomi; }
            set { m_byoyomi = value; }
        }

        private int m_numberOfMovesPerByoyomi;
        public int NumberOfMovesPerByoyomi
        {
            get { return m_numberOfMovesPerByoyomi; }
            set { m_numberOfMovesPerByoyomi = value; }
        }

        public TimeSettings()
        {
            m_mainTime = new TimeSpan(0, 10, 0);
            m_byoyomi = new TimeSpan(0, 0, 30);
            m_numberOfMovesPerByoyomi = 1;
        }

        public TimeSettings(TimeSettings other)
        {
            m_mainTime = new TimeSpan(other.MainTime.Hours, other.MainTime.Minutes, other.MainTime.Seconds);
            m_byoyomi = new TimeSpan(other.Byoyomi.Hours, other.Byoyomi.Minutes, other.Byoyomi.Seconds);
            m_numberOfMovesPerByoyomi = other.NumberOfMovesPerByoyomi;
        }
    }
}
