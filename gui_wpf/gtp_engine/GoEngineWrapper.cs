using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTPEngine
{
    public class GoEngineWrapper
    {
        public struct Parameters
        {
            public String name;        // Pachi
            public String filename;    // "pachi.exe"
            public String arguments;   // "stones_only,threads=4,max_tree_size=1024,resign_threshold=0,maximize_score"
        }

        public class ResponseEventArgs : EventArgs
        {
            /// <summary>
            /// Gets or sets the GTP command.
            /// </summary>
            public String RawResponse { get; set; }

            /// <summary>
            /// Initializes a new instance of the ResponseEventArgs class.
            /// </summary>
            /// <param name="command">The response.</param>
            public ResponseEventArgs(String response)
            {
                this.RawResponse = response;
            }
        }

        public event EventHandler<ResponseEventArgs> ResponsePushed;
        public event EventHandler<GoMove> HasPlayed;
        public event EventHandler<ResponseEventArgs> CommandPushed;
        public event EventHandler<EventArgs> Exited;

        #region Attributes

        // Define static variables shared by class methods. 
        private StringBuilder m_output;
        private Process m_goProcess;
        private StreamWriter m_streamWriter;
        private Queue<String> m_commands;
        private bool m_waitingForAnswer;
        private GoColor m_lastColorAsked;

        #endregion

        #region Constructor / Destructor

        public GoEngineWrapper( Parameters param )
        {
            // create the go engine in a process
            m_goProcess = new Process();
            m_goProcess.StartInfo.FileName = param.filename;
            m_goProcess.StartInfo.Arguments = param.arguments;

            // Set UseShellExecute to false for redirection.
            m_goProcess.StartInfo.UseShellExecute = false;
            m_goProcess.StartInfo.CreateNoWindow = true;
            m_goProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // This stream is read asynchronously using an event handler.
            m_goProcess.StartInfo.RedirectStandardOutput = true;
            m_output = new StringBuilder("");

            m_goProcess.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            m_goProcess.Exited += m_goProcess_Exited;
            m_goProcess.ErrorDataReceived += m_goProcess_ErrorDataReceived;
            m_goProcess.Disposed += m_goProcess_Disposed;

            // Redirect standard input as well.  This stream 
            // is used synchronously.
            m_goProcess.StartInfo.RedirectStandardInput = true;

            // Start the process.
            m_goProcess.Start();

            // Use a stream writer to synchronously write the sort input.
            m_streamWriter = m_goProcess.StandardInput;

            // Start the asynchronous read of the sort output stream.
            m_goProcess.BeginOutputReadLine();

            m_waitingForAnswer = false;
            m_commands = new Queue<String>();

            // send first command and verify exe
            SendCommand("name");
                
        }

        void m_goProcess_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine("CRASH");
        }

        void m_goProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("CRASH");
        }

        void m_goProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("CRASH");
        }

        ~GoEngineWrapper()
        {
            // Wait for the sort process to write the sorted text lines.
            m_goProcess.WaitForExit();
        }

        #endregion

        #region Methods

        #region private

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            m_waitingForAnswer = false;

            Console.Write(outLine.Data);
            // Collect the sort command output. 
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                // Send event to notify response
                if (ResponsePushed != null)
                {
                    ResponsePushed(this, new ResponseEventArgs(outLine.Data));
                }

                // TODO proper parsing with regex to avoid errors
                // Analyse the response
                GoMove move = null;
                if (outLine.Data.StartsWith("="))
                {
                    m_waitingForAnswer = false;
                    if (outLine.Data == "= pass")
                    {
                        move = new GoMove(m_lastColorAsked, null, true);
                    }
                    else
                    {
                        // parse the answer
                        String coord = outLine.Data.Substring(2);
                        if (coord.Length == 2)
                        {
                            int x = (int)(coord[0] - 'A') + 1;
                            // because I column doesn't exist
                            if (coord[0] > 'I')
                            {
                                x = x - 1;
                            }

                            int y = int.Parse(coord.Substring(1));
                            GoPoint point = new GoPoint(x, y);
                            move = new GoMove(m_lastColorAsked, point, false);
                        }
                    }
                }
                // Send the play
                if (HasPlayed != null && move != null)
                {
                    HasPlayed(this, move);
                }
            }
            ExecuteNextCommand();
        }

        public void ExecuteNextCommand()
        {
            if (!m_waitingForAnswer)
            {
                if (m_commands.Count > 0)
                {
                    String command = m_commands.Dequeue();
                    if (!String.IsNullOrEmpty(command))
                    {
                        m_streamWriter.WriteLine(command);
                        m_waitingForAnswer = true;
                        // Send event to notify response
                        if (CommandPushed != null)
                        {
                            CommandPushed(this, new ResponseEventArgs(command));
                        }
                    }
                }
            }
        }

        #endregion

        #region public

        public void SendCommand(String command)
        {
            m_commands.Enqueue(command);
            ExecuteNextCommand();
        }

        public static String GetColorLetter(GoColor color)
        {
            String colorLetter = "";
            switch (color)
            {
                case GoColor.BLACK: colorLetter = "B"; break;
                case GoColor.WHITE: colorLetter = "W"; break;
                default:
                    throw new Exception("Invalid color!");
            }
            return colorLetter;
        }

        public void ClearBoard()
        {
            SendCommand("clear_board");
        }

        public void SetGameInfo(GoGameInfo gameInfo)
        {
            SendCommand(String.Format("boardsize {0}", gameInfo.Size));
            SendCommand("clear_board");
            SendCommand(String.Format("komi {0}", gameInfo.Komi));
            SendCommand(String.Format("time_settings {0} {1} {2}"
                , gameInfo.TimeSettings.MainTime.TotalSeconds
                , gameInfo.TimeSettings.Byoyomi.TotalSeconds
                , gameInfo.TimeSettings.NumberOfMovesPerByoyomi));

            if (gameInfo.Handicap > 0)
            {
                //private static String[] HandicapPoint = { "C3", "G7", "C7", "G3", "E5", "C5", "G5", "E3", "E7" };
                String handicapCommand = "set_free_handicap ";
                switch(gameInfo.Handicap)
                {
                    case 2: handicapCommand += "C3 G7"; break;
                    case 3: handicapCommand += "C3 G7 G3"; break;
                    case 4: handicapCommand += "C3 G7 G3 C7"; break;
                    case 5: handicapCommand += "C3 G7 G3 C7 E5"; break;
                    case 6: handicapCommand += "C3 G7 G3 C7 C5 G5"; break;
                    case 7: handicapCommand += "C3 G7 G3 C7 E5 C5 G5"; break;
                    case 8: handicapCommand += "C3 G7 G3 C7 C5 G5 E3 E7"; break;
                    case 9: handicapCommand += "C3 G7 G3 C7 E5 C5 G5 E3 E7"; break;
                    default:
                        // unhandle case
                        handicapCommand = String.Empty;
                        break;
                }
                SendCommand(handicapCommand);
            }
        }

        public void PlayMove(GoColor color, int x, int y)
        {
            int letter = (int)'A';
            letter += x - 1;
            // because the I column doesn't exist
            if (x >= 9)
            {
                letter = letter + 1;
            }
            String posCoord = String.Format("{0}{1}", (char)letter, y);

            SendCommand(String.Format("play {0} {1}", GetColorLetter(color), posCoord));
        }

        public void Pass(GoColor color)
        {
            if (color == GoColor.EMPTY)
            {
                throw new Exception("You can't pass with an invalid color!");
            }
            SendCommand(String.Format("play {0} PASS", color == GoColor.BLACK ? "B" : "W"));
        }

        public void GenerateMove(GoColor color, TimeSpan timeLeft, int numberOfByoyomiMove)
        {
            m_lastColorAsked = color;
            SendCommand(String.Format("time_left {0} {1} {2}"
                , color == GoColor.BLACK ? "b" : "w"
                , (int)timeLeft.TotalSeconds
                , numberOfByoyomiMove));
            SendCommand(String.Format("genmove {0}", GetColorLetter(color)));
        }

        #endregion

        #endregion
    }
}
