using System;
using System.Collections.Generic;
using UnityEngine;


// Class representing a feed forward neural network.
public class NNetwork {
    // Neuron matrix of network.
    public Neuron[,] Neurons {
        get;
        private set;
    }

    // Size of layers.
    public uint[] Structure {
        get;
        private set;
    }

    // Number of weights in whole network.
    public int WeightCount {
        get;
        private set;
    }

    // Initializes a new fully connected feedforward neural network with given structure.
    public NNetwork(uint[] structure) {
        this.Structure = structure;
        // +1 for biased Neurons
        this.Neurons = new Neuron[this.Structure.Length, GetHighestNeuronCount() + 1];

        // Calculate weight count of whole network
        WeightCount = 0;
        for (int i = 0; i < this.Structure.Length; i++) {
            // Last layer not contains biased neuron.
            int layerSize = (int) this.Structure[i];
            layerSize = i == (this.Structure.Length - 1) ? layerSize : layerSize + 1;

            // Add neurons to network
            for (int j = 0; j < layerSize; j++) {
                // In every layer last neuron is biased.
                int connectionCount = i == 0 ? 0 : (int) this.Structure[i - 1] + 1;
                bool inputNeuron = i == 0 ? true : false;
                bool biasedNeuron = j == (layerSize - 1) ? true : false;
                this.Neurons[i, j] = new Neuron(connectionCount, inputNeuron, biasedNeuron);
                WeightCount += connectionCount;
            }
        }
    }

    // Processes the given inputs using the current network's weights.
    public double[] ProcessInputs(double[] inputs) {
        // Fill out first layer with input data
        List<double> previousLayerValues = new List<double>(inputs);
        List<double> currentLayerValues = new List<double>(inputs);
        for (int j = 0; j < inputs.Length; j++) {
            this.Neurons[0, j].Value = inputs[j];
        }
        // Bias of first layer
        previousLayerValues.Add(this.Neurons[0,inputs.Length].Value);

        // First layer is input layer and already set
        for (int i = 1; i < this.Structure.Length; i++) {
            // Calculate values of this layer using previous layer
            currentLayerValues.Clear();
            int layerSize = (int) this.Structure[i];
            layerSize = i == (this.Structure.Length - 1) ? layerSize : layerSize + 1; 
            for (int j = 0; j < layerSize; j++) {
                this.Neurons[i, j].CalculateValue(previousLayerValues.GetRange(0, previousLayerValues.Count).ToArray());
                currentLayerValues.Add(this.Neurons[i, j].Value);
            }
            previousLayerValues = new List<double>(currentLayerValues.GetRange(0, currentLayerValues.Count).ToArray());
        }

        // Return array of last layer values.
        double[] output = currentLayerValues.GetRange(0, currentLayerValues.Count).ToArray();
        return output;
    }

    // Set all weights in whole network.
    public void SetWeights(List<float> parameterList) {
        int startingIdx = 0;
        for (int i = 0; i < this.Structure.Length; i++) {
            int layerSize = (int) this.Structure[i];
            layerSize = i == (this.Structure.Length - 1) ? layerSize : layerSize + 1; 
            for (int j = 0; j < layerSize; j++) {
                // Get part of params that contains weights for just this neuron, then move index. 
                int connectionCount = this.Neurons[i, j].ConnectionCount;
                this.Neurons[i, j].SetWeights(parameterList.GetRange(startingIdx, connectionCount));
                startingIdx += connectionCount;
            }
        }
    }

    // Returns with the highest layer size.
    private int GetHighestNeuronCount() {
        int max = 0;
        for (int i = 0; i < (int) this.Structure.Length; i++) {
            max = max < this.Structure[i] ? (int) this.Structure[i] : max;
        }
        return max;
    }
}
