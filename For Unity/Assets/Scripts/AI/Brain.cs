using System;
using UnityEngine;
using System.Collections.Generic;


// Handles a gene and a network for a player and contains general rules.
public class Brain {
    // Determines wether we show the vision indicator by default.
    public static bool shouldShowVision {
        get;
        set;
    }

    // Determines wether we show the sensor end indicators by default.
    public static bool shouldShowSensor {
        get;
        set;
    }

    // Flag for timebased scoring rule.
    public static bool isScoringTimeBased {
        get;
        set;
    }

    // Flag for house visibility scoring rule.
    public static bool shouldSeeHouseToScore {
        get;
        set;
    }

    // Flag for teamwork scoring rule.
    public static bool teamworkBonusForCatchers {
        get;
        set;
    }

    // Flag for enableing sidestep.
    public static bool canSidestep {
        get;
        set;
    }

    // Time of round in seconds.
    public static float RoundTime {
        get;
        set;
    }

    // The underlying gene of this brain.
    public Gene Gene {
        get;
        private set;
    }

    // A feedforward neural network which was created from the gene of this brain.
    public NNetwork Network {
        get;
        private set;
    }

    private bool isAlive = false;

    // Whether the player pf this brain if still participating in the round.
    public bool IsAlive {
        get { return isAlive; }
        private set {
            if (isAlive != value) {
                isAlive = value;
                if (!isAlive && BrainDied != null) {
                    BrainDied(this);
                }
            }
        }
    }

    // Event for when the brain died (stopped participating in the simulation).
    public event Action<Brain> BrainDied;

    // Create new brain using given gene and network structure.
    public Brain(Gene gene, uint[] structure) {
        IsAlive = false;
        this.Gene = gene;
        Network = new NNetwork(structure);

        //Check if structure is valid
        if (Network.WeightCount != gene.WeightsCount) {
            throw new ArgumentException("Parameter not matches weight count.");
        }

        List<float> parameterList = gene.Weights;
        Network.SetWeights(parameterList);
    }

    // Resets this brain to be alive again.
    public void Reset() {
        Gene.Evaluation = 0;
        Gene.Fitness = 0;
        IsAlive = true;
    }

    // Kills this brain (sets IsAlive to false).
    public void Die(float score) {
        Gene.Evaluation = score;
        IsAlive = false;
    }
}
