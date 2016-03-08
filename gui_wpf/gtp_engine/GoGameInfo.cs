using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoGameInfo
    {
        private int m_size;
        public int Size
        {
            get { return m_size; }
            set { m_size = value; }
        }

        private float m_komi;
        public float Komi
        {
            get { return m_komi; }
            set { m_komi = value; }
        }

        private int m_handicap;
        public int Handicap
        {
            get { return m_handicap; }
            set { m_handicap = value; }
        }

        private TimeSettings m_timeSettings;
        public TimeSettings TimeSettings
        {
            get { return m_timeSettings; }
            set { m_timeSettings = value; }
        }

        public GoGameInfo()
        {
            m_size = 9;
            m_komi = 0.5f;
            m_handicap = 0;
            m_timeSettings = new TimeSettings();
        }
    }
}
