using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace gui_wpf.UI
{
    public class RoutedMovePlayedEventArgs : RoutedEventArgs
    {
        public GoBoardPoint Position { get; set; }
        public Stone StoneColor { get; set; }

        public RoutedMovePlayedEventArgs(RoutedEvent routedEvent, object source, GoBoardPoint pos, Stone stoneColor)
            : base(routedEvent, source)
        {
            Position = pos;
            StoneColor = stoneColor;
        }
    }
}