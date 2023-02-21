using System;
using System.Collections.Generic;
using UnityEngine;


// Class representing a single layer of a fully connected feedforward neural network.
public class Neuron {
    // Value of neuron.
    public double Value {
        get;
        set;
    }

    // All weight needed to calculate value.
    public double[] Weights {
        get;
        set;
    }

    // Number of connection needed to calculate value.
    public int ConnectionCount {
        get;
        set;
    }

    // True if this Neuron is part of first layer.
    public bool InputNeuron {
        get;
        set;
    }

    // True if this is the extra biased neuron of a layer. 
    public bool Biased {
        get;
        set;
    }

    // Create new neuron by given parameters. Weight are not set yet.
    public Neuron(int connectionCount, bool inputNeuron, bool biased) {
        this.ConnectionCount = connectionCount;
        this.InputNeuron = inputNeuron;
        this.Biased = biased;
        if (this.Biased) {
            this.Value = 1;
        } else {
            this.Value = 1;
        }
        this.Weights = new double[connectionCount];
    }

    // Function used to transform value.
    public static double NeuronActivationFunction(double xValue) {
        // This is the standard Tahn calculation in range -10, 10
        if (xValue > 10) {
            return 1.0;
        } else if (xValue < -10) {
            return -1.0;
        } else {
            return Math.Tanh(xValue);
        }
    }

    // Calculate value based on weights if neuron not biased of input.
    public double CalculateValue(double[] inputs) {
        // InputNeuron shouldn't calculate, value should be set already.
        // BiasedNeuron always should be 1.
        if (this.InputNeuron || this.Biased) {
            return this.Value;
        }
        //Check arguments
        if (inputs.Length != this.ConnectionCount) {
            throw new ArgumentException("Input count not matches connection count!");
        }

        // Calculate value of neuron based on weights of connected neurons.
        // Bias should be last in inputs and should be 1.
        this.Value = 0;
        for (int i = 0; i < inputs.Length; i++) {
            this.Value += (double) (inputs[i] * this.Weights[i]);
        }
        // Transform value based on activation function
        this.Value = NeuronActivationFunction(this.Value);
        return this.Value;
    }

    // Set weights to given values.
    public void SetWeights(List<float> weights) {
        if (this.InputNeuron) {
            return;
        }
        for (int i = 0; i < Weights.Length; i++) {
            this.Weights[i] = weights[i];
        }
    }
}
