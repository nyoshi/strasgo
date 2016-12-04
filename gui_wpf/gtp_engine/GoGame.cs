using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoGame
    {
        public enum RulesType
        {
            normal,
            capture_N
        }

        #region Game is Over Event
        public class GameResultEventArgs : EventArgs
        {
            public GoColor WinnerColor;
            public float Score;
            public float BlackScore;
            public float WhiteScore;
            public bool TimeOut;

            public GameResultEventArgs(GoColor winner, float score, float blackScore, float whiteScore, bool timeOut)
            {
                WinnerColor = winner;
                Score = score;
                BlackScore = blackScore;
                WhiteScore = whiteScore;
                TimeOut = timeOut;
            }
        }

        public event EventHandler<GameResultEventArgs> GameIsOver;
        private void OnGameIsOver(GameResultEventArgs args)
        {
            IsGameOver = true;
            if (GameIsOver != null)
            {
                GameIsOver(this, args);
            }
        }

        #endregion

        public GoClock Clock;       // keep track of the time 
        public GoGameTree Tree;     // keep track of the variations and the game tree
        public GoGameInfo GameInfo; // keep all the game settings (size, time settings, handicap, komi)
        public GoBoard Board;       // keep track of the stones in play, is the result of the stack of turn
        public List<GoTurn> Turns;  // keep track of every turn and affect the board
        public GoPoint KoPoint;
        public GoResult Result;
        public RulesType GameRule;
        public int NumberOfStonesToCapture; // use in the rules type capture 5 but it could be another number than 5 actually

        public bool IsGameOver;

        public GoTurn GetLastTurn() { return Turns.Count > 0 ? Turns.Last() : null; }

        private int m_currentTurn; // start at zero, even with handicap
        private GoColor m_colorToMove;

        public GoColor GetToMove() { return m_colorToMove; }

        private bool m_isPause;
        public bool IsPause
        {
            get { return m_isPause; }
            set
            {
                m_isPause = value;
                if (m_isPause)
                {
                    Clock.Pause();
                    m_currentNode = GetLastTurn();
                }
                else
                {
                    Clock.UnPause();
                }
            }
        }

        private GoTurn m_currentNode; // hold the current node of the game graph
        public GoTurn GetNodeToDisplay() { return m_currentNode; }

        public GoGame(GoGameInfo info)
        {
            GameInfo = info;
            Clock = new GoClock(GameInfo.TimeSettings);
            Tree = new GoGameTree();
            Board = new GoBoard(GameInfo.Size, GameInfo.Handicap);
            Turns = new List<GoTurn>();
            KoPoint = null;
            IsGameOver = false;
            GameRule = RulesType.normal;
            NumberOfStonesToCapture = 5;

            m_currentTurn = 0;
            m_colorToMove = GameInfo.Handicap < 2 ? GoColor.BLACK : GoColor.WHITE;
        }

        // return true if the move is accepted, otherwise false
        // and message indicate why
        public bool PlayMove(GoMove move, ref GoMessageId message)
        {
            bool isValid = false;
            if (m_currentTurn != Turns.Count)
            {
                throw new Exception("You can't play if the state of the game is not the most recent, you might want to create a variation though but it is not implemented yet!");
            }

            if(move.IsPass)
            {
                // this is a pass
                // is it the second one?
                if (Turns.Last().Move.Point == null)
                {
                    // compute score
                    float score = 0.0f;
                    GoColor winner = GoColor.EMPTY;

                    float whiteScore = 0.0f;
                    float blackScore = 0.0f;
                    StrasCouting(out whiteScore, out blackScore);
                    if (whiteScore > blackScore)
                    {
                        winner = GoColor.WHITE;
                        score = whiteScore - blackScore;
                    }
                    else
                    {
                        winner = GoColor.BLACK;
                        score = blackScore - whiteScore;
                    }

                    // two pass the game is over
                    switch (GameRule)
                    {
                        case RulesType.normal:
                            OnGameIsOver(new GameResultEventArgs(winner, score, blackScore, whiteScore, false));
                            break;
                        case RulesType.capture_N:
                            int numberOfCapturesForWhite = GetNumberOfCapturedStonesFor(GoColor.WHITE);
                            int numberOfCapturesForBlack = GetNumberOfCapturedStonesFor(GoColor.BLACK);
                            GoColor moreCaptures = numberOfCapturesForWhite == numberOfCapturesForBlack 
                                ? GoColor.EMPTY 
                                : numberOfCapturesForWhite > numberOfCapturesForBlack 
                                    ? GoColor.WHITE 
                                    : GoColor.BLACK;
                            OnGameIsOver(new GameResultEventArgs(moreCaptures
                            , 0
                            , numberOfCapturesForBlack
                            , numberOfCapturesForWhite
                            , false));
                            break;
                        default:
                            throw new Exception("Error: unsupported rules type!");
                    }

                }

                GoTurn thisTurn = new GoTurn(move, m_currentTurn);
                thisTurn.OldKoPoint = KoPoint;
                // link the turns
                if (GetLastTurn() != null)
                {
                    GetLastTurn().NextTurns.Add(thisTurn);
                    thisTurn.PreviousTurn = GetLastTurn();
                }
                // add to the list of turns
                Turns.Add(thisTurn);
                m_currentTurn++;
                isValid = true;
            }
            else if(move.Color == m_colorToMove)
            {
                // is it the ko point?
                if (KoPoint != null && move.Point == KoPoint)
                {
                    message = GoMessageId.KO_THREAT_FIRST;
                    isValid = false;
                }
                else
                {
                    // is it on an empty space?
                    if (Board.isPointFree(move.Point))
                    {
                        // is it capturing something?
                        List<GoPoint> captured = Board.WillCapture(move);
                        if (captured.Count > 0)
                        {
                            GoTurn thisTurn = new GoTurn(move, m_currentTurn);
                            thisTurn.Killed = captured;
                            thisTurn.OldKoPoint = KoPoint;
                            // link the turns
                            if (GetLastTurn() != null)
                            {
                                GetLastTurn().NextTurns.Add(thisTurn);
                                thisTurn.PreviousTurn = GetLastTurn();
                            }
                            // add to the list of turns
                            Turns.Add(thisTurn);
                            m_currentTurn++;
                            isValid = true;
                        }
                        else
                        {
                            // otherwise is it a suicide?
                            if (Board.IsSuicide(move))
                            {
                                message = GoMessageId.SUICIDE_MOVE;
                                isValid = false;
                            }
                            else
                            {
                                // play the move increment turn counting
                                GoTurn thisTurn = new GoTurn(move, m_currentTurn);
                                thisTurn.OldKoPoint = KoPoint;
                                // link the turns
                                if (GetLastTurn() != null )
                                { 
                                    GetLastTurn().NextTurns.Add(thisTurn);
                                    thisTurn.PreviousTurn = GetLastTurn();
                                }
                                // add to the list of turns
                                Turns.Add(thisTurn);
                                m_currentTurn++;
                                isValid = true;
                            }
                        }
                    }
                    else
                    {
                        message = GoMessageId.ALREADY_A_STONE;
                        isValid = false;
                    }
                }
            }
            else
            {
                // WARNING your move will be ignored
                message = GoMessageId.NOT_COLOR_TURN;
                isValid = false;
            }

            if (isValid)
            {
                Board.Execute(Turns.Last());
                KoPoint = null;
                // did it created a ko?
                if (GetLastTurn().Killed != null && GetLastTurn().Killed.Count == 1)
                {
                    // test if the captured position would be a suicide for the other color
                    GoMove suicideTest = new GoMove(
                        Helper.GetOppositeColor(move.Color),
                        GetLastTurn().Killed[0],
                        false);
                    if( Board.IsSuicide(suicideTest))
                    {
                        // but if it capture exactly one stone back it is a ko
                        List<GoPoint> koTest = Board.WillCapture(suicideTest);
                        if (koTest.Count == 1)
                        {
                            KoPoint = suicideTest.Point;
                        }
                    }
                }
                else
                {
                    // otherwise reinitialise the ko to null
                    KoPoint = null;
                }

                // if it capture and we are playing the capture 5 stones rules
                // we need to check if the game is over
                if (GameRule == RulesType.capture_N && GetLastTurn().Killed != null && GetLastTurn().Killed.Count > 0)
                {
                    if (GetNumberOfCapturedStonesFor(m_colorToMove) >= NumberOfStonesToCapture)
                    {                        
                        OnGameIsOver(new GameResultEventArgs(m_colorToMove
                            , 0
                            , GetNumberOfCapturedStonesFor(GoColor.BLACK)
                            , GetNumberOfCapturedStonesFor(GoColor.WHITE)
                            , false));
                    }
                }

                Clock.NotifyMovePlayed(m_colorToMove);
                m_colorToMove = Helper.GetOppositeColor(m_colorToMove);
            }

            return isValid;
        } // end of PlayMove

        public void UpdateClock()
        {
            bool isTimeOut = Clock.update(m_colorToMove);
            if (isTimeOut)
            {
                // Time out the game is over
                OnGameIsOver(new GameResultEventArgs(Helper.GetOppositeColor(m_colorToMove), 0, 0, 0, true));
            }
        }

        public void StrasCouting(out float whiteScore, out float blackScore)
        {
            whiteScore = GameInfo.Komi;
            blackScore = 0.0f;
            foreach (GoColor color in Board.Stones.Values)
            {
                if (color == GoColor.BLACK)
                {
                    blackScore++;
                }
                else
                {
                    whiteScore++;
                }
            }
        }

        public int GetNumberOfCapturedStonesFor(GoColor color)
        {
            int count = 0;
            foreach (GoTurn turn in Turns)
            {
                if (turn.Move.Color == color && turn.Killed != null)
                {
                    count += turn.Killed.Count;
                }
            }
            return count;
        }

        // step can be negative
        public void GetBoardModifications( ref List<GoPoint> pointsToRemove, ref List<GoMove> moveToAdd, int step )
        {
            pointsToRemove.Clear();
            moveToAdd.Clear();

            // special case when m_currentNode is null but a move has been played
            if (m_currentNode == null && step > 0 && Turns.Count == 1)
            {
                moveToAdd.Add(new GoMove(Turns[0].Move.Color, Turns[0].Move.Point, false) );
                m_currentNode = Turns[0];
                return;
            }


            // going forward
            if (step > 0 && m_currentNode != null)
            {
                for (int i = 0; i < step; ++i)
                {
                    // take 1st variation as default
                    if (m_currentNode.NextTurns.Count > 0)
                    {
                        m_currentNode = m_currentNode.NextTurns[0];
                        if (!m_currentNode.Move.IsPass)
                        {
                            // wanna add this move so if it was asked to be remove, cancel it
                            pointsToRemove.Remove(m_currentNode.Move.Point);
                            moveToAdd.Add(new GoMove(m_currentNode.Move.Color, m_currentNode.Move.Point, false));

                            if (m_currentNode != null && m_currentNode.Killed != null)
                            {
                                foreach (GoPoint point in m_currentNode.Killed)
                                {
                                    // want to remove it? so if wanted to add it, cancel it
                                    List<int> indexToRemove = new List<int>();
                                    for (int j = 0; j < moveToAdd.Count; ++j)
                                    {
                                        if (moveToAdd[j].Point == point)
                                        {
                                            indexToRemove.Add(j);
                                        }
                                    }
                                    foreach (int j in indexToRemove)
                                    {
                                        moveToAdd.RemoveAt(j);
                                    }
                                    pointsToRemove.Add(point);
                                }
                            }
                        }
                    }
                }
            }
            // going backward
            else if (step < 0 && m_currentNode != null)
            {
                for (int i = step; i < 0; ++i)
                {
                    if (m_currentNode.PreviousTurn != null)
                    {
                        if (!m_currentNode.Move.IsPass)
                        {
                            // want to remove it? so if wanted to add it, cancel it
                            List<int> indexToRemove = new List<int>();
                            for (int j = 0; j < moveToAdd.Count; ++j)
                            {
                                if (moveToAdd[j].Point == m_currentNode.Move.Point)
                                {
                                    indexToRemove.Add(j);
                                }
                            }
                            foreach (int j in indexToRemove)
                            {
                                moveToAdd.RemoveAt(j);
                            }

                            pointsToRemove.Add(m_currentNode.Move.Point);
                            if (m_currentNode != null && m_currentNode.Killed != null)
                            {
                                foreach (GoPoint point in m_currentNode.Killed)
                                {
                                    pointsToRemove.Remove(point);
                                    moveToAdd.Add(new GoMove(Helper.GetOppositeColor(m_currentNode.Move.Color), point, false));
                                }
                            }
                        }
                        m_currentNode = m_currentNode.PreviousTurn;
                    }
                }
            }
        }
    }
}
