using AccessBattleAI;

namespace NeuralNetTrainer
{
    class TrainingLog
    {
        public int ID;
        public Nou AI;
        public double Score;

        public override string ToString()
        {
            return "ID=" + ID + ", Score: " + Score;
        }
    }
}
