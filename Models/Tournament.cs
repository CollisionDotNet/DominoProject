using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.Models
{
    public class Tournament
    {
        private static Tournament instance = null; // Singletone
        public static int GamesPerTurn;
        public static Tournament GetInstance()
        {
            if (instance == null)
                instance = new Tournament();
            return instance;
        }

        public static void LoadInstance(Tournament toLoad)
        {
            instance = toLoad;
        }
        private Tournament() 
        {
        }


        private List<User> allPlayers = null;
        public List<User> AllPlayers
        {
            get
            {
                return allPlayers;
            }
            set
            {
                if (allPlayers == null)
                {
                    allPlayers = value;
                }
            }
        }
        public List<User> ActivePlayers { get; set; }


        public enum Stage
        {
            NotStarted,
            QualifyStage,
            Playoff,
            Ended
        }
        public Stage stage = Stage.NotStarted;


        public List<ViewModels.PlayerQualifyingStageViewModel> QualifyingStageResult;

        
        public class StandingsTree
        {
            public class Round
            {
                public Matchup[] matchups;
            }
            public Round[] rounds;
            public string GetRoundName(int roundNum) // Current tournament's stage name (0 - first, [roundNum - 1] - final)
            {
                if (roundNum == rounds.Length - 1)
                    return "Финал";
                else if (roundNum == rounds.Length - 2)
                    return "Полуфинал";
                else
                    return $"1/{rounds[roundNum].matchups.Length}";
            }
            public string GetRoundDirectory(int roundNum) // Path to wwwroot
            {
                 return $@"{Helper.LogsDirectoryPath}\Round{roundNum + 1}";
            }
            public class Matchup
            {
                public PlayerSlot Top;
                public PlayerSlot Bottom;
                public PlayerSlot WinnerSlot;
                public PlayerSlot LoserSlot; // Only for third place matchup
                public bool played = false;
                public bool resShowed = false;
                public bool[] gamesResReveales;
                public Controllers.GameController.SetScores setScores;
                public string directoryPath;
                public string firstPlayerStringResult => setScores == null || !Top.showScores ? "\u00A0" : setScores.result.firstPlayerResult.ToString();
                public string secondPlayerStringResult => setScores == null || !Bottom.showScores ? "\u00A0" : setScores.result.secondPlayerResult.ToString();
            }
            public class PlayerSlot
            {
                public enum SlotCSSClass
                {
                    nocontent,
                    neutral,
                    win, 
                    loss
                }
                public bool showScores;
                public SlotCSSClass CSSclass;

                public PlayerSlot()
                {
                    showScores = false;
                    CSSclass = SlotCSSClass.nocontent;
                }
                public User user;
                public string GetPlayerName => user == null ? "\u00A0" : user.GetFullName();
            }
            public PlayerSlot Winner;
            public Matchup ConsolationGame;
            public StandingsTree(List<User> players)
            {
                if (players == null)
                {
                    return;
                }
                    if ((players.Count != 0) && ((players.Count & (players.Count - 1)) == 0)) // player.Count is power of 2
                {
                    rounds = new Round[(int)Math.Log2(players.Count)];
                    bool hasConsolation = rounds.Length > 1; // Is there a third place matchup?
                    for (int i = 0; i < rounds.Length; i++)
                    {
                        rounds[i] = new Round();
                        if (i == rounds.Length - 1) // There are two matchups in the final stage: final and third place matchup 
                        {
                            if (hasConsolation)
                            {
                                rounds[i].matchups = new Matchup[2];
                            }
                            else
                            {
                                rounds[i].matchups = new Matchup[1];
                            }
                        } 
                        else
                        {
                            rounds[i].matchups = new Matchup[players.Count / (int)Math.Pow(2, i + 1)];
                        }
                        for (int j = 0; j < rounds[i].matchups.Length; j++)
                        {
                            rounds[i].matchups[j] = new Matchup() { directoryPath = Path.Combine(GetRoundDirectory(i), $"Matchup{j + 1}")};
                            if (i == 0) // First round
                            {
                                rounds[i].matchups[j].Top = new PlayerSlot() { user = players[2 * j], CSSclass = PlayerSlot.SlotCSSClass.neutral };
                                rounds[i].matchups[j].Bottom = new PlayerSlot() { user = players[2 * j + 1], CSSclass = PlayerSlot.SlotCSSClass.neutral };
                            }
                            else
                            {
                                rounds[i].matchups[j].Top = new PlayerSlot();
                                rounds[i].matchups[j].Bottom = new PlayerSlot();
                            }

                        }       
                        
                    }
                    if (hasConsolation)
                    {
                        ConsolationGame = rounds[rounds.Length - 1].matchups[1];
                    }
                    for (int i = 0; i < rounds.Length; i++) // Select winners and set them to WinnerSlot
                    {
                        for (int j = 0; j < rounds[i].matchups.Length; j++)
                        {
                            if (i != rounds.Length - 1) // Not a final
                            {
                                rounds[i].matchups[j].WinnerSlot = j % 2 == 0 ? rounds[i + 1].matchups[j / 2].Top : rounds[i + 1].matchups[j / 2].Bottom;
                            }
                            else // Final
                            {
                                Winner = new PlayerSlot();
                                rounds[i].matchups[0].WinnerSlot = Winner;
                            }
                            if (i == rounds.Length - 2 && hasConsolation) // Semifinals
                            {
                                if (j == 0)
                                    rounds[i].matchups[j].LoserSlot = ConsolationGame.Top;
                                else if (j == 1)
                                    rounds[i].matchups[j].LoserSlot = ConsolationGame.Bottom;
                            }
                        }

                    }
                    if (hasConsolation)
                    {
                        ConsolationGame.WinnerSlot = new PlayerSlot();
                    }
                }

            }                                 
        }
        public StandingsTree Tree { get; set; }
    }
}
