using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace gui_wpf.UI
{
    public class GoBoardAnnotation
    {
        private GoBoardAnnotationType m_Type;
        private GoBoardPoint m_Position;
        private string m_Text;

        public GoBoardAnnotation(GoBoardAnnotationType type, GoBoardPoint position)
            : this(type, position, "")
        {

        }

        public GoBoardAnnotation(GoBoardAnnotationType type, GoBoardPoint position, string text)
        {
            m_Type = type;
            m_Position = position;
            m_Text = text;
        }

        public GoBoardAnnotationType Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public GoBoardPoint Position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }
    }
}