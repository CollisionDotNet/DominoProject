using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.ViewModels
{
    // Represents qualifying stage results for Views.Game.QualifyResults
    public class PlayerQualifyingStageViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Group { get; set; }
        public int QualifyingStageScores { get; set; }
        public Controllers.GameController.SetScores.ResultContext ResultContext { get; set; }
    }
}
