using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using GTPEngine;
using System.Windows.Controls;

namespace gui_wpf
{

    // TODO
    // - the installation program doesn't launch the .exe directly
    // - to make it fair the time settings will be the time of the player who has the most
    // - Undo
    // - Navigation through the game
    // - SGF import Export
    // - Early counting feature
    // - Pachi catching a game half way through (once:done, twice? several times? switching color?)
    // - implement capturing animation, stopped if the player plays
    // - prevent playing after counting
    // - restart a game properly

    // - fix pass/pass doesn't disable all pass buttons

    // - add version information
    // - add checking online new version
    // - add auto upgrade
    // - change text when player pass
    // - clock color alert
    // - add localisation!

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Events

        public event EventHandler<GoGame.GameResultEventArgs> GameIsOver;

        #endregion

        #region Properties

        private GoEngineWrapper m_gtpEngine;
        public GoEngineWrapper GtpEngine
        {
            get { return m_gtpEngine; }
        }

        private Exception m_exeption;
        public Exception Exception
        {
            get { return m_exeption; }
        }

        GoGameInfo m_gameInfo;
        public GoGameInfo GameInfo
        {
            get { return m_gameInfo; }
        }

        GoGame m_game;
        public GoGame Game
        {
            get { return m_game; }
        }

        public GoColor PlayerColor;

        #endregion
        
        #region Methods

        public void ClearBoard(int handicap, int minutes, bool isTwoHumanPlayers, int boardSize)
        {
            // set the game settings
            m_gameInfo = new GoGameInfo();
            m_gameInfo.Size = boardSize;
            m_gameInfo.Komi = 0.0f;
            m_gameInfo.Handicap = handicap;
            m_gameInfo.TimeSettings.MainTime = new TimeSpan(0, minutes, 0);
            m_gameInfo.TimeSettings.Byoyomi = new TimeSpan(0, 0, 5);
            m_gameInfo.TimeSettings.NumberOfMovesPerByoyomi = 1;

            m_game = new GoGame(m_gameInfo);
            m_game.GameIsOver += m_game_GameIsOver;

            StartPachi();

            if (m_gtpEngine != null)
            {
                if (!isTwoHumanPlayers)
                {
                    m_gtpEngine.SetGameInfo(m_gameInfo);
                }
            }
        }

        public void PlaceCrosscut()
        {
            if( m_gameInfo.Size == 9 )
            {
                m_game.Board.Stones.Add(new GoPoint(5, 5), GoColor.BLACK);
                m_game.Board.Stones.Add(new GoPoint(5, 6), GoColor.WHITE);
                m_game.Board.Stones.Add(new GoPoint(6, 6), GoColor.BLACK);
                m_game.Board.Stones.Add(new GoPoint(6, 5), GoColor.WHITE);

                m_gtpEngine.PlayMove(GoColor.BLACK, 5, 5);
                m_gtpEngine.PlayMove(GoColor.WHITE, 5, 6);
                m_gtpEngine.PlayMove(GoColor.BLACK, 6, 6);
                m_gtpEngine.PlayMove(GoColor.WHITE, 6, 5);
            }
            else if( m_gameInfo.Size == 13)
            {
                m_game.Board.Stones.Add(new GoPoint(7, 7), GoColor.BLACK);
                m_game.Board.Stones.Add(new GoPoint(7, 8), GoColor.WHITE);
                m_game.Board.Stones.Add(new GoPoint(8, 8), GoColor.BLACK);
                m_game.Board.Stones.Add(new GoPoint(8, 7), GoColor.WHITE);

                m_gtpEngine.PlayMove(GoColor.BLACK, 7, 7);
                m_gtpEngine.PlayMove(GoColor.WHITE, 7, 8);
                m_gtpEngine.PlayMove(GoColor.BLACK, 8, 8);
                m_gtpEngine.PlayMove(GoColor.WHITE, 8, 7);
            }

        }

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
 	        base.OnStartup(e);

            ClearBoard(0, 5, true, 9);
        }

        void m_game_GameIsOver(object sender, GoGame.GameResultEventArgs e)
        {
            if (GameIsOver != null)
            {
                GameIsOver(this, e);
            }
        }

        public void StartPachi()
        {
            // create process
            // if we need an AI
            try
            {
                if (m_gtpEngine == null)
                {
                    GoEngineWrapper.Parameters param;
                    param.name = "Pachi UTC";
                    param.filename = "pachi.exe";
                    param.arguments = "stones_only,threads=4,max_tree_size=768,resign_threshold=0,maximize_score";
                    m_gtpEngine = new GoEngineWrapper(param);

                    m_gtpEngine.ResponsePushed += GtpEngine_ResponsePushed;
                }
            }
            catch (Exception exeption)
            {
                m_exeption = exeption;
            }
        }

        public void CatchUpGame()
        {
            // play the moves
            if (m_game.Turns.Count > 0)
            {
                foreach (GoTurn turn in m_game.Turns)
                {
                    m_gtpEngine.PlayMove(turn.Move.Color, turn.Move.Point.X, turn.Move.Point.Y);
                }
            }

            // now make pachi play
            if (m_game.GetToMove() != PlayerColor)
            {
                GenerateMove(m_game.GetToMove());
            }
        }

        public void GenerateMove(GoColor color)
        {
            m_gtpEngine.GenerateMove(color
                , m_game.Clock.getRemainingTime(color)
                , m_game.Clock.getByoyomiMove(color));
        }

        void GtpEngine_ResponsePushed(object sender, GoEngineWrapper.ResponseEventArgs e)
        {

        }

    }


}
