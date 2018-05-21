using AccessBattleAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetTrainer
{
    class TrainingLog
    {
        public int ID;
        public Nou AI;
        public double Score;
        public List<TrainingLog> Opponents = new List<TrainingLog>();

        public override string ToString()
        {
            return "ID=" + ID + ", OP:" + Opponents.Count;
        }
    }
}
