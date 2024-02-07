using DominoProject.Models;
using System.Collections.Generic;

namespace DominoProject.ViewModels
{
    public class StepViewModel
    {
        // Represents a single step in a separate game
        public string firstPlayer; // name
        public string secondPlayer; // name
        public int winner;
        public int firstRes;
        public int secondRes;
        public IEnumerable<Step> steps { get; private set; }

        public StepViewModel(IEnumerable<Step> steps, int winner, int firstRes, int secondRes, string firstPlayer, string secondPlayer)
        {
            this.steps = steps;
            this.winner = winner;
            this.firstRes = firstRes;
            this.secondRes = secondRes;
        }
    }
}
