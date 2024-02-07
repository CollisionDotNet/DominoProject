using System.Collections.Generic;

namespace DominoProject.Models
{
    public class Domino
    {
        public int first;
        public int second;

        public Domino(int first, int second)
        {
            this.first = first;
            this.second = second;
        }
        public Domino (string[] stone)
        {
            this.first = int.Parse(stone[0]);
            this.second = int.Parse(stone[1]);
        }
    }
    public class Step
    {
        public Domino field { get; set; }
        public bool toRigth { get; set; }
        public List<Domino> firstHand { get; set; }
        public List<Domino> secondHand { get; set; }
    }
}
