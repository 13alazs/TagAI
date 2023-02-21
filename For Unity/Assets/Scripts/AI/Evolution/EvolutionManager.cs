using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;


// Highest conroller of evolution parts.
public class EvolutionManager : MonoBehaviour {
    // Flag for every saveable data
    [SerializeField]
    private bool Save = false;

    // Amount of saveable genes per population.
    [SerializeField]
    private uint SaveCount = 0;

    // Amount of loadable genes per population.
    [SerializeField]
    private uint LoadCount = 0;

    // Runners population size.
    [SerializeField]
    private int RunnersPopulationSize = 30;

    // Catchers population size.
    [SerializeField]
    private int CatchersPopulationSize = 30;

    // Application countdown in game rounds.
    [SerializeField]
    private int QuitAfter = 50;

    // Flag for elitist
    [SerializeField]
    private bool ElitistSelection = false;

    // Structure of the brain's Network
    [SerializeField]
    private uint[] NetworkStructure;

    // Set time of round in seconds.
    [SerializeField]
    private float RoundTime = 60f;

    // Flag for field of vision visibility.
    [SerializeField]
    private bool ShowVision = true;

    // Flag for sensor visibility.
    [SerializeField]
    private bool ShowSensor = false;

    // Flag for timebased scoring rule.
    [SerializeField]
    private bool TimeBasedScoring = false;

    // Flag for house visibility scoring rule.
    [SerializeField]
    private bool ShouldSeeHouseToScore = true;

    // Flag for teamwork scoring rule.
    [SerializeField]
    private bool BonusForTeamwork = true;

    // Flag for enableing sidestep.
    [SerializeField]
    private bool EnableSidestep = true;

    // Event for when all playerbrains have died.
    public event System.Action AllBrainsDied;

    // List containing brains of players.
    private List<Brain> brainList = new List<Brain>();

    public static EvolutionManager Instance {
        get;
        private set;
    }

    // The amount of playerbrains that are currently alive.
    public int BrainsAliveCount {
        get;
        private set;
    }

    // This only needed for sending data to UI.
    public int GenerationCount {
        get {
            return (int) this.genAlg.GenerationCount;
        }
    }

    private GeneticAlg genAlg;
    private string statisticsFileName;

    // Unity method
    void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one EvolutionManager in the Scene.");
            return;
        }
        Instance = this;
        // Set visions and scoring rules
        SetVisionVisibility(this.ShowVision);
        SetSensorVisibility(this.ShowSensor);
        SetTimeBasedScoring(this.TimeBasedScoring);
        SetHouseScoringRule(this.ShouldSeeHouseToScore);
        SetTeamworkScoringRule(this.BonusForTeamwork);
        SetSideStep(this.EnableSidestep);
        SetRoundTime(this.RoundTime);
    }

    // Whole simulation launched by this.
    public void StartEvolution() {
        NNetwork nn = new NNetwork(NetworkStructure);
        this.genAlg = new GeneticAlg((uint) nn.WeightCount, (uint) RunnersPopulationSize, (uint) CatchersPopulationSize, this.LoadCount);
        this.genAlg.Evaluation += StartEvaluation;
        this.genAlg.Elitist = this.ElitistSelection;
        AllBrainsDied += this.genAlg.EvaluationFinished;

        //Statistics
        if (this.Save) {
            statisticsFileName = "Simulation_" + GameStateManager.Instance.FieldName + " " + DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
            SaveStatistics();
        }
        this.genAlg.FitnessCalculationFinished += HandleSavingAndQuiting;
        this.genAlg.Start();
    }

    // Save all data that should then quit if should.
    private void HandleSavingAndQuiting(List<Gene> currentPopulation, GameObjectEnum playerType) {
        if (this.Save) {
            AddRoundBasedStatistics(currentPopulation, playerType);
            if (this.SaveCount > 0) {
                SaveBestPlayers(currentPopulation, playerType);
            }
        }
        // Escape application if should
        if (this.QuitAfter > 0 && this.QuitAfter == this.genAlg.GenerationCount) {
            Application.Quit();
        }
    }

    // Collect general statistics data and save to file.
    private void SaveStatistics() {
        File.WriteAllText(statisticsFileName + ".txt",
            "Runner population size: " + this.RunnersPopulationSize + Environment.NewLine +
            "Catcher population size: " + this.CatchersPopulationSize + Environment.NewLine +
            "Fieldname: " + GameStateManager.Instance.FieldName + Environment.NewLine +
            "Preloaded: " + this.LoadCount + Environment.NewLine +
            "Selection elitist: " + this.ElitistSelection + Environment.NewLine +
            "Round time: " + this.RoundTime + Environment.NewLine +
            "Time based scoring: " + this.TimeBasedScoring + Environment.NewLine +
            "Should see house to score: " + this.ShouldSeeHouseToScore + Environment.NewLine +
            "Bonus for teamwork: " + this.BonusForTeamwork + Environment.NewLine + Environment.NewLine);
    }

    // Collect round specific data for statistics and add to file.
    private void AddRoundBasedStatistics(List<Gene> currentPopulation, GameObjectEnum playerType) {
        if (currentPopulation.Count > 0) {
            File.AppendAllText(statisticsFileName + ".txt",
                this.genAlg.GenerationCount + "\t" + currentPopulation[0].Evaluation + "\t" + playerType.ToString() + Environment.NewLine);
            if (playerType == GameObjectEnum.Runner) {
                File.AppendAllText(statisticsFileName + ".txt",
                    this.genAlg.GenerationCount + " Safe runners: \t" + House.Counter + Environment.NewLine);
            }
        }
    }

    // Save all the best players it can.
    private void SaveBestPlayers(List<Gene> currentPopulation, GameObjectEnum playerType) {
        string saveFolder = statisticsFileName + "/";
        if (!Directory.Exists(saveFolder)) {
            Directory.CreateDirectory(saveFolder);
        }
        string generationFolder = "Generation_" + this.genAlg.GenerationCount + "/";
        if (!Directory.Exists(saveFolder + generationFolder)) {
            Directory.CreateDirectory(saveFolder + generationFolder);
        }
        int shouldSaveCount = currentPopulation.Count < (int) SaveCount ? currentPopulation.Count : (int) SaveCount;
        for (int i = 0; i < currentPopulation.Count; i++) {
            if (shouldSaveCount <= i) {
                return;
            }
            currentPopulation[i].Save(saveFolder + generationFolder + "gene_" + playerType.ToString() + "_" + i + ".txt");
        }
    }

    // This should set the players for a new round by adding them brain and setting the field.
    private void StartEvaluation(List<Gene> runnersGenes, List<Gene> catchersGenes) {
        // Create new playerbrains from the two genelists
        brainList.Clear();
        this.BrainsAliveCount = 0;
        for (int i = 0; i < runnersGenes.Count; i++) {
            brainList.Add(new Brain(runnersGenes[i], NetworkStructure));
        }
        for (int i = 0; i < catchersGenes.Count; i++) {
            brainList.Add(new Brain(catchersGenes[i], NetworkStructure));
        }

        // Set field
        FieldManager.Instance.SetPlayers(RunnersPopulationSize, CatchersPopulationSize);
        List<PlayerController> ingameRunners = FieldManager.Instance.GetRunnersRef();
        List<PlayerController> ingameCatchers = FieldManager.Instance.GetCatchersRef();
        if (ingameRunners.Count != RunnersPopulationSize || ingameCatchers.Count != CatchersPopulationSize) {
            Debug.LogError("Player count does not match population size.");
            return;
        }
        // Add brains to players
        for (int i = 0; i < RunnersPopulationSize; i++) {
            ingameRunners[i].Brain = brainList[i];
            this.BrainsAliveCount++;
            brainList[i].BrainDied += OnBrainDied;
        }
        for (int i = RunnersPopulationSize; i < CatchersPopulationSize + RunnersPopulationSize; i++) {
            ingameCatchers[i - RunnersPopulationSize].Brain = brainList[i];
            this.BrainsAliveCount++;
            brainList[i].BrainDied += OnBrainDied;
        }
        FieldManager.Instance.Restart();
    }

    // Called when a player connected to the brain died.
    private void OnBrainDied(Brain brain) {
        this.BrainsAliveCount--;
        if (this.BrainsAliveCount == 0 && AllBrainsDied != null) {
            AllBrainsDied();
        }
    }

    // Sets field of vision visibility.
    private void SetVisionVisibility(bool show) {
        Brain.shouldShowVision = show;
    }

    // Sets sensor visibility.
    private void SetSensorVisibility(bool show) {
        Brain.shouldShowSensor = show;
    }

    // Sets time based scoring rule.
    private void SetTimeBasedScoring(bool value) {
        Brain.isScoringTimeBased = value;
    }

    // Sets that runners have to see house to score in it.
    private void SetHouseScoringRule(bool value) {
        Brain.shouldSeeHouseToScore = value;
    }

    // Sets Teamwork scoring Rule.
    private void SetTeamworkScoringRule(bool value) {
        Brain.teamworkBonusForCatchers = value;
    }

    // Sets wether players can sidestep or not.
    private void SetSideStep(bool value) {
        Brain.canSidestep = value;
    }

    // Sets round time of game.
    private void SetRoundTime(float roundTime) {
        Brain.RoundTime = roundTime;
    }
}
