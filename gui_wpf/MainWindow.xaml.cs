using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;
using System.Globalization;

using WPFLocalization;
using gui_wpf.UI;
using GTPEngine;


// TODO
// to fix: when capture 5: player as white wins!
// the message board still says pachi is thinking...

// to fix: if quickly start a game with pachi and exit while he is thinking
// his next move will count for the new game, but the game is not the same anymore! 


namespace gui_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Settings

        private int MainTimeForPachi = 3;
        private TimeSpan ByoyomiForPachi = new TimeSpan(0, 0, 3); // give 3 sec byoyomi for pachi

        #endregion

        #region Screens

        private enum GridOverlay
        {
            menu,
            settings,
            board
        }

        #endregion

        #region Properties

        private GoColor m_colorToPlay;
        public GoColor ColorToPlay
        {
            get { return m_colorToPlay; }
            set
            {
                m_colorToPlay = value;
                if (value == GoColor.BLACK)
                {
                    goBoardPainter.ToPlay = Stone.Black;
                }
                else if (value == GoColor.WHITE)
                {
                    goBoardPainter.ToPlay = Stone.White;
                }
                else
                {
                    throw new Exception("Invalid color to play!");
                }
            }
        }

        private GoEngineWrapper m_engine;
        private App m_app;

        private bool m_isTwoHumanPlayer;
        public bool IsTwoHumanPlayer
        {
            get { return m_isTwoHumanPlayer; }
            private set
            {
                m_isTwoHumanPlayer = value;
                goBoardPainter.IsTwoHumanPlay = value;
                if (!m_isTwoHumanPlayer)
                {
                    GtpEngineState = GtpState.loading;
                    m_app.StartPachi();
                }
            }
        }

        private bool m_isGameRunning;
        public bool IsGameRunning
        {
            get { return m_isGameRunning; }
            set
            {
                m_isGameRunning = value;
                if (m_isGameRunning)
                {
                    m_app.Game.Clock.Start();
                }
                goBoardPainter.DisplayMouseOver = value;
                if (!value)
                {
                    goBoardPainter.DisplayCountedStones = m_app.Game.GameRule == GoGame.RulesType.normal;
                }
            }
        }

        private bool m_isDemonstration = false;
        public bool IsDemonstration
        {
            get { return m_isDemonstration; }
            set
            {
                if (m_isDemonstration)
                {
                    // from true to false
                    // restore last turn configuration
                    // Stop any capturing stones animation
                    goBoardPainter.CapturedStones.Clear();
                    goBoardPainter.KoPoint.X = -1;
                    goBoardPainter.LastMove.X = -1;

                    // just go to the very end (the game will handle the overflow)
                    int step = m_app.Game.Turns.Count;

                    // find stones to add and stones to remove
                    List<GoPoint> pointsToRemove = new List<GoPoint>();
                    List<GoMove> moveToAdd = new List<GoMove>();
                    m_app.Game.GetBoardModifications(ref pointsToRemove, ref moveToAdd, step);

                    foreach (GoPoint point in pointsToRemove)
                    {
                        goBoardPainter.StoneList.Remove(ConvertToBoard(point));
                    }
                    foreach (GoMove move in moveToAdd)
                    {
                        GoBoardPoint boardPoint = ConvertToBoard(move.Point);
                        if( goBoardPainter.StoneList[boardPoint] == null)
                        {
                            goBoardPainter.StoneList.Add(boardPoint, GetStone(move.Color));
                        }
                    }
                    if (m_app.Game.GetLastTurn() != null && !m_app.Game.GetLastTurn().Move.IsPass)
                    {
                        goBoardPainter.LastMove = ConvertToBoard(m_app.Game.GetLastTurn().Move.Point);
                    }
                    
                }

                m_isDemonstration = value;
                checkbox_demo.IsChecked = m_isDemonstration;

                goBoardPainter.DisplayCountedStones = m_isDemonstration ? false : m_app.Game.IsGameOver;
                goBoardPainter.DisplayMouseOver = !m_isDemonstration;
                navigation_menu.Visibility = m_isDemonstration ? Visibility.Visible : Visibility.Collapsed;
                m_app.Game.IsPause = m_isDemonstration;
                goBoardPainter.Redraw();

            }
        }

        private enum GtpState
        {
            loading,
            ready,
            thinking,
            none
        }

        private GtpState m_gtpEngineState;
        private GtpState GtpEngineState
        {
            set
            {
                if (m_gtpEngineState != value)
                {
                    m_gtpEngineState = value;

                    if (!IsTwoHumanPlayer && IsGameRunning)
                    {
                        switch (m_gtpEngineState)
                        {
                            case GtpState.thinking:
                                system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiThinking") + "...";
                                break;
                            case GtpState.loading:
                                system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiGettingReady") + "...";
                                break;
                            case GtpState.ready:
                                system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiReady");
                                break;
                        }
                    }
                }
            }
        }

        private MediaPlayer m_playSound = new MediaPlayer();
        private MediaPlayer m_bowlSound = new MediaPlayer();

        #endregion

        #region Helper

        static Stone GetStone( GoColor color )
        {
            switch(color)
            {
                case GoColor.BLACK: return Stone.Black;
                case GoColor.WHITE: return Stone.White;
                case GoColor.EMPTY: return Stone.Empty;
            }

            return Stone.Empty;
        }

        #endregion

        #region Methods


        public MainWindow()
        {
            InitializeComponent();

            m_app = (App)Application.Current;
            
            // initialise sounds
            m_playSound.Open(new Uri(@"sounds/stone.mp3", UriKind.RelativeOrAbsolute));
            m_bowlSound.Open(new Uri(@"sounds/bowl.mp3", UriKind.RelativeOrAbsolute));

            Uri iconUri = new Uri("pack://application:,,,/strasgo.ico", UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            IsDemonstration = false;

            system_message.Text = "";
            GtpEngineState = GtpState.none;

            m_engine = m_app.GtpEngine;
            m_engine.ResponsePushed += GtpEngine_ResponsePushed;
            m_engine.HasPlayed += GtpEngine_HasPlayed;
            m_engine.CommandPushed += GtpEngine_CommandPushed;

            GtpEngineState = m_colorToPlay != m_app.PlayerColor ? GtpState.thinking : GtpState.ready;

            m_app.GameIsOver += m_app_GameIsOver;

            IsGameRunning = false;

            // TODO save/load user settings!
            slider_handicap.Value = m_app.Game.GameInfo.Handicap;
            slider_time.Value = MainTimeForPachi;

            console_output.Text = String.Empty;
            debug_column.Visibility = Visibility.Hidden;

            capturedPanel.Visibility = System.Windows.Visibility.Collapsed;

            radio_9.IsChecked = true;

            displayMenuOverlay();
            InitUI(MainTimeForPachi);


            // init tick for clock update
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds( 1 );
            timer.Tick += timer_Tick;
            timer.Start();

            this.KeyUp += MainWindow_KeyUp;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (IsGameRunning)
            {
                // update pachi message if needed
                if (m_gtpEngineState == GtpState.loading)
                {
                    system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiGettingReady") + (system_message.Text.EndsWith("...") ? ""
                        : system_message.Text.EndsWith("..") ? "..."
                        : system_message.Text.EndsWith(".") ? ".."
                        : ".");
                }
                else if (m_gtpEngineState == GtpState.thinking)
                {
                    system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiThinking") + (system_message.Text.EndsWith("...") ? ""
                        : system_message.Text.EndsWith("..") ? "..."
                        : system_message.Text.EndsWith(".") ? ".."
                        : ".");

                    // check here if the last mssage was a genmove if not request again!
                    checkIfPachiIsPlaying();

                }
            }

            if (IsGameRunning)
            {
                m_app.Game.UpdateClock();

                // update clock display
                white_clock.Text = m_app.Game.Clock.WhiteInfiniteTime ? "--:--" : m_app.Game.Clock.getWhiteTime().ToString("mm':'ss");
                black_clock.Text = m_app.Game.Clock.BlackInfiniteTime ? "--:--" : m_app.Game.Clock.getBlackTime().ToString("mm':'ss");
            }
            else
            {
                white_clock.Text = "--:--";
                black_clock.Text = "--:--";
            }
        }

        private void displayMenuOverlay()
        {
            showOverlay(GridOverlay.menu);
        }

        private void showOverlay( GridOverlay toShow )
        {
            menuGridOverlay.Visibility = toShow == GridOverlay.menu ? Visibility.Visible : Visibility.Hidden;
            settingsGridOverlay.Visibility = toShow == GridOverlay.settings ? Visibility.Visible : Visibility.Hidden;            
        }

        private void InitUI(int mainTimeInMin)
        {
            IsDemonstration = false;

            // goboard size
            goBoardPainter.BoardSize = radio_9.IsChecked.HasValue && radio_9.IsChecked.Value
                ? 9 
                : 13;

            resign_button.Content = Properties.Resources.ResourceManager.GetString("L_resign");
            slider_time.Visibility = System.Windows.Visibility.Visible;
            slider_time_label.Visibility = System.Windows.Visibility.Visible;

            // get handicap and time settings
            m_app.ClearBoard((int)slider_handicap.Value
                , mainTimeInMin
                , IsTwoHumanPlayer
                , goBoardPainter.BoardSize);

            // Some game related info
            ColorToPlay = m_app.GameInfo.Handicap > 1 ? GoColor.WHITE : GoColor.BLACK;  // that can be white if it is an handicap game!

            // disable pass button now
            pass_button.IsEnabled = IsTwoHumanPlayer || m_colorToPlay == m_app.PlayerColor;

            goBoardPainter.StoneList.Clear();
            goBoardPainter.LastMove.X = -1;
            goBoardPainter.KoPoint.X = -1;
            goBoardPainter.ForbiddenMove.X = -1;            

            white_capture.Text = "0";
            black_capture.Text = "0";

            checkbox_captured.IsEnabled = true;
            //checkbox_captured.IsChecked = false;
            //capturedPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        public static Visibility IsDebug
        {
#if DEBUG
            get { return Visibility.Visible; }
#else
            get { return Visibility.Collapsed; }
#endif
        }

        private GoPoint ConvertFromBoard(GoBoardPoint position)
        {
            // cause the plainter has his Y inverted
            int x = position.X + 1;
            int y = m_app.GameInfo.Size - position.Y;
            return new GoPoint(x, y);
        }

        private GoBoardPoint ConvertToBoard(GoPoint point)
        {
            // cause the plainter has his Y inverted
            int x = point.X - 1;
            int y = m_app.GameInfo.Size - point.Y;
            return new GoBoardPoint(x, y);
        }

        private void Pass(GoColor color)
        {
            // check if it is the player turn
            if (m_isTwoHumanPlayer || m_colorToPlay == color)
            {
                goBoardPainter.ForbiddenMove.X = -1;

                GoMessageId messageId = GoMessageId.OK;
                GoMove move = new GoMove(m_colorToPlay, null, true);

                // check logic and execute turn
                if (m_app.Game.PlayMove(move, ref messageId))
                {
                    // remove ko and last move marker
                    goBoardPainter.KoPoint.X = -1;
                    goBoardPainter.LastMove.X = -1;

                    if (!m_isTwoHumanPlayer)
                    {
                        if (!m_app.Game.IsGameOver)
                        {
                            // send the move played to AI
                            m_engine.Pass(color);

                            // ask AI to play its next move
                            m_app.GenerateMove(Helper.GetOppositeColor(color));

                            GtpEngineState = GtpState.thinking;
                        }
                    }
                    // get ready for next turn
                    ColorToPlay = m_colorToPlay ^ GoColor.WHITE;

                    // disable pass button now
                    pass_button.IsEnabled = IsTwoHumanPlayer || m_colorToPlay == m_app.PlayerColor;

                    goBoardPainter.Redraw();
                }
                else
                {
                    // analyze the message id and warn the player
                    throw new Exception(String.Format("Illegal move played with id {0}", messageId));
                }
            }
            else
            {
                // it is not your turn!
            }
        }


        #endregion

        #region GUI events

        void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
#if !RELEASE
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Key == Key.C)
                {
                    if (white_clock.Visibility == Visibility.Visible)
                    {
                        white_clock.Visibility = Visibility.Collapsed;
                        black_clock.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        white_clock.Visibility = Visibility.Visible;
                        black_clock.Visibility = Visibility.Visible;
                    }
                }
                else if (e.Key == Key.D)
                {
                    if (debug_column.Visibility == Visibility.Hidden)
                        debug_column.Visibility = Visibility.Visible;
                    else
                        debug_column.Visibility = Visibility.Hidden;
                }
                else if (e.Key == Key.S)
                {
                    goBoardPainter.DisplayCountedStones = !goBoardPainter.DisplayCountedStones;
                    goBoardPainter.Redraw();
                }
            }
#endif
        }

        private void console_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (m_engine == null)
                {
                    throw new Exception("No GTP engine defined");
                }

                TextBox textbox = (TextBox)sender;
                m_engine.SendCommand(textbox.Text);
                textbox.Text = String.Empty;
            }
        }


        private void goBoardPainter_MovePlayed(object sender, RoutedMovePlayedEventArgs e)
        {
            if (!IsGameRunning || IsDemonstration)
            {
                return;
            }

            // TODO in demonstration create variation...

            goBoardPainter.ForbiddenMove.X = -1;

            // check if it is the player turn
            if (m_isTwoHumanPlayer || m_colorToPlay == m_app.PlayerColor)
            {
                GoMessageId messageId = GoMessageId.OK;
                GoMove move = new GoMove(m_colorToPlay, ConvertFromBoard(e.Position), false);

                // check logic and execute turn
                if (m_app.Game.PlayMove(move, ref messageId))
                {
                    m_playSound.Stop();
                    m_playSound.Play();

                    GoTurn thisTurn = m_app.Game.GetLastTurn();

                    // play the move
                    goBoardPainter.StoneList.Add(
                        new GoBoardPoint(e.Position.X, e.Position.Y),
                        e.StoneColor);
                    // update ko
                    if (m_app.Game.KoPoint != null)
                    {
                        // draw the ko only if the player try to play it
                        //goBoardPainter.KoPoint = ConvertToBoard(m_app.Game.KoPoint);
                    }
                    else
                    {
                        goBoardPainter.KoPoint.X = -1;
                    }
                    goBoardPainter.CapturedStones.Clear();
                    // remove captured
                    if (thisTurn.Killed != null)
                    {
                        goBoardPainter.CapturedColor = goBoardPainter.ToPlay ^ Stone.White;
                        foreach (GoPoint captured in thisTurn.Killed)
                        {
                            GoBoardPoint point = ConvertToBoard(captured);
                            goBoardPainter.CapturedStones.Add(point);
                            goBoardPainter.StoneList.Remove(point);
                        }
                        goBoardPainter.DrawCapturedStones();
                    }
                    // mark last move
                    goBoardPainter.LastMove = ConvertToBoard(thisTurn.Move.Point);

                    if (!m_isTwoHumanPlayer)
                    {
                        // send the move played to AI
                        m_engine.PlayMove(m_colorToPlay, move.Point.X, move.Point.Y);

                        // ask AI to play its next move
                        m_app.GenerateMove(Helper.GetOppositeColor(m_colorToPlay));

                        GtpEngineState = GtpState.thinking;
                    }
                    else if( !m_app.Game.IsGameOver )
                    {
                        // erase last message
                        system_message.Text = "";
                    }

                    // get ready for next turn
                    ColorToPlay = m_colorToPlay ^ GoColor.WHITE;

                    // disable pass button now
                    pass_button.IsEnabled = IsTwoHumanPlayer || m_colorToPlay == m_app.PlayerColor;

                    updateUIAfterTurn(thisTurn);
                }
                else
                {
                    switch(messageId)
                    {
                        case GoMessageId.ALREADY_A_STONE:
                            system_message.Text = Properties.Resources.ResourceManager.GetString("L_alreadyAStone");
                            goBoardPainter.ForbiddenMove = ConvertToBoard(move.Point);
                            break;
                        case GoMessageId.KO_THREAT_FIRST:
                            system_message.Text = Properties.Resources.ResourceManager.GetString("L_ko");
                            goBoardPainter.ForbiddenMove = ConvertToBoard(move.Point);
                            break;
                        case GoMessageId.NOT_COLOR_TURN:
                            system_message.Text = Properties.Resources.ResourceManager.GetString("L_wrongTurn");
                            break;
                        case GoMessageId.SUICIDE_MOVE:
                            system_message.Text = Properties.Resources.ResourceManager.GetString("L_suicide");
                            goBoardPainter.ForbiddenMove = ConvertToBoard(move.Point);
                            break;
                    }
                }
            }

            goBoardPainter.Redraw();
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            IsGameRunning = true;
            InitUI((int)slider_time.Value);
            system_message.Text = "";

            if (!IsTwoHumanPlayer)
            {
                if (m_engine != null)
                {
                    if (m_colorToPlay != m_app.PlayerColor)
                        GtpEngineState = GtpState.thinking;
                    else
                        GtpEngineState = GtpState.ready;
                }
            }

            if (IsTwoHumanPlayer)
            {
                m_app.Game.Clock.OrginialTime.Byoyomi = new TimeSpan(0, 0, 15); // give 15 sec byoyomi to human players
                m_app.Game.Clock.WhiteInfiniteTime = true;
                m_app.Game.Clock.BlackInfiniteTime = true;
                m_app.Game.Clock.CanLooseOnTime = false;
            }
            else
            {
                if (m_app.PlayerColor == GoColor.BLACK)
                {
                    m_app.Game.Clock.OrginialTime.Byoyomi = ByoyomiForPachi;
                    m_app.Game.Clock.WhiteInfiniteTime = false;
                    m_app.Game.Clock.BlackInfiniteTime = true;
                    m_app.Game.Clock.CanLooseOnTime = true;
                }
                else
                {
                    m_app.Game.Clock.OrginialTime.Byoyomi = ByoyomiForPachi;
                    m_app.Game.Clock.WhiteInfiniteTime = true;
                    m_app.Game.Clock.BlackInfiniteTime = false;
                    m_app.Game.Clock.CanLooseOnTime = true;
                }
            }

            Button clicked = sender as Button;
            if (clicked != null && clicked.Name == "play_5")
            {
                m_app.Game.GameRule = GoGame.RulesType.capture_N;
                capturedPanel.Visibility = System.Windows.Visibility.Visible;
                checkbox_captured.IsChecked = true;
                checkbox_captured.IsEnabled = false;

                // do we need to place a crosscut?
                if (checkbox_crosscut.IsChecked.HasValue && checkbox_crosscut.IsChecked.Value)
                {
                    m_app.PlaceCrosscut();
                }
            }
            else
            {
                m_app.Game.GameRule = GoGame.RulesType.normal;
            }

            // add additional stones
            foreach (KeyValuePair<GoPoint, GoColor> stone in m_app.Game.Board.Stones)
            {
                goBoardPainter.StoneList.Add(ConvertToBoard(stone.Key), ConvertToBoardColor(stone.Value));
            }

            goBoardPainter.Redraw();

            m_app.CatchUpGame();
            showOverlay(GridOverlay.board);
        }

        private void Only_Players_Button_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            IsTwoHumanPlayer = true;
            
            white_clock.Visibility = Visibility.Collapsed;
            black_clock.Visibility = Visibility.Collapsed;

            slider_time.Visibility = System.Windows.Visibility.Hidden;
            slider_time_label.Visibility = System.Windows.Visibility.Hidden;

            showOverlay(GridOverlay.settings);
        }

        private void Vs_Pachi_White_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            goBoardPainter.PlayerColor = Stone.Black;
            m_app.PlayerColor = GoColor.BLACK;
            IsTwoHumanPlayer = false;

            white_clock.Visibility = Visibility.Visible;
            black_clock.Visibility = Visibility.Visible;
            showOverlay(GridOverlay.settings);
        }

        private void Vs_Pachi_Black_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            goBoardPainter.PlayerColor = Stone.White;
            m_app.PlayerColor = GoColor.WHITE;
            IsTwoHumanPlayer = false;

            white_clock.Visibility = Visibility.Visible;
            black_clock.Visibility = Visibility.Visible;

            showOverlay(GridOverlay.settings);
        }

        private void Captured_Button_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            if (capturedPanel.Visibility != System.Windows.Visibility.Visible)
            {
                capturedPanel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                capturedPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Pass_Button_Click(object sender, RoutedEventArgs e)
        {
            if (IsGameRunning)
            {
                m_bowlSound.Stop();
                m_bowlSound.Play();

                Pass(m_colorToPlay);
            }
        }

        private void Resign_Button_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            // TODO result screen anim
            IsGameRunning = false;
            InitUI(MainTimeForPachi);
            displayMenuOverlay();

            GtpEngineState = GtpState.none;
        }

        private void ClearBoard_Button_Click(object sender, RoutedEventArgs e)
        {
            m_bowlSound.Stop();
            m_bowlSound.Play();

            // TODO ask for saving?
            // TODO printing kifu?

            // reinitialise UI
            InitUI(MainTimeForPachi);
            displayMenuOverlay();
        }
        
        private void slider_handicap_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            Console.WriteLine(slider.Value);
        }

        private void slider_time_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            Console.WriteLine(slider.Value);
        }

        private void captured_checked(object sender, RoutedEventArgs e)
        {
            CheckBox control = (CheckBox)sender;
            if (control.IsChecked.Value)
            {
                capturedPanel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                capturedPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void mouse_over_checked(object sender, RoutedEventArgs e)
        {
            CheckBox control = (CheckBox)sender;
            IsDemonstration = control.IsChecked.Value;
        }

        private void Begin_click(object sender, RoutedEventArgs e)
        {
            Navigate(-m_app.Game.Turns.Count);
        }
        private void Previous_click(object sender, RoutedEventArgs e)
        {
            Navigate(-1);
        }

        private void Next_click(object sender, RoutedEventArgs e)
        {
            Navigate(1);
        }

        private void End_click(object sender, RoutedEventArgs e)
        {
            Navigate(m_app.Game.Turns.Count);
        }

        private void Navigate( int step )
        {
            // Stop any capturing stones animation
            goBoardPainter.CapturedStones.Clear();
            goBoardPainter.KoPoint.X = -1;
            goBoardPainter.LastMove.X = -1;
            goBoardPainter.ForbiddenMove.X = -1;

            // find stones to add and stones to remove
            List<GoPoint> pointsToRemove = new List<GoPoint>();
            List<GoMove> moveToAdd = new List<GoMove>();
            m_app.Game.GetBoardModifications(ref pointsToRemove, ref moveToAdd, step);

            foreach (GoPoint point in pointsToRemove)
            {
                goBoardPainter.StoneList.Remove(ConvertToBoard(point));
            }
            foreach (GoMove move in moveToAdd)
            {
                goBoardPainter.StoneList.Add(
                        ConvertToBoard(move.Point),
                        GetStone(move.Color));
            }
            if (m_app.Game.GetNodeToDisplay() != null && !m_app.Game.GetNodeToDisplay().Move.IsPass)
            {
                goBoardPainter.LastMove = ConvertToBoard(m_app.Game.GetNodeToDisplay().Move.Point);
            }
            goBoardPainter.Redraw();
        }

        #region Localization buttons

        private void EN_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.UICulture = new CultureInfo("en-US");
        }

        private void FR_Click(object sender, RoutedEventArgs e)
        {
            LocalizationManager.UICulture = new CultureInfo("fr-FR");
        }

        #endregion

        #endregion

        #region Go game events

        void m_app_GameIsOver(object sender, GoGame.GameResultEventArgs e)
        {

            // do not accept any moves after
            IsGameRunning = false;

            if (!e.TimeOut)
            {
                if (m_app.Game.GameRule == GoGame.RulesType.normal)
                {
                    // display score
                    console_output.Text += String.Format("Black: {2}\nWhite: {3}\n{0}+{1}\n"
                        , e.WinnerColor == GoColor.BLACK ? "B" : "W"
                        , e.Score
                        , e.BlackScore
                        , e.WhiteScore);

                    system_message.Text = String.Format(Properties.Resources.ResourceManager.GetString("L_twoPass") + "\n" + Properties.Resources.ResourceManager.GetString("L_result")
                        , e.WhiteScore, e.WhiteScore > 1 ? "s" : ""
                        , e.BlackScore, e.BlackScore > 1 ? "s" : ""
                        , e.WinnerColor == GoColor.BLACK ? Properties.Resources.ResourceManager.GetString("L_black") : Properties.Resources.ResourceManager.GetString("L_white")
                        , e.Score, e.Score > 1 ? "s" : ""
                        );
                }
                else if (m_app.Game.GameRule == GoGame.RulesType.capture_N)
                {
                    if (m_app.Game.GetLastTurn().Move.IsPass)
                    {
                        system_message.Text = Properties.Resources.ResourceManager.GetString("L_twoPass") + "\n";
                    }
                    else
                    {
                        system_message.Text = string.Empty;
                    }

                    if (e.WinnerColor != GoColor.EMPTY)
                    {
                        system_message.Text += String.Format(Properties.Resources.ResourceManager.GetString("L_score_capture_N")
                            , e.WinnerColor == GoColor.BLACK 
                                ? Properties.Resources.ResourceManager.GetString("L_black") 
                                : Properties.Resources.ResourceManager.GetString("L_white")
                            , e.WinnerColor == GoColor.BLACK 
                                ? e.BlackScore 
                                : e.WhiteScore
                            , e.WinnerColor == GoColor.BLACK
                                ? e.WhiteScore
                                : e.BlackScore
                            );
                    }
                    else
                    {
                        system_message.Text += String.Format(Properties.Resources.ResourceManager.GetString("L_draw")                            
                            , e.BlackScore
                            , e.WhiteScore
                            );
                    }
                }
            }
            else
            {
                console_output.Text += String.Format("{0}+T\n"
                    , e.WinnerColor == GoColor.BLACK ? "B" : "W");

                system_message.Text = String.Format("{0} " + Properties.Resources.ResourceManager.GetString("L_timeResult")
                    , e.WinnerColor == GoColor.BLACK ? Properties.Resources.ResourceManager.GetString("L_black") : Properties.Resources.ResourceManager.GetString("L_white"));
            }

            GtpEngineState = GtpState.none;

            if (m_engine != null)
            {
                // stop pachi from thinking...
                m_engine.ClearBoard();
            }

            // change button resign to quit
            resign_button.Content = Properties.Resources.ResourceManager.GetString("L_playAgain");
            resign_button.IsEnabled = true;

            // draw couting
            goBoardPainter.DisplayCountedStones = (m_app.Game.GameRule == GoGame.RulesType.normal);

            pass_button.IsEnabled = false;
        }

        private Stone ConvertToBoardColor( GoColor color)
        {
            switch( color)
            {
                case GoColor.BLACK: return Stone.Black;
                case GoColor.WHITE: return Stone.White;
                default: return Stone.Empty;
            }
            return Stone.Empty;
        }

        private void checkIfPachiIsPlaying()
        {
            GoColor pachiColor = Helper.GetOppositeColor(m_app.PlayerColor);
            string genMoveCommand = String.Format("genmove {0}", GoEngineWrapper.GetColorLetter(pachiColor));

            if (genMoveCommand != m_lasCommand)
            {
                if (m_engine.isInQueue(genMoveCommand))
                {
                    if (m_engine.CommandsInQueueCount() == 1)
                    {
                        m_engine.ExecuteNextCommand();
                    }
                }
                else
                {
                    m_app.GenerateMove(pachiColor);
                }
            }
        }

        private string m_lasCommand;
        void GtpEngine_CommandPushed(object sender, GoEngineWrapper.ResponseEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                console_output.Text += e.RawResponse + "\n";
                m_lasCommand = e.RawResponse;
            }));
        }

        void GtpEngine_ResponsePushed(object sender, GoEngineWrapper.ResponseEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                console_output.Text += e.RawResponse + "\n";
            }));
        }        

        void GtpEngine_HasPlayed(object sender, GoMove e)
        {
            if (!IsGameRunning)
            {
                return;
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                // if it is a move play it!
                if (m_isTwoHumanPlayer || m_colorToPlay == (m_app.PlayerColor ^ GoColor.WHITE))
                {
                    GoMessageId messageId = GoMessageId.OK;
                    // check logic and execute turn
                    if (m_app.Game.PlayMove(e, ref messageId))
                    {
                        m_playSound.Stop();
                        m_playSound.Play();

                        GoTurn thisTurn = m_app.Game.GetLastTurn();

                        if (thisTurn.Move.IsPass)
                        {
                            // remove ko and last move marker
                            goBoardPainter.KoPoint.X = -1;
                            goBoardPainter.LastMove.X = -1;

                            // because the last move might has ended the game
                            if (IsGameRunning)
                            {
                                system_message.Text = Properties.Resources.ResourceManager.GetString("L_pachiPass");
                                GtpEngineState = GtpState.none;
                            }
                        }
                        else
                        {
                            // convert to board coordinate
                            GoBoardPoint position = ConvertToBoard(e.Point);
                            if (!IsDemonstration)
                            {
                                goBoardPainter.PlayMove(goBoardPainter.ToPlay, position.X, position.Y);
                            }

                            // update ko
                            if (m_app.Game.KoPoint != null)
                            {
                                // no need to draw the ko, only mark it if the player try to play it
                                //goBoardPainter.KoPoint = ConvertToBoard(m_app.Game.KoPoint);
                            }
                            else
                            {
                                goBoardPainter.KoPoint.X = -1;
                            }

                            if (!IsDemonstration)
                            {
                                goBoardPainter.CapturedStones.Clear();
                                // remove captured
                                if (thisTurn.Killed != null)
                                {
                                    goBoardPainter.CapturedColor = goBoardPainter.ToPlay ^ Stone.White;
                                    foreach (GoPoint captured in thisTurn.Killed)
                                    {
                                        GoBoardPoint point = ConvertToBoard(captured);
                                        goBoardPainter.CapturedStones.Add(point);
                                        goBoardPainter.StoneList.Remove(point);
                                    }
                                    goBoardPainter.DrawCapturedStones();
                                }
                                // mark last move
                                goBoardPainter.LastMove = ConvertToBoard(thisTurn.Move.Point);
                            }

                            // because the last move might has ended the game
                            if (IsGameRunning)
                            {
                                // update message
                                system_message.Text = Properties.Resources.ResourceManager.GetString("L_yourTurn");
                                GtpEngineState = GtpState.none;
                            }
                        }

                        // get ready for next turn
                        ColorToPlay = m_colorToPlay ^ GoColor.WHITE;

                        // disable pass button now
                        pass_button.IsEnabled = IsTwoHumanPlayer || m_colorToPlay == m_app.PlayerColor;

                        if (!IsDemonstration)
                        {
                            goBoardPainter.Redraw();
                        }
                        updateUIAfterTurn(thisTurn);
                    }
                    else
                    {
                        // analyze the message id and warn the player
                        // but usually the gtp engine doesn't make errors...
                    }
                }
            }));

        }

        private void updateUIAfterTurn(GoTurn turn)
        {
            // get new capture count
            white_capture.Text = m_app.Game.GetNumberOfCapturedStonesFor(GoColor.WHITE).ToString();
            black_capture.Text = m_app.Game.GetNumberOfCapturedStonesFor(GoColor.BLACK).ToString();
        }

        #endregion

    }
}
