using System;
using System.Collections.Generic;
using System.IO;
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

        double[][] _ihWeights; // Hidden node input weights
        double[] _hBiases; // Hidden node offsets
        double[][] _hoWeights; // Hidden node output weights

        double[] _hOutputs;
        double[] _oBiases; // Output node offsets

        double[] _outputs;
        public double[] Outputs => _outputs;

        private NeuralNetwork()
        {
            _rnd = new Random();
        }

        static int SeedBase = Environment.TickCount;
        static readonly object SeedLock = new object();

        public NeuralNetwork(int numInput, int numHidden, int numOutput, int? seed = null)
        {
            if (seed != null)
                _rnd = new Random(seed.Value); // For unit testing
            else
            {
                // Default constructor of Random uses Environment.TickCount
                // This approach tries to counter problems that might occur when multiple instances
                // are created within the same time frame.
                lock (SeedLock) { _rnd = new Random((++SeedBase).GetHashCode() ^ Environment.TickCount); }
            }

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

            // back-prop related arrays below
            hGrads = new double[numHidden];
            oGrads = new double[numOutput];

            _ihPrevWeightsDelta = MakeMatrix(numInput, numHidden);
            _hPrevBiasesDelta = new double[numHidden];
            _hoPrevWeightsDelta = MakeMatrix(numHidden, numOutput);
            _oPrevBiasesDelta = new double[numOutput];
        }

        static double[][] MakeMatrix(int rows, int cols) // helper for ctor
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            return result;
        }

        public void Mutate(double delta)
        {
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
            for (int no = 0; no < _numOutput; ++no)
            {
                _oBiases[no] += RandomBetween(delta, -delta);
            }
            for (int nh = 0; nh < _numHidden; ++nh)
            {
                for (int no = 0; no < _numOutput; ++no)
                {
                    _hoWeights[nh][no] += RandomBetween(delta, -delta);
                }
            }

        }

        double RandomBetween(double min, double max) => _rnd.NextDouble() * (max - min) + min;

        /// <summary>
        /// Compute outputs of the net.
        /// </summary>
        /// <param name="softmax">If true, the maximum output value will be scaled to 1.</param>
        public void ComputeOutputs(bool softmax)
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

            if (softmax)
            {
                double[] softOut = Softmax(oSums); // softmax activation does all outputs at once for efficiency
                Array.Copy(softOut, _outputs, _numOutput);
            }
            else
            {
                Array.Copy(oSums, _outputs, _numOutput);
            }
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

        public string SaveAsString()
        {
            var sb = new StringBuilder();
            var info = System.Globalization.CultureInfo.InvariantCulture;

            sb.AppendLine("# Neural Network Definition");
            sb.AppendLine("# This file was automatically generated. Do not change!");
            sb.AppendLine();
            sb.AppendLine("# Number of nodes:");
            sb.AppendLine("Inputs: " + _numInput);
            sb.AppendLine("Hidden: "+ _numHidden);
            sb.AppendLine("Outputs: "+ _numOutput);
            sb.AppendLine();
            sb.AppendLine("# Biases:");
            sb.Append("BHidden: ");
            for (int i = 0; i < _hBiases.Length; ++i)
            {
                if (i != 0) sb.Append(";");
                sb.Append(_hBiases[i].ToString(info));
            }
            sb.AppendLine();
            sb.Append("BOutput: ");
            for (int i = 0; i < _oBiases.Length; ++i)
            {
                if (i != 0) sb.Append(";");
                sb.Append(_oBiases[i].ToString(info));
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("# Weights:");
            sb.Append("WHidden: ");
            for (int i = 0; i < _ihWeights.Length; ++i)
            {
                for (int j = 0; j < _ihWeights[0].Length; ++j)
                {
                    if (i != 0 || j != 0) sb.Append(";");
                    sb.Append(_ihWeights[i][j].ToString(info));
                }
            }
            sb.AppendLine();
            sb.Append("WOutput: ");
            for (int i = 0; i < _hoWeights.Length; ++i)
            {
                for (int j = 0; j < _hoWeights[0].Length; ++j)
                {
                    if (i != 0 || j != 0) sb.Append(";");
                    sb.Append(_hoWeights[i][j].ToString(info));
                }
            }

            // The seed is not saved

            sb.AppendLine();
            return sb.ToString();
        }

        public bool SaveAsFile(string filename)
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
                {
                    var info = System.Globalization.CultureInfo.InvariantCulture;

                    file.Write(SaveAsString());

                    file.Flush();
                    file.Close();
                }
            }
            catch (Exception e)
            {
                AccessBattle.Log.WriteLine(AccessBattle.LogPriority.Error, "Cannot save neural network. " + e.Message);
                return false;
            }
            return true;
        }

        public static NeuralNetwork ReadFromString(string data)
        {
            var network = new NeuralNetwork();
            using (StringReader reader = new StringReader(data))
            {
                try
                {
                    var info = System.Globalization.CultureInfo.InvariantCulture;
                    string line;
                    while ((line = reader.ReadLine()?.Trim()) != null)
                    {
                        if (line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

                        if (line.StartsWith("Inputs:"))
                        {
                            network._numInput = Convert.ToInt32(line.Substring("Inputs:".Length));
                        }
                        else if (line.StartsWith("Hidden:"))
                        {
                            network._numHidden = Convert.ToInt32(line.Substring("Hidden:".Length));
                        }
                        else if (line.StartsWith("Outputs:"))
                        {
                            network._numOutput = Convert.ToInt32(line.Substring("Outputs:".Length));
                        }
                        else if (line.StartsWith("BHidden:"))
                        {
                            int i = 0;
                            network._hBiases = new double[network._numHidden];
                            foreach (var str in line.Substring("BHidden:".Length).Split(';'))
                            {
                                network._hBiases[i++] = Convert.ToDouble(str, info);
                            }
                        }
                        else if (line.StartsWith("BOutput:"))
                        {
                            int i = 0;
                            network._oBiases = new double[network._numOutput];
                            foreach (var str in line.Substring("BOutput:".Length).Split(';'))
                            {
                                network._oBiases[i++] = Convert.ToDouble(str, info);
                            }
                        }
                        else if (line.StartsWith("WHidden:"))
                        {
                            network._ihWeights = MakeMatrix(network._numInput, network._numHidden);
                            int ni = 0;
                            int nh = 0;

                            foreach (var str in line.Substring("BOutput:".Length).Split(';'))
                            {
                                network._ihWeights[ni][nh] = Convert.ToDouble(str, info);
                                if (++nh >= network._numHidden)
                                {
                                    nh = 0;
                                    ++ni;
                                }
                            }
                        }
                        else if (line.StartsWith("WOutput:"))
                        {
                            network._hoWeights = MakeMatrix(network._numHidden, network._numOutput);

                            int nh = 0;
                            int no = 0;

                            foreach (var str in line.Substring("BOutput:".Length).Split(';'))
                            {
                                network._hoWeights[nh][no] = Convert.ToDouble(str, info);
                                if (++no >= network._numOutput)
                                {
                                    no = 0;
                                    ++nh;
                                }
                            }
                        }

                    }
                    // We do not do any further validations here. Software will crash soon enough if something was wrong.

                    // Init missing fields
                    network._inputs = new double[network._numInput];
                    network._outputs = new double[network._numOutput];
                    network._hOutputs = new double[network._numHidden];
                }
                catch (Exception e)
                {
                    AccessBattle.Log.WriteLine(AccessBattle.LogPriority.Error, "Cannot read neural network. " + e.Message);
                    return null;
                }
            }
            return network;
        }

        public static NeuralNetwork ReadFromFile(string filename)
        {
            try
            {
                return ReadFromString(System.IO.File.ReadAllText(filename));
            }
            catch (Exception e)
            {
                AccessBattle.Log.WriteLine(AccessBattle.LogPriority.Error, "Cannot read neural network. " + e.Message);
                return null;
            }
        }

        #region Backpropagation

        // back-prop specific arrays (these could be local to UpdateWeights)
        private double[] oGrads; // output gradients for back-propagation
        private double[] hGrads; // hidden gradients for back-propagation

        // back-prop momentum specific arrays (necessary as class members)
        private double[][] _ihPrevWeightsDelta;  // for momentum with back-propagation
        private double[] _hPrevBiasesDelta;
        private double[][] _hoPrevWeightsDelta;
        private double[] _oPrevBiasesDelta;

        private void UpdateWeights(double[] tValues, double learnRate, double momentum)
        {
            // update the weights and biases using back-propagation, with target values, eta (learning rate), alpha (momentum)
            // assumes that SetWeights and ComputeOutputs have been called and so all the internal arrays and matrices have values (other than 0.0)
            if (tValues.Length != _numOutput)
                throw new Exception("target values not same Length as output in UpdateWeights");

            // 1. compute output gradients
            for (int i = 0; i < oGrads.Length; ++i)
            {
                double derivative = (1 - _outputs[i]) * _outputs[i]; // derivative of softmax = (1 - y) * y (same as log-sigmoid)
                oGrads[i] = derivative * (tValues[i] - _outputs[i]); // 'mean squared error version' includes (1-y)(y) derivative
                                                                    //oGrads[i] = (tValues[i] - outputs[i]); // cross-entropy version drops (1-y)(y) term! See http://www.cs.mcgill.ca/~dprecup/courses/ML/Lectures/ml-lecture05.pdf page 25.
            }

            // 2. compute hidden gradients
            for (int i = 0; i < hGrads.Length; ++i)
            {
                double derivative = (1 - _hOutputs[i]) * (1 + _hOutputs[i]); // derivative of tanh = (1 - y) * (1 + y)
                double sum = 0.0;
                for (int j = 0; j < _numOutput; ++j) // each hidden delta is the sum of numOutput terms
                {
                    double x = oGrads[j] * _hoWeights[i][j];
                    sum += x;
                }
                hGrads[i] = derivative * sum;
            }

            // 3a. update hidden weights (gradients must be computed right-to-left but weights can be updated in any order)
            for (int i = 0; i < _ihWeights.Length; ++i) // 0..2 (3)
            {
                for (int j = 0; j < _ihWeights[0].Length; ++j) // 0..3 (4)
                {
                    double delta = learnRate * hGrads[j] * _inputs[i]; // compute the new delta
                    _ihWeights[i][j] += delta; // update. note we use '+' instead of '-'. this can be very tricky.
                    _ihWeights[i][j] += momentum * _ihPrevWeightsDelta[i][j]; // add momentum using previous delta. on first pass old value will be 0.0 but that's OK.
                    _ihPrevWeightsDelta[i][j] = delta; // don't forget to save the delta for momentum
                }
            }

            // 3b. update hidden biases
            for (int i = 0; i < _hBiases.Length; ++i)
            {
                double delta = learnRate * hGrads[i] * 1.0; // the 1.0 is the constant input for any bias; could leave out
                _hBiases[i] += delta;
                _hBiases[i] += momentum * _hPrevBiasesDelta[i]; // momentum
                _hPrevBiasesDelta[i] = delta; // don't forget to save the delta
            }

            // 4. update hidden-output weights
            for (int i = 0; i < _hoWeights.Length; ++i)
            {
                for (int j = 0; j < _hoWeights[0].Length; ++j)
                {
                    double delta = learnRate * oGrads[j] * _hOutputs[i];  // see above: hOutputs are inputs to the nn outputs
                    _hoWeights[i][j] += delta;
                    _hoWeights[i][j] += momentum * _hoPrevWeightsDelta[i][j]; // momentum
                    _hoPrevWeightsDelta[i][j] = delta; // save
                }
            }

            // 4b. update output biases
            for (int i = 0; i < _oBiases.Length; ++i)
            {
                double delta = learnRate * oGrads[i] * 1.0;
                _oBiases[i] += delta;
                _oBiases[i] += momentum * _oPrevBiasesDelta[i]; // momentum
                _oPrevBiasesDelta[i] = delta; // save
            }
        } // UpdateWeights

        // ----------------------------------------------------------------------------------------

        // train data: { in, in, in, ... ,  out, out, ... } // It's combined data.
        // It's a list of dataset arrays to pass multiple datasets at once.
        public void Train(double[][] trainData, int maxEprochs, double learnRate, double momentum)
        {
            // train a back-prop style NN classifier using learning rate and momentum
            int epoch = 0;
            double[] tValues = new double[_numOutput]; // target values

            int[] sequence = new int[trainData.Length];
            for (int i = 0; i < sequence.Length; ++i)
                sequence[i] = i;

            while (epoch < maxEprochs)
            {
                double mse = MeanSquaredError(trainData);
                if (mse < 0.001) break; // consider passing value in as parameter

                Shuffle(sequence); // visit each training data in random order
                for (int i = 0; i < trainData.Length; ++i)
                {
                    int idx = sequence[i];
                    Array.Copy(trainData[idx], _inputs, _numInput); // more flexible might be a 'GetInputsAndTargets()'
                    Array.Copy(trainData[idx], _numInput, tValues, 0, _numOutput);
                    ComputeOutputs(true);
                    UpdateWeights(tValues, learnRate, momentum); // use curr outputs and targets and back-prop to find better weights
                } // each training tuple
                ++epoch;
            }
        } // Train

        void Shuffle(int[] sequence)
        {
            for (int i = 0; i < sequence.Length; ++i)
            {
                int r = _rnd.Next(i, sequence.Length);
                int tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }

        private double MeanSquaredError(double[][] trainData) // used as a training stopping condition
        {
            // average squared error per training tuple
            double sumSquaredError = 0.0;
            double[] tValues = new double[_numOutput]; // last numOutput values

            for (int i = 0; i < trainData.Length; ++i) // walk thru each training case. looks like (6.9 3.2 5.7 2.3) (0 0 1)  where the parens are not really there
            {
                Array.Copy(trainData[i], _inputs, _numInput); // get xValues. more flexible would be a 'GetInputsAndTargets()'
                Array.Copy(trainData[i], _numInput, tValues, 0, _numOutput); // get target values
                ComputeOutputs(true); // compute output using current weights
                for (int j = 0; j < _numOutput; ++j)
                {
                    double err = tValues[j] - _outputs[j];
                    sumSquaredError += err * err;
                }
            }

            return sumSquaredError / trainData.Length;
        }

        #endregion
    }
}
