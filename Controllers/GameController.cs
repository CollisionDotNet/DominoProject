using DominoProject.Models;
using DominoProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using DominoProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;

namespace DominoProject.Controllers
{
    [Authorize(Roles = "admin")]
    public class GameController : Controller
    {
        private UserContext database;
        public GameController(UserContext userContext) // Setting db context object using dependency injection
        {
            database = userContext;
        }
        public IActionResult Info()
        {                                  
            return View();
        }
        public class SetScores // Datatype to store game set results
        {
            public enum ResultContext { OK, BadFile, Cheater }; // Game was ok, there was a bad player file or one of the players made an impossible move
            public ResultContext firstPlayerResultContext; // Same for separate players
            public ResultContext secondPlayerResultContext;
            public readonly User firstPlayer;
            public readonly User secondPlayer;
            public readonly int gamesPerSide; // Specifies games amount for every side (f.e. 10 stands for 10 games on every side, 20 in total)
            public (int firstPlayerScore, int secondPlayerScore)[] scores { get; set; } // Scores array for every game in set

            public (int firstPlayerResult, int secondPlayerResult) result
            {
                get
                {
                    return ComputeResult();
                }
            }
            public SetScores(User first, User second, int gamesPerSide) // Init this scores set
            {
                firstPlayer = first;
                secondPlayer = second;
                this.gamesPerSide = gamesPerSide;
                scores = new (int, int)[2 * gamesPerSide];
                firstPlayerResultContext = ResultContext.OK;
                secondPlayerResultContext = ResultContext.OK;
            }
            private (int, int) ComputeResult() // Compute results out of every game scores
            {
                (int, int) res = (0, 0);
                for (int i = 0; i < 2 * gamesPerSide; i++)
                {
                    res.Item1 += scores[i].firstPlayerScore;
                    res.Item2 += scores[i].secondPlayerScore;
                }
                return res;
            }
        }

        private static async Task<(Assembly assembly, string brokenFilePath)> CreateAssembly(string MTablePath, string FPlayerPath, string SPlayerPath) // Uses reflection to create assembly from two players files and master file (MTable)
        {
            Assembly assembly = null;
            string brokenFilePath = "";

            string MTableContent = await System.IO.File.ReadAllTextAsync(MTablePath);
            string FPlayerContent = await System.IO.File.ReadAllTextAsync(FPlayerPath);
            string SPlayerContent = await System.IO.File.ReadAllTextAsync(SPlayerPath);

            SyntaxTree MTableSyntaxTree = CSharpSyntaxTree.ParseText(MTableContent);
            SyntaxTree FPlayerSyntaxTree = CSharpSyntaxTree.ParseText(FPlayerContent);
            SyntaxTree SPlayerSyntaxTree = CSharpSyntaxTree.ParseText(SPlayerContent);

            string assemblyName = Path.GetRandomFileName();
            var refPaths = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(Console).GetTypeInfo().Assembly.Location,
                typeof(File).GetTypeInfo().Assembly.Location,
                typeof(KeyValuePair).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(System.Linq.Enumerable).GetTypeInfo().Assembly.Location), "System.Core.dll"),
                typeof(Enumerable).GetTypeInfo().Assembly.Location
            };
            MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { MTableSyntaxTree, FPlayerSyntaxTree, SPlayerSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {                   
                        // Find file with uncompilable code
                        if (diagnostic.ToString().Contains("MFPlayer"))
                            brokenFilePath = FPlayerPath;
                        else if(diagnostic.ToString().Contains("MSPlayer"))
                            brokenFilePath = SPlayerPath;
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                }
            }
            return (assembly, brokenFilePath);
        }

        private async Task<SetScores> LaunchGame(User firstPlayer, User secondPlayer, int gamesPerSide, string logsDirectory) // Async game organizing among two players
        {

            SetScores results = new SetScores(firstPlayer, secondPlayer, gamesPerSide); // Creating empty scores set among two players

            (Assembly assembly, string brokenFilePath) = await CreateAssembly(Helper.MTablePath, firstPlayer.FPlayerFilePath, secondPlayer.SPlayerFilePath); // Compiling assembly
            if (assembly == null) // Then there was a broken file
            {
                if (firstPlayer.FPlayerFilePath == brokenFilePath)
                    results.firstPlayerResultContext = SetScores.ResultContext.BadFile;
                else if (secondPlayer.SPlayerFilePath == brokenFilePath)
                    results.secondPlayerResultContext = SetScores.ResultContext.BadFile;
                return results;
            }
            Type type = assembly.GetType("DominoC.MTable");
            object[] parametersArray = new object[] { 0, gamesPerSide, logsDirectory, firstPlayer.GetFullName(), secondPlayer.GetFullName() };
            MethodInfo methodInfo = type.GetMethod("LaunchOrFindCheater"); // Executing LaunchOrFindCheater() from DominoC.MTable
            object classInstance = Activator.CreateInstance(type, null);
            
            ValueTuple<(int, int)[], int> output = (ValueTuple<(int, int)[], int>)methodInfo.Invoke(classInstance, parametersArray); // Get game results


            if (output.Item2 != 0) // Find cheaters if there were any of them
            {
                if (output.Item2 == 1)
                    results.firstPlayerResultContext = SetScores.ResultContext.Cheater;
                else if (output.Item2 == 2)
                    results.secondPlayerResultContext = SetScores.ResultContext.Cheater;
                return results;
            }

            for (int i = 0; i < gamesPerSide; i++) // Get scores
            {
                results.scores[i].firstPlayerScore = output.Item1[i].Item1;
                results.scores[i].secondPlayerScore = output.Item1[i].Item2;
            }

            (assembly, brokenFilePath) = await CreateAssembly(Helper.MTablePath, firstPlayer.SPlayerFilePath, secondPlayer.FPlayerFilePath); // Do the same after switching sides
            if (assembly == null)
            {
                if (firstPlayer.FPlayerFilePath == brokenFilePath)
                    results.firstPlayerResultContext = SetScores.ResultContext.BadFile;
                else if (secondPlayer.SPlayerFilePath == brokenFilePath)
                    results.secondPlayerResultContext = SetScores.ResultContext.BadFile;
                return results;
            }
            type = assembly.GetType("DominoC.MTable");
            parametersArray = new object[] { gamesPerSide, gamesPerSide, logsDirectory, secondPlayer.GetFullName(), firstPlayer.GetFullName() };
            methodInfo = type.GetMethod("LaunchOrFindCheater");
            classInstance = Activator.CreateInstance(type, null);
            output = (ValueTuple<(int, int)[], int>)methodInfo.Invoke(classInstance, parametersArray);

            if (output.Item2 != 0)
            {
                if (output.Item2 == 1)
                    results.secondPlayerResultContext = SetScores.ResultContext.Cheater;
                else if (output.Item2 == 2)
                    results.firstPlayerResultContext = SetScores.ResultContext.Cheater;
                return results;
            }

            for (int i = 0; i < gamesPerSide; i++)
            {
                results.scores[gamesPerSide + i].firstPlayerScore = output.Item1[i].Item2;
                results.scores[gamesPerSide + i].secondPlayerScore = output.Item1[i].Item1;
            }

            return results;
        }

        public async Task<IActionResult> DoQualifyingRound() // Set qualifying round for every player against a bot
        {
            // All users with uploaded files are participating
            Tournament.GetInstance().AllPlayers = database.Users.Where(u => !string.IsNullOrWhiteSpace(u.FPlayerFilePath)).ToList();
            Tournament.GetInstance().ActivePlayers = Tournament.GetInstance().AllPlayers;
            Tournament.GetInstance().stage = Tournament.Stage.QualifyStage;
            // Logs
            Directory.CreateDirectory($"{Helper.LogsDirectoryPath}\\Qualify Stage");
            foreach (User user in Tournament.GetInstance().ActivePlayers)
            {
                Directory.CreateDirectory($"{Helper.LogsDirectoryPath}\\Qualify Stage\\{user.Name} vs Dummy");
                // Player vs Dummy
                SetScores curPlayerScores = await LaunchGame(user, Models.User.Dummy, 10, $"{Helper.LogsDirectoryPath}\\Qualify Stage\\{user.Name} vs Dummy");
                if(curPlayerScores.firstPlayerResultContext == SetScores.ResultContext.OK)
                {
                    user.QualifyingStageScores = curPlayerScores.result.firstPlayerResult;
                }
                else
                {
                    user.QualifyingStageScores = Helper.BadFileOrCheaterScores * ((int)curPlayerScores.firstPlayerResultContext);
                }
            }
            // Results processing
            Tournament.GetInstance().ActivePlayers = Tournament.GetInstance().ActivePlayers.OrderBy(u => u.QualifyingStageScores).ToList();
            Tournament.GetInstance().QualifyingStageResult = new List<PlayerQualifyingStageViewModel>();
            foreach (User player in Tournament.GetInstance().ActivePlayers)
            {
                // Passing results to Views.QualifyResults
                Tournament.GetInstance().QualifyingStageResult.Add(new PlayerQualifyingStageViewModel
                {
                    Name = player.Name,
                    Surname = player.Surname,
                    Group = player.Group,
                    QualifyingStageScores = player.QualifyingStageScores,
                    ResultContext = player.QualifyingStageScores >= Helper.BadFileOrCheaterScores ? (SetScores.ResultContext)(player.QualifyingStageScores / Helper.BadFileOrCheaterScores) : SetScores.ResultContext.OK
                });
            }
            return RedirectToAction("QualifyResults");
        }

        public IActionResult QualifyResults()
        {
            //Qualifying stage results view
            return View(Tournament.GetInstance().QualifyingStageResult);
        }
        // <count> players are to playoff
        public IActionResult StartPlayoff(int count)
        {
            if (Tournament.GetInstance().stage == Tournament.Stage.QualifyStage)
            {
                Tournament.GetInstance().stage = Tournament.Stage.Playoff;
                Tournament.GetInstance().ActivePlayers = Tournament.GetInstance().ActivePlayers.OrderBy(u => u.QualifyingStageScores).ToList();
                Tournament.GetInstance().ActivePlayers = Tournament.GetInstance().ActivePlayers.Take(count).ToList();
                Tournament.GetInstance().Tree = new Tournament.StandingsTree(Tournament.GetInstance().ActivePlayers);
            }
            return RedirectToAction("Playoff");
        }
        // Organizes playoff matchup between two players and saves its result
        public async Task<IActionResult> PlayMatchup(int roundNum, int matchupNum)
        {
            Directory.CreateDirectory(Tournament.GetInstance().Tree.GetRoundDirectory(roundNum));
            Tournament.StandingsTree.Matchup matchup = Tournament.GetInstance().Tree.rounds[roundNum].matchups[matchupNum];
            do
            {
                Directory.CreateDirectory(matchup.directoryPath);
                matchup.setScores = await LaunchGame(matchup.Top.user, matchup.Bottom.user, 10, matchup.directoryPath);
            }
            while (matchup.setScores.result.firstPlayerResult == matchup.setScores.result.secondPlayerResult); // In case of a draw

            if (matchup.setScores.result.firstPlayerResult < matchup.setScores.result.secondPlayerResult)
            {
                matchup.WinnerSlot.user = matchup.Top.user;
                if (matchup.LoserSlot != null)
                {
                    matchup.LoserSlot.user = matchup.Bottom.user;
                }
            }
            else
            {
                matchup.WinnerSlot.user = matchup.Bottom.user;
                if (matchup.LoserSlot != null)
                {
                    matchup.LoserSlot.user = matchup.Top.user;
                }
            }
            matchup.played = true;
            matchup.gamesResReveales = new bool[2 * matchup.setScores.gamesPerSide];
            return RedirectToAction("Playoff");
        }
        public IActionResult ShowResult(int roundNum, int matchupNum)
        {
            Tournament.StandingsTree.Matchup matchup = Tournament.GetInstance().Tree.rounds[roundNum].matchups[matchupNum];
            if (matchup.setScores.result.firstPlayerResult < matchup.setScores.result.secondPlayerResult)
            {
                matchup.Top.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.win;
                matchup.Bottom.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.loss;
            }
            else
            {
                matchup.Top.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.loss;
                matchup.Bottom.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.win;
            }
            matchup.WinnerSlot.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.neutral;
            if(matchup.LoserSlot != null)
                matchup.LoserSlot.CSSclass = Tournament.StandingsTree.PlayerSlot.SlotCSSClass.neutral;
            matchup.Top.showScores = true;
            matchup.Bottom.showScores = true;
            matchup.resShowed = true;
            return RedirectToAction("Playoff");
        }
        // Reveals result of a selected matchup and then shows its detailed info
        public IActionResult RevealGameRes(int roundNum, int matchupNum, int num)
        {
            Tournament.StandingsTree.Matchup matchup = Tournament.GetInstance().Tree.rounds[roundNum].matchups[matchupNum];
            matchup.gamesResReveales[num] = true;
            return View("MatchupInfo", matchup);
        }
        // Shows detailed info of a selected matchup
        public IActionResult MatchupInfo(int roundNum, int matchupNum)
        {
            Tournament.StandingsTree.Matchup matchup = Tournament.GetInstance().Tree.rounds[roundNum].matchups[matchupNum];
            ViewBag.roundNum = roundNum;
            ViewBag.matchupNum = matchupNum;
            return View(matchup);
        }
        // Opens view with playoff tree (Views.Game.Playoff)
        public IActionResult Playoff()
        {
            return View(Tournament.GetInstance().Tree);
        }
        // Parses game log file for visualization
        public IActionResult Visualise(string path)
        {
            string[] allLogStrings = System.IO.File.ReadAllLines(Helper.WebRootFullPath(path));
            List<Step> steps = new List<Step>();


            int i = 14; // * skip 2-15 lines, we'll get dominoes for hand from first step
            string leftFieldDomino = String.Empty;
            bool currentPlayerIsFirst = true;

            // * Split() gives empty string at end of array
            do
            {
                // next step
                Step step = new Step();
                i += 4;
                currentPlayerIsFirst = !currentPlayerIsFirst;

                // computer's stone
                string[] field = allLogStrings[i].Split(',');
                string[] stone;
                if (field[0] == leftFieldDomino)
                {
                    stone = field[field.Length - 2].Split(':');
                    step.toRigth = true;
                }
                else
                {
                    leftFieldDomino = field[0];
                    stone = leftFieldDomino.Split(':');
                    step.toRigth = false;
                }
                step.field = new Domino(stone);

                // first player
                string[] firstHand = allLogStrings[i + 1].Split(',');
                step.firstHand = new List<Domino>();
                for (int j = 0; j < firstHand.Length - 1; j++)
                    step.firstHand.Add(new Domino(firstHand[j].Split(':')));

                // second player
                string[] secondHand = allLogStrings[i + 2].Split(',');
                step.secondHand = new List<Domino>();
                for (int j = 0; j < secondHand.Length - 1; j++)
                    step.secondHand.Add(new Domino(secondHand[j].Split(':')));

                // add step
                steps.Add(step);
                // end game if someone ran out of dominoes
            } while (allLogStrings[i + 1] != String.Empty && allLogStrings[i + 2] != String.Empty);

            string[] results = allLogStrings[i + 3].Split(':');
            // Views.Game.Visualize
            return View(new StepViewModel(steps, int.Parse(results[0]), int.Parse(results[0]), int.Parse(results[0]), allLogStrings[0], allLogStrings[1]));
        }
        // Serializes current Tournament singletone object and write it in logs
        public IActionResult SaveTournamentData()
        {          
            if (Tournament.GetInstance() != null)
            {
                string tournamentJson = Helper.SerializeObject(Tournament.GetInstance()).Value;
                FileStream stream = System.IO.File.Create(Helper.WebRootFullPath("tournament.txt"));
                stream.Close();
                StreamWriter writer = new StreamWriter(Helper.WebRootFullPath("tournament.txt"));
                writer.Write(tournamentJson);
                writer.Close();
            }
            return RedirectToAction("Info");
        }


        // Loads tournament state from file
        public IActionResult LoadTournamentData()
        {
            
            string toDeserialize = System.IO.File.ReadAllText(Helper.WebRootFullPath("tournament.txt"));
            Console.WriteLine(toDeserialize);
            Tournament tournament = JsonConvert.DeserializeObject<Tournament>(toDeserialize);
            Tournament.LoadInstance(tournament);
            return RedirectToAction("Info");
        }
    }
}
