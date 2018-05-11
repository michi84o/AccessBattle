using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Some of this code is based on the example code from a Microsoft keynote.
// Here is an article based on it.
// https://visualstudiomagazine.com/articles/2013/09/01/neural-network-training-using-back-propagation.aspx
// I'm not using Back-Propagation here.

namespace AccessBattleAI.Models
{
    public class NeuralNetwork
    {
        Random _rnd;

        int _numInput;
        int _numHidden;
        int _numOutput;

        double[] _inputs;
        public double[] Inputs => _inputs;

        double[][] _ihWeights; // input-hidden
        double[] _hBiases;
        double[] _hOutputs;

        double[][] _hoWeights; // hidden-output
        double[] _oBiases;

        double[] _outputs;
        public double[] Outputs => _outputs;

        public NeuralNetwork(int numInput, int numHidden, int numOutput, int? seed = null)
        {
            if (seed != null)
                _rnd = new Random(seed.Value); // For unit testing
            else
                _rnd = new Random();

            _numInput = numInput;
            _numHidden = numHidden;
            _numOutput = numOutput;

            _inputs = new double[numInput];

            _ihWeights = MakeMatrix(numInput, numHidden);
            _hBiases = new double[numHidden];
            _hOutputs = new double[numHidden];

            _hoWeights = MakeMatrix(numHidden, numOutput);
            _oBiases = new double[numOutput];

            _outputs = new double[numOutput];
        }

        static double[][] MakeMatrix(int rows, int cols) // helper for ctor
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            return result;
        }

        public void Mutate()
        {
            double delta = 0.0001;
            // Change the weights and biases slightly
            for (int ni = 0; ni < _numInput; ++ni)
            {
                for (int nh = 0; nh < _numHidden; ++nh)
                {
                    _ihWeights[ni][nh] += RandomBetween(delta, -delta);
                }
            }
            for (int nh = 0; nh < _numHidden; ++nh)
            {
                _hBiases[nh] += RandomBetween(delta, -delta);
            }
            for (int nh = 0; nh < _numInput; ++nh)
            {
                for (int no = 0; no < _numOutput; ++no)
                {
                    _hoWeights[nh][no] += RandomBetween(delta, -delta);
                }
            }
        }

        double RandomBetween(double min, double max) => _rnd.NextDouble() * (max - min) + min;

        public void ComputeOutputs()
        {
            double[] hSums = new double[_numHidden]; // hidden nodes sums scratch array
            double[] oSums = new double[_numOutput]; // output nodes sums

            for (int j = 0; j < _numHidden; ++j)  // compute i-h sum of weights * inputs
                for (int i = 0; i < _numInput; ++i)
                    hSums[j] += _inputs[i] * _ihWeights[i][j]; // note +=

            for (int i = 0; i < _numHidden; ++i)  // add biases to input-to-hidden sums
                hSums[i] += _hBiases[i];

            for (int i = 0; i < _numHidden; ++i)   // apply activation
                _hOutputs[i] = HyperTanFunction(hSums[i]); // hard-coded

            for (int j = 0; j < _numOutput; ++j)   // compute h-o sum of weights * hOutputs
                for (int i = 0; i < _numHidden; ++i)
                    oSums[j] += _hOutputs[i] * _hoWeights[i][j];

            for (int i = 0; i < _numOutput; ++i)  // add biases to input-to-hidden sums
                oSums[i] += _oBiases[i];

            double[] softOut = Softmax(oSums); // softmax activation does all outputs at once for efficiency
            Array.Copy(softOut, _outputs, softOut.Length);            
        } // ComputeOutputs

        static double HyperTanFunction(double x)
        {
            if (x < -20.0) return -1.0; // approximation is correct to 30 decimals
            else if (x > 20.0) return 1.0;
            else return Math.Tanh(x);
        }

        static double[] Softmax(double[] oSums) // does all output nodes at once so scale doesn't have to be re-computed each time
        {
            // determine max output sum
            double max = oSums[0];
            for (int i = 0; i < oSums.Length; ++i)
                if (oSums[i] > max) max = oSums[i];

            // determine scaling factor -- sum of exp(each val - max)
            double scale = 0.0;
            for (int i = 0; i < oSums.Length; ++i)
                scale += Math.Exp(oSums[i] - max);

            double[] result = new double[oSums.Length];
            for (int i = 0; i < oSums.Length; ++i)
                result[i] = Math.Exp(oSums[i] - max) / scale;

            return result; // now scaled so that xi sum to 1.0
        }
    }
}
