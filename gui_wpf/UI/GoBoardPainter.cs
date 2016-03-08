using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

// Sources
// http://www.codeproject.com/Articles/23257/Beginner-s-WPF-Animation-Tutorial

namespace gui_wpf.UI
{
    // Coordinates are in [0,size[
    // the origin is top left
    public class GoBoardPainter : FrameworkElement
    {
        public static readonly RoutedEvent MovePlayedEvent = EventManager.RegisterRoutedEvent("MovePlayed", RoutingStrategy.Bubble, typeof(MovePlayedEventHandler), typeof(GoBoardPainter));
        public static readonly DependencyProperty BoardSizeProperty = DependencyProperty.Register("BoardSize", typeof(int), typeof(GoBoardPainter), new FrameworkPropertyMetadata(9, new PropertyChangedCallback(OnBoardSizeChanged)), new ValidateValueCallback(BoardSizeValidateCallback));
        public static readonly DependencyProperty MouseHoverTypeProperty = DependencyProperty.Register("MouseHoverType", typeof(GoBoardHoverType), typeof(GoBoardPainter), new FrameworkPropertyMetadata(GoBoardHoverType.None, new PropertyChangedCallback(OnMouseHoverTypeChanged)));

        public delegate void MovePlayedEventHandler(object sender, RoutedMovePlayedEventArgs args);

        public event MovePlayedEventHandler MovePlayed
        {
            add { AddHandler(MovePlayedEvent, value); }
            remove { RemoveHandler(MovePlayedEvent, value); }
        }

        private List<Visual> m_Visuals = new List<Visual>();
        private Dictionary<GoBoardPoint, Stone> m_StoneList = new Dictionary<GoBoardPoint, Stone>();
        private ObservableCollection<GoBoardAnnotation> m_AnnotationsList = new ObservableCollection<GoBoardAnnotation>();
        private Stone m_ToPlay = Stone.Black;
        private Stone m_PlayerColor = Stone.Black;
        public Stone PlayerColor
        {
            set { m_PlayerColor = value; }
        }

        public bool IsTwoHumanPlay;
        public GoBoardPoint KoPoint;
        public GoBoardPoint LastMove;
        public GoBoardPoint ForbiddenMove;
        public bool DisplayCountedStones; // count all stones strasbourg-rule
        public bool DisplayMouseOver;

        public List<GoBoardPoint> CapturedStones = new List<GoBoardPoint>();
        public Stone CapturedColor;

        private DrawingVisual m_BoardVisual, m_StonesVisual, m_StarPointVisual, m_CoordinatesVisual, m_AnnotationVisual, m_MouseHoverVisual, m_KoVisual, m_LastMoveVisual, m_CountingVisual, m_capturedStonesVisual, m_forbiddenMoveVisual;
        private Brush m_BlackStoneBrush, m_WhiteStoneBrush, m_BoardBrush, m_StoneShadowBrush, m_BlackStoneShadowBrush, m_WhiteStoneShadowBrush;
        private Pen m_BlackStoneAnnotationPen, m_WhiteStoneAnnotationPen, m_BoardBlackPen, m_BlackPen, m_RedPen;
        private Typeface m_BoardTypeface;

        private Rect m_GoBoardRect;
        private Rect m_GoBoardHitBox;
        private GoBoardPoint m_MousePosition;
        private string[] m_Coordinates = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V" };
        private int m_Border = 20;
        private int m_BoardSize;
        private double m_BoardWidthFactor = 15;
        private double m_BoardHeightFactor = 15;

        public GoBoardPainter()
        {
            Resources.Source = new Uri("pack://application:,,,/gui_wpf;component/GoBoardPainterResources.xaml");

            m_BlackStoneBrush = (Brush)TryFindResource("blackStoneBrush");
            m_WhiteStoneBrush = (Brush)TryFindResource("whiteStoneBrush");
            m_BoardBrush = (Brush)TryFindResource("boardBrush");
            m_StoneShadowBrush = (Brush)TryFindResource("stoneShadowBrush");
            m_WhiteStoneAnnotationPen = (Pen)TryFindResource("whiteStoneAnnotationPen");
            m_BlackStoneAnnotationPen = (Pen)TryFindResource("blackStoneAnnotationPen");
            m_BoardBlackPen = (Pen)TryFindResource("boardBlackPen");
            m_BlackPen = (Pen)TryFindResource("blackPen");
            m_RedPen = (Pen)TryFindResource("redPen");
            m_BlackStoneShadowBrush = (Brush)TryFindResource("blackStoneShadowBrush");
            m_WhiteStoneShadowBrush = (Brush)TryFindResource("whiteStoneShadowBrush");

            m_BoardTypeface = new Typeface("Arial");
            KoPoint.X = -1;
            LastMove.X = -1;
            ForbiddenMove.X = -1;
            DisplayCountedStones = false;
            DisplayMouseOver = true;

            InitializeBoard(this.BoardSize - 1);

            // init tick for clock update
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        #region Draw Methods

        private void DrawBoard()
        {
            m_BoardVisual = new DrawingVisual();

            using (DrawingContext dc = m_BoardVisual.RenderOpen())
            {
                dc.DrawRectangle(m_BoardBrush, new Pen(Brushes.Black, 0.2), new Rect(0, 0, m_BoardSize * m_BoardWidthFactor + m_Border * 2, m_BoardSize * m_BoardHeightFactor + m_Border * 2));
                dc.DrawRectangle(m_BoardBrush, new Pen(Brushes.Black, 0.2), new Rect(m_Border, m_Border, m_BoardSize * m_BoardWidthFactor, m_BoardSize * m_BoardHeightFactor));

                for (int x = 0; x < m_BoardSize; x++)
                {
                    for (int y = 0; y < m_BoardSize; y++)
                    {
                        dc.DrawRectangle(null, m_BoardBlackPen, new Rect(getPosX(x), getPosY(y), m_BoardWidthFactor, m_BoardHeightFactor));
                    }
                }
            }
        }

        public void DrawStones()
        {
            m_BoardVisual.Children.Remove(m_StonesVisual);
            m_StonesVisual = new DrawingVisual();

            using (DrawingContext dc = m_StonesVisual.RenderOpen())
            {
                // all shadows first
                foreach (var item in m_StoneList)
                {
                    double posX = getPosX(item.Key.X);
                    double posY = getPosY(item.Key.Y);

                    dc.DrawEllipse(m_StoneShadowBrush, null, new Point(posX + 1, posY + 1), m_BoardWidthFactor / 2 - 0.15, m_BoardWidthFactor / 2 - 0.15);
                }
                // then stones
                foreach (var item in m_StoneList)
                {
                    double posX = getPosX(item.Key.X);
                    double posY = getPosY(item.Key.Y);

                    dc.DrawEllipse(((item.Value == Stone.White) ? m_WhiteStoneBrush : m_BlackStoneBrush), m_BlackPen, new Point(posX, posY), m_BoardWidthFactor / 2 - 0.15, m_BoardWidthFactor / 2 - 0.15);
                }
            }

            m_BoardVisual.Children.Add(m_StonesVisual);
        }

        private void DrawStarPoints()
        {
            List<Point> starPointList = new List<Point>();

            if (m_BoardSize == 18)
            {
                starPointList.Add(new Point(getPosX(3), getPosY(3)));
                starPointList.Add(new Point(getPosX(3), getPosY(9)));
                starPointList.Add(new Point(getPosX(3), getPosY(15)));
                starPointList.Add(new Point(getPosX(9), getPosY(3)));
                starPointList.Add(new Point(getPosX(9), getPosY(9)));
                starPointList.Add(new Point(getPosX(9), getPosY(15)));
                starPointList.Add(new Point(getPosX(15), getPosY(3)));
                starPointList.Add(new Point(getPosX(15), getPosY(9)));
                starPointList.Add(new Point(getPosX(15), getPosY(15)));
            }
            else if (m_BoardSize == 12)
            {
                starPointList.Add(new Point(getPosX(3), getPosY(3)));
                starPointList.Add(new Point(getPosX(3), getPosY(6)));
                starPointList.Add(new Point(getPosX(3), getPosY(9)));
                starPointList.Add(new Point(getPosX(6), getPosY(3)));
                starPointList.Add(new Point(getPosX(6), getPosY(6)));
                starPointList.Add(new Point(getPosX(6), getPosY(9)));
                starPointList.Add(new Point(getPosX(9), getPosY(3)));
                starPointList.Add(new Point(getPosX(9), getPosY(6)));
                starPointList.Add(new Point(getPosX(9), getPosY(9)));
            }
            else if (m_BoardSize == 8)
            {
                starPointList.Add(new Point(getPosX(2), getPosY(2)));
                starPointList.Add(new Point(getPosX(2), getPosY(6)));
                starPointList.Add(new Point(getPosX(4), getPosY(4)));
                starPointList.Add(new Point(getPosX(6), getPosY(2)));
                starPointList.Add(new Point(getPosX(6), getPosY(6)));
            }

            m_BoardVisual.Children.Remove(m_StarPointVisual);
            m_StarPointVisual = new DrawingVisual();

            using (DrawingContext dc = m_StarPointVisual.RenderOpen())
            {
                starPointList.ForEach(delegate(Point p)
                {
                    dc.DrawGeometry(Brushes.Black, m_BlackPen, new EllipseGeometry(p, 1.2, 1.2));
                });
            }

            m_BoardVisual.Children.Add(m_StarPointVisual);
        }

        private void DrawCoordinates()
        {
            m_BoardVisual.Children.Remove(m_CoordinatesVisual);
            m_CoordinatesVisual = new DrawingVisual();

            using (DrawingContext dc = m_CoordinatesVisual.RenderOpen())
            {
                for (int i = 0; i < m_BoardSize + 1; i++)
                {
                    double posX = 3;
                    double posY = getPosY(i) - 3;

                    dc.DrawText(new FormattedText((m_BoardSize + 1 - i).ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 4, Brushes.Black), new Point(posX, posY));

                    posX = getPosX(i) - 1;
                    posY = getPosY(m_BoardSize) + m_Border / 2;

                    dc.DrawText(new FormattedText(m_Coordinates[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 4, Brushes.Black), new Point(posX, posY));
                }
            }

            m_BoardVisual.Children.Add(m_CoordinatesVisual);
        }

        private void DrawAnnotations()
        {
            m_BoardVisual.Children.Remove(m_AnnotationVisual);
            m_AnnotationVisual = new DrawingVisual();

            using (DrawingContext dc = m_AnnotationVisual.RenderOpen())
            {
                foreach (var anno in m_AnnotationsList)
                {
                    Stone stone = m_StoneList.ContainsKey(anno.Position) ? m_StoneList[anno.Position] : Stone.Empty;
                    Pen annoPen = (stone != Stone.Empty && stone == Stone.Black) ? m_BlackStoneAnnotationPen : m_WhiteStoneAnnotationPen;
                    Brush annoColor = (stone != Stone.Empty && stone == Stone.Black) ? Brushes.White : Brushes.Black;

                    switch (anno.Type)
                    {
                        case GoBoardAnnotationType.Circle:
                            dc.DrawEllipse(Brushes.Transparent, annoPen, new Point(getPosX(anno.Position.X), getPosY(anno.Position.Y)), m_BoardWidthFactor / 4, m_BoardWidthFactor / 4);
                            break;
                        case GoBoardAnnotationType.Rectangle:
                            dc.DrawRectangle(Brushes.Transparent, annoPen, new Rect(new Point(getPosX(anno.Position.X) - m_BoardWidthFactor / 4, getPosY(anno.Position.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                            break;
                        case GoBoardAnnotationType.Label:
                            FormattedText text = new FormattedText(anno.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 8, annoColor);
                            dc.DrawRectangle(stone == Stone.Empty ? m_BoardBrush : Brushes.Transparent, null, new Rect(new Point(getPosX(anno.Position.X) - m_BoardWidthFactor / 4, getPosY(anno.Position.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                            dc.DrawText(text, new Point(getPosX(anno.Position.X) - text.Width / 2, getPosY(anno.Position.Y) - text.Height / 2));
                            break;
                        case GoBoardAnnotationType.Triangle:
                            string first = getPosX(anno.Position.X) + "," + (getPosY(anno.Position.Y) - 5).ToString().Replace(',', '.');
                            string second = (getPosX(anno.Position.X) - 4).ToString().Replace(',', '.') + "," + (getPosY(anno.Position.Y) + 3).ToString().Replace(',', '.');
                            string third = (getPosX(anno.Position.X) + 4).ToString().Replace(',', '.') + "," + (getPosY(anno.Position.Y) + 3).ToString().Replace(',', '.');
                            dc.DrawGeometry(Brushes.Transparent, annoPen, Geometry.Parse("M " + first + " L " + second + " L " + third + " Z"));
                            break;
                        default:
                            break;
                    }
                }
            }

            m_BoardVisual.Children.Add(m_AnnotationVisual);
        }

        private void DrawCountingStones()
        {
            m_BoardVisual.Children.Remove(m_CountingVisual);
            
            if (DisplayCountedStones)
            {
                m_CountingVisual = new DrawingVisual();
                int blackScore = 0;
                int whiteScore = 0;

                using (DrawingContext dc = m_CountingVisual.RenderOpen())
                {
                    for (int j = 0; j < BoardSize; ++j)
                    {
                        for (int i = 0; i < BoardSize; ++i)
                        {
                            GoBoardPoint point = new GoBoardPoint(i, j);
                            if (m_StoneList.ContainsKey(point))
                            {
                                Stone stone = m_StoneList[point];
                                if (stone == Stone.Empty || stone == Stone.Boundary)
                                {
                                    throw new Exception(String.Format("Found a stone with no valid color at point {0},{1}!", point.X, point.Y));
                                }

                                Pen annoPen = m_WhiteStoneAnnotationPen;
                                Brush annoColor = Brushes.Black;
                                String score = String.Empty;
                                if (stone == Stone.Black)
                                {
                                    // overwrite for black
                                    annoPen = m_BlackStoneAnnotationPen;
                                    annoColor = Brushes.White;
                                    blackScore++;
                                    score = String.Format("{0}", blackScore);
                                }
                                else
                                {
                                    whiteScore++;
                                    score = String.Format("{0}", whiteScore);
                                }

                                FormattedText text = new FormattedText(score, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 8, annoColor);
                                dc.DrawText(text, new Point(getPosX(point.X) - text.Width / 2, getPosY(point.Y) - text.Height / 2));
                            }
                        }
                    }
                }
                m_BoardVisual.Children.Add(m_CountingVisual);
            }
        }

        private void DrawKo()
        {
            m_BoardVisual.Children.Remove(m_KoVisual);
            if (KoPoint.X != -1 && KoPoint.Y != -1)
            {
                m_KoVisual = new DrawingVisual();

                using (DrawingContext dc = m_KoVisual.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Transparent, m_RedPen, new Rect(new Point(getPosX(KoPoint.X) - m_BoardWidthFactor / 4, getPosY(KoPoint.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                }

                m_BoardVisual.Children.Add(m_KoVisual);
            }
        }

        private void DrawForbiddenMove()
        {
            m_BoardVisual.Children.Remove(m_forbiddenMoveVisual);
            if (ForbiddenMove.X != -1 && ForbiddenMove.Y != -1)
            {
                m_forbiddenMoveVisual = new DrawingVisual();

                using (DrawingContext dc = m_forbiddenMoveVisual.RenderOpen())
                {
                    // draw a cross
                    string first  = (getPosX(ForbiddenMove.X) + 4).ToString().Replace(',', '.') + "," + (getPosY(ForbiddenMove.Y) - 4).ToString().Replace(',', '.');
                    string second = (getPosX(ForbiddenMove.X) - 4).ToString().Replace(',', '.') + "," + (getPosY(ForbiddenMove.Y) - 4).ToString().Replace(',', '.');
                    string third  = (getPosX(ForbiddenMove.X) - 4).ToString().Replace(',', '.') + "," + (getPosY(ForbiddenMove.Y) + 4).ToString().Replace(',', '.');
                    string fourth = (getPosX(ForbiddenMove.X) + 4).ToString().Replace(',', '.') + "," + (getPosY(ForbiddenMove.Y) + 4).ToString().Replace(',', '.');
                    dc.DrawGeometry(Brushes.Transparent, m_RedPen, Geometry.Parse("M " + first + " L " + third + " M " + second + " L" + fourth));
                }

                m_BoardVisual.Children.Add(m_forbiddenMoveVisual);
            }
        }

        private void DrawLastMove()
        {
            m_BoardVisual.Children.Remove(m_LastMoveVisual);

            if (m_StoneList.Count > 0 && LastMove.X != -1 && LastMove.Y != -1)
            {
                m_LastMoveVisual = new DrawingVisual();
                
                using (DrawingContext dc = m_LastMoveVisual.RenderOpen())
                {
                    Pen penToUse = m_WhiteStoneAnnotationPen;
                    if( m_StoneList[LastMove] == Stone.Black)
                    {
                        penToUse = m_BlackStoneAnnotationPen;
                    }
                    dc.DrawEllipse(Brushes.Transparent, penToUse, new Point(getPosX(LastMove.X), getPosY(LastMove.Y)), m_BoardWidthFactor / 4, m_BoardWidthFactor / 4);
                    //dc.DrawRectangle(Brushes.Transparent, penToUse, new Rect(new Point(getPosX(LastMove.X) - m_BoardWidthFactor / 4, getPosY(LastMove.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                }

                m_BoardVisual.Children.Add(m_LastMoveVisual);
            }
        }

        private void DrawMouseHoverVisual()
        {
            m_BoardVisual.Children.Remove(m_MouseHoverVisual);

            if (!DisplayMouseOver)
            {
                return;
            }

            // Draw the mouse over only if it is player's turn
            if (IsTwoHumanPlay ||
                m_PlayerColor == Stone.Empty ||
                m_PlayerColor == m_ToPlay)
            {
                m_MouseHoverVisual = new DrawingVisual();

                using (DrawingContext dc = m_MouseHoverVisual.RenderOpen())
                {
                    switch (MouseHoverType)
                    {
                        case GoBoardHoverType.Stone:
                            if (m_MousePosition.Equals(GoBoardPoint.Empty) || m_StoneList.ContainsKey(m_MousePosition)) break;
                            double posX = getPosX(m_MousePosition.X);
                            double posY = getPosY(m_MousePosition.Y);

                            dc.DrawEllipse(((m_ToPlay == Stone.White) ? m_WhiteStoneShadowBrush : m_BlackStoneShadowBrush), null, new Point(posX, posY), m_BoardWidthFactor / 2 - 0.15, m_BoardWidthFactor / 2 - 0.15);
                            break;
                        case GoBoardHoverType.None:
                        default:
                            break;
                    }
                }

                m_BoardVisual.Children.Add(m_MouseHoverVisual);
            }
        }

        public void DrawCapturedStones()
        {
            m_BoardVisual.Children.Remove(m_capturedStonesVisual);

            if (CapturedStones.Count > 0)
            {
                // draw stones
                m_capturedStonesVisual = new DrawingVisual();
                using (DrawingContext dc = m_capturedStonesVisual.RenderOpen())
                {
                    // then stones
                    foreach (var item in CapturedStones)
                    {
                        double posX = getPosX(item.X);
                        double posY = getPosY(item.Y);

                        dc.DrawEllipse(((CapturedColor == Stone.White) ? m_WhiteStoneBrush : m_BlackStoneBrush), m_RedPen, new Point(posX, posY), m_BoardWidthFactor / 2 - 0.15, m_BoardWidthFactor / 2 - 0.15);
                    }
                }
                m_BoardVisual.Children.Add(m_capturedStonesVisual);
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (m_capturedStonesVisual != null)
            {
                m_capturedStonesVisual.Opacity -= 0.04f;
            }
        }

        #endregion

        private void InitializeBoard(int boardSize)
        {
            m_Visuals.ForEach(delegate(Visual v) { RemoveVisualChild(v); });

            m_Visuals.Clear();
            m_StoneList.Clear();
            m_AnnotationsList.Clear();
            m_AnnotationsList.CollectionChanged += new NotifyCollectionChangedEventHandler(m_AnnotationsList_CollectionChanged);

            m_BoardSize = boardSize;

            m_GoBoardRect = new Rect(new Size(m_BoardSize * m_BoardWidthFactor, m_BoardSize * m_BoardHeightFactor));
            m_GoBoardHitBox = m_GoBoardRect;
            m_GoBoardHitBox.Inflate((m_BoardWidthFactor / 2), (m_BoardHeightFactor / 2));

            this.Width = m_GoBoardRect.Width + m_Border * 2;
            this.Height = m_GoBoardRect.Height + m_Border * 2;

            DrawBoard();
            DrawCoordinates();
            DrawStarPoints();
            DrawStones();
            DrawKo();
            DrawLastMove();
            DrawCountingStones();
            DrawForbiddenMove();
            DrawMouseHoverVisual();

            m_Visuals.Add(m_BoardVisual);

            m_Visuals.ForEach(delegate(Visual v) { AddVisualChild(v); });
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point pos = e.GetPosition(this);

            if (!m_GoBoardHitBox.Contains(new Point(pos.X - m_Border, pos.Y - m_Border))) return;

            int x = (int)Math.Round((pos.X - m_Border) / (m_GoBoardRect.Width / m_BoardSize));
            int y = (int)Math.Round((pos.Y - m_Border) / (m_GoBoardRect.Height / m_BoardSize));

            try
            {
                RaiseEvent(new RoutedMovePlayedEventArgs(MovePlayedEvent, this, new GoBoardPoint(x, y), m_ToPlay));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void PlayMove(Stone color, int x, int y)
        {
            StoneList.Add(new GoBoardPoint(x, y), color);
            Redraw();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point pos = e.GetPosition(this);

            if (!m_GoBoardHitBox.Contains(new Point(pos.X - m_Border, pos.Y - m_Border)))
            {
                m_MousePosition = GoBoardPoint.Empty;
                DrawMouseHoverVisual();
                return;
            }

            int x = (int)Math.Round((pos.X - m_Border) / (m_GoBoardRect.Width / m_BoardSize));
            int y = (int)Math.Round((pos.Y - m_Border) / (m_GoBoardRect.Height / m_BoardSize));

            m_MousePosition = new GoBoardPoint(x, y);
            DrawMouseHoverVisual();
        }

        private void m_AnnotationsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawAnnotations();
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return m_Visuals.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= m_Visuals.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return m_Visuals[index];
        }

        private double getPosX(double value)
        {
            return m_BoardWidthFactor * value + m_Border;
        }

        private double getPosY(double value)
        {
            return m_BoardHeightFactor * value + m_Border;
        }

        private static void OnBoardSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as GoBoardPainter).InitializeBoard((sender as GoBoardPainter).BoardSize - 1);
        }

        private static void OnMouseHoverTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
        }

        private static bool BoardSizeValidateCallback(object target)
        {
            if ((int)target < 2 || (int)target > 19)
                return false;

            return true;
        }

        public void Redraw()
        {
            DrawStones();
            DrawKo();
            DrawLastMove();
            DrawAnnotations();
            DrawForbiddenMove();
            DrawCountingStones();
        }

        public int BoardSize
        {
            get { return (int)GetValue(BoardSizeProperty); }
            set { SetValue(BoardSizeProperty, value); }
        }

        public GoBoardHoverType MouseHoverType
        {
            get { return (GoBoardHoverType)GetValue(MouseHoverTypeProperty); }
            set { SetValue(MouseHoverTypeProperty, value); }
        }

        public Stone ToPlay
        {
            get { return m_ToPlay; }
            set { m_ToPlay = value; }
        }

        public Dictionary<GoBoardPoint, Stone> StoneList
        {
            get { return m_StoneList; }
            set
            {
                m_StoneList = value;
                DrawStones();
            }
        }

        public ObservableCollection<GoBoardAnnotation> AnnotationList
        {
            get { return m_AnnotationsList; }
            set
            {
                m_AnnotationsList = value;
                DrawAnnotations();
            }
        }
    }
}