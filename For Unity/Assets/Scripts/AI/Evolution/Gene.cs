using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


// This used to reduce data of members in the genetic algorithm
public class Gene {
    // Score of player.
    public float Evaluation {
        get;
        set;
    }

    // This is calculated by using the avarage evaluation.
    public float Fitness {
        get;
        set;
    }

    // List of weight values.
    public List<float> Weights {
        get;
        set;
    }

    // Number of weights.
    public int WeightsCount {
        get {
            if (this.Weights == null) {
                return 0;
            }
            return this.Weights.Count;
        }
    }

    // Used for generating random numbers
    private static Random randomGen = new Random();

    private static char SPLITTER = ';';

    // Sets weights to given list and fitness to zero.
    public Gene(float[] weights) {
        this.Weights = new List<float>();
        for (int i = 0; i < weights.Length; i++) {
            this.Weights.Add(weights[i]);
        }
        this.Fitness = 0;
    }

    // Set the weight in the index to the given value.
    public void SetWeight(int index, float value) {
        this.Weights[index] = value;
    }

    // Set all weights to be random within [-range, range].
    public void GenerateRandomWeights(float range) {
        if (0 >= range) {
            throw new ArgumentException("Range should be positive.");
        }
        for (int i = 0; i < this.Weights.Count; i++) {
            this.Weights[i] = (float) ((-1 * range / 2) + (randomGen.NextDouble() * range));
        }
    }

    // Returns a copy of the parameter vector.
    public float[] GenerateWeightsArray() {
        return this.Weights.GetRange(0, this.WeightsCount).ToArray();
    }

    // Save all weight of the gene in the given file.
    public void Save(string path) {
        StringBuilder stringStream = new StringBuilder();
        for (int i = 0; i < this.WeightsCount; i++) {
            string writeValue = this.Weights[i].ToString();
            stringStream.Append(writeValue);
            stringStream.Append(SPLITTER);
        }
        // The last splitter should be removed
        stringStream.Remove(stringStream.Length - 1, 1);
        File.WriteAllText(path, stringStream.ToString());
    }

    // Returns with a new gene, its weights are loaded from given file.
    public static Gene Load(string path) {
        string stringStream = File.ReadAllText(path);
        string[] readValues = stringStream.Split(SPLITTER);
        List<float> loadedWeights = new List<float>();
        for (int i = 0; i < readValues.Length; i++) {
            float convertedWeight;
            // See Microsoft .Net TryParse documentation
            if (!float.TryParse(readValues[i], out convertedWeight)) {
                throw new ArgumentException("Loadable file has incorrect format.");
            }
            loadedWeights.Add(convertedWeight);
        }
        return new Gene(loadedWeights.GetRange(0, loadedWeights.Count).ToArray());
    }
}
