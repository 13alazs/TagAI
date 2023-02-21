using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


// This handles the generic genetics.
public class GeneticAlg {
    // Genetic params
    // For setting random params in genes, range: [-INIT_RANGE, INIT_RANGE].
    public const float INIT_RANGE = 1.0f;
    // Chance of crossover.
    public const float CROSS_PROB = 0.6f;
    // Chance of param mutation.
    public const float MUTATION_PROB = 0.2f;
    // Mutation range: [-MUTATION_DEGREE, MUTATION_DEGREE].
    public const float MUTATION_DEGREE = 1.5f;
    // Chance of gene mutation.
    public const float MUTATION_AMOUNT = 1.0f;

    private static System.Random randomGen = new System.Random();

    private List<Gene> runnersPopulation;
    private List<Gene> catchersPopulation;

    // Count of genes in runner population.
    public uint RunnersPopulationSize {
        get;
        set;
    }

    // Count of genes in catcher population.
    public uint CatchersPopulationSize {
        get;
        set;
    }

    // Serial number of the currently running generation.
    public uint GenerationCount {
        get;
        set;
    }

    private const string LoadFolder = "Loader/";

    // How many genes should be loaded from file.
    public uint LoadCount {
        get;
        set;
    }

    // Flag for elitist genetic logic.
    public bool Elitist {
        get;
        set;
    }

    // Event for finishing fitness calculation.
    public event System.Action<List<Gene>, GameObjectEnum> FitnessCalculationFinished;

    // Event for starting evaluation.
    public event System.Action<List<Gene>, List<Gene>> Evaluation;

    // Unity method
    public void Start() {
        Evaluation(this.runnersPopulation, this.catchersPopulation);
    }

    // Constructor.
    public GeneticAlg(uint geneParamCount, uint runnersPopulationSize, uint catchersPopulationSize, uint loadCount) {
        this.RunnersPopulationSize = runnersPopulationSize;
        this.CatchersPopulationSize = catchersPopulationSize;
        this.LoadCount = loadCount;
        this.GenerationCount = 1;
        SetPopulation(geneParamCount, runnersPopulationSize, ref this.runnersPopulation, GameObjectEnum.Runner);
        SetPopulation(geneParamCount, catchersPopulationSize, ref this.catchersPopulation, GameObjectEnum.Catcher);
    }

    // Create new population by adding random or loaded genes.
    private void SetPopulation(uint geneParamCount, uint populationSize, ref List<Gene> currentPopulation, GameObjectEnum type) {
        currentPopulation = new List<Gene>((int) populationSize);
        // Load any genes that needed and possible
        for (int i = 0; i < populationSize; i++) {
            string fileName = "gene_" + type.ToString() + "_" + i + ".txt";
            if (i < LoadCount && Directory.Exists(LoadFolder) && File.Exists(LoadFolder + fileName)) {
                currentPopulation.Add(Gene.Load(LoadFolder + fileName));
            } else {
                currentPopulation.Add(new Gene(new float[geneParamCount]));
                currentPopulation[i].GenerateRandomWeights(INIT_RANGE);
            }
        }
    }

    // EvolutionManager calls this when a round of the game is finished.
    // This sets up the populations for the next round.
    public void EvaluationFinished() {
        CalculateFitness(ref this.runnersPopulation);
        CalculateFitness(ref this.catchersPopulation);

        // Reorder
        SortPopulation(ref this.runnersPopulation);
        SortPopulation(ref this.catchersPopulation);

        // Invoke event to save statistics
        FitnessCalculationFinished(this.catchersPopulation, GameObjectEnum.Catcher);
        FitnessCalculationFinished(this.runnersPopulation, GameObjectEnum.Runner);

        // Selection
        List<Gene> tempRunnersPopulation;
        List<Gene> tempCatchersPopulation;
        if (this.Elitist) {
            tempRunnersPopulation = ElitistSelection(this.runnersPopulation, this.RunnersPopulationSize);
            tempCatchersPopulation = ElitistSelection(this.catchersPopulation, this.CatchersPopulationSize);
        } else {
            tempRunnersPopulation = SuccessRateBasedSelection(this.runnersPopulation, this.RunnersPopulationSize);
            tempCatchersPopulation = SuccessRateBasedSelection(this.catchersPopulation, this.CatchersPopulationSize);
        }

        // Combination
        List<Gene> newRunnersPopulation;
        List<Gene> newCatchersPopulation;
        if (this.Elitist) {
            newRunnersPopulation = ElitistCombination(tempRunnersPopulation, this.RunnersPopulationSize);
            newCatchersPopulation = ElitistCombination(tempCatchersPopulation, this.CatchersPopulationSize);
        } else {
            newRunnersPopulation = RandomCombination(tempRunnersPopulation, this.RunnersPopulationSize);
            newCatchersPopulation = RandomCombination(tempCatchersPopulation, this.CatchersPopulationSize);
        }

        // Mutation
        MutateExceptBestTwo(ref newRunnersPopulation);
        MutateExceptBestTwo(ref newCatchersPopulation);

        // Reshuffle beacuse order set starting position in the game
        ShuffleOrder(ref newRunnersPopulation);
        ShuffleOrder(ref newCatchersPopulation);

        // Set new to current
        this.runnersPopulation = newRunnersPopulation;
        this.catchersPopulation = newCatchersPopulation;
        this.GenerationCount++;

        // Restart
        Evaluation(runnersPopulation, catchersPopulation);
    }

    // Set fitness of each members of population.
    public static void CalculateFitness(ref List<Gene> currentPopulation) {
        float evalSumm = 0;
        for (int i = 0; i < currentPopulation.Count; i++) {
            evalSumm += currentPopulation[i].Evaluation;
        }
        float averageEval = evalSumm / currentPopulation.Count;
        for (int i = 0; i < currentPopulation.Count; i++) {
            currentPopulation[i].Fitness = currentPopulation[i].Evaluation / averageEval;
        }
    }

    // Reorder population based on fitness. Highest is the first.
    public static void SortPopulation(ref List<Gene> currentPopulation) {
        for (int i = 0; i < currentPopulation.Count; i++) {
            int maxIdx = i;
            for (int j = (i + 1); j < currentPopulation.Count; j++) { 
                if (currentPopulation[maxIdx].Fitness < currentPopulation[j].Fitness) {
                    maxIdx = j;
                }
            }
            if (maxIdx != i) {
                Gene moreFitGene = currentPopulation[maxIdx];
                currentPopulation[maxIdx] = currentPopulation[i];
                currentPopulation[i] = moreFitGene;
            }
        }
    }

    // Add best 2 to temporary population.
    public static List<Gene> ElitistSelection(List<Gene> currentPopulation, uint populationSize) {
        // Need at least 2 member.
        if (currentPopulation.Count < 2) {
            return currentPopulation;
        }
        List<Gene> tempPopulation = new List<Gene>();
        if (populationSize >= 3) {
            tempPopulation.Add(currentPopulation[0]);
            tempPopulation.Add(currentPopulation[1]);
        }
        return tempPopulation;
    }

    // Selection where anyone above avarage fitness can become parent.
    // Chance is based on fitness.
    private List<Gene> SuccessRateBasedSelection(List<Gene> currentPopulation, uint populationSize) {
        List<Gene> tempPopulation = new List<Gene>();
        // based on rounded down fitness
        for (int i = 0; i < currentPopulation.Count; i++) {
            if (currentPopulation[i].Fitness <  1) {
                break;
            } else {
                for (int j = 0; j < (int) currentPopulation[i].Fitness; i++) {
                    tempPopulation.Add(new Gene(currentPopulation[j].GenerateWeightsArray()));
                }
            }
        }

        // based on fraction part
        for (int i = 0; i < currentPopulation.Count; i++) {
            float remainder = currentPopulation[i].Fitness - (int) currentPopulation[i].Fitness;
            if (randomGen.NextDouble() < remainder) {
                tempPopulation.Add(new Gene(currentPopulation[i].GenerateWeightsArray()));
            }
        }

        // If no one scores
        if (tempPopulation.Count < 2 && populationSize != 0) {
            tempPopulation.Add(new Gene(currentPopulation[0].GenerateWeightsArray()));
            tempPopulation.Add(new Gene(currentPopulation[1].GenerateWeightsArray()));
        }
        return tempPopulation;
    }

    // Only combine the best two.
    public static List<Gene> ElitistCombination(List<Gene> tempPopulation, uint newPopulationSize) {
        // Need at least 2 to combinate them.
        if (tempPopulation.Count < 2) {
            return tempPopulation;
        }
        List<Gene> newPop = new List<Gene>();
        // Adding new children to new population until its filled
        while (newPop.Count < newPopulationSize) {
            Gene[] children = GenChildrenByCrossingParents(tempPopulation[0], tempPopulation[1]);
            newPop.Add(children[0]);
            if (newPop.Count < newPopulationSize) {
                newPop.Add(children[1]);
            }
        }
        return newPop;
    }

    // Create new population by combining random genes of given popupation.
    public static List<Gene> RandomCombination(List<Gene> tempPopulation, uint newPopulationSize) {
        // Need at least 2 to combinate them.
        if (tempPopulation.Count < 2) {
            return tempPopulation;
        }

        List<Gene> newPop = new List<Gene>();
        // Do not change best two
        newPop.Add(tempPopulation[0]);
        newPop.Add(tempPopulation[1]);

        // Adding new children to new population until its filled
        while (newPop.Count < newPopulationSize) {
            // Two different random index
            int i = randomGen.Next(0, tempPopulation.Count);
            int j;
            do {
                j = randomGen.Next(0, tempPopulation.Count);
            } while (j == i);

            Gene[] children = GeneticAlg.GenChildrenByCrossingParents(tempPopulation[i], tempPopulation[j]);
            newPop.Add(children[0]);
            if (newPop.Count < newPopulationSize) {
                newPop.Add(children[1]);
            }
        }
        return newPop;
    }

    // Might mutate members of population but definietly not the best two.
    private void MutateExceptBestTwo(ref List<Gene> newPop) {
        for (int i = 2; i < newPop.Count; i++) {
            // Only mutate by chance
            if (randomGen.NextDouble() < MUTATION_AMOUNT) {
                MutateGene(newPop[i]);
            }
        }
    }

    // Create two children of two parents.
    public static Gene[] GenChildrenByCrossingParents(Gene parent1, Gene parent2) {
        Gene[] children = new Gene[2];
        float[] child1Params = new float[parent1.WeightsCount];
        float[] child2Params = new float[parent1.WeightsCount];

        // Swap params by chance
        for (int i = 0; i < parent1.WeightsCount; i++) {
            if (randomGen.Next() < CROSS_PROB) {
                child1Params[i] = parent2.Weights[i];
                child2Params[i] = parent1.Weights[i];
            } else {
                child1Params[i] = parent1.Weights[i];
                child2Params[i] = parent2.Weights[i];
            }
        }
        children[0] = new Gene(child1Params);
        children[1] = new Gene(child2Params);
        return children;
    }

    // Might mutate a gene.
    public static void MutateGene(Gene gene) {
        for (int i = 0; i < gene.WeightsCount; i++) {
            if (randomGen.NextDouble() < MUTATION_PROB) {
                float mutation = (float) (randomGen.NextDouble() * (MUTATION_DEGREE * 2) - MUTATION_DEGREE);
                float mutatedValue = gene.Weights[i] + mutation;
                gene.SetWeight(i, mutatedValue);
            }    
        } 
    }

    // Shuffles the memebers of a population so the ingame positioning of them will be random.
    public static void ShuffleOrder(ref List<Gene> population) {
        int currentIdx = population.Count;
        while (currentIdx > 0) {
            int randomIdx = randomGen.Next(currentIdx);
            currentIdx--;
            Gene tempGene = population[randomIdx];
            population[randomIdx] = population[currentIdx];
            population[currentIdx] = tempGene;
        }
    }
}
