using System;
using UnityEngine;
using System.Collections.Generic;


// Handles everything that happens in a round.
public class FieldManager : MonoBehaviour {
    // for checking instance of FieldManager
    public static FieldManager Instance {
        get;
        private set;
    }

    // Starting position of runners
    [SerializeField]
    private PlayerController[] StartingRunners;
    // Starting position of catchers
    [SerializeField]
    private PlayerController[] StartingCatchers;

    // ingame players
    private List<PlayerController> runners = new List<PlayerController>();
    private List<PlayerController> catchers = new List<PlayerController>();
    
    // waiting befor round start
    private float BufferTime = 2f;
    private float timer = 0f;
    private bool shouldRestart = true;

    // wether the players are playing or not
    private bool isRunning = false;

    // Timer for round, visible on ui.
    public float RoundTimer {
        get;
        private set;
    }

    // Count of ingame runners.
    public int RunnersCount {
        get { return runners.Count; }
    }

    // Count of ingame catchers.
    public int CatchersCount {
        get { return catchers.Count; }
    }

    // Count of runners that are still playing in round.
    public int RemainingRunners {
        get;
        private set;
    }

    // Best score of already dead runners of round.
    public float BestRunnerScore {
        get;
        private set;
    }

    // Best score of all catchers of round.
    public float BestCatcherScore {
        get;
        private set;
    }

    // Unity method
    void Awake() {
        if (Instance != null) {
            Debug.LogError("A Scene should only contain one FieldManager.");
            return;
        }
        Instance = this;

        // Hide player placeholders
        foreach (PlayerController player in StartingRunners) {
            player.gameObject.SetActive(false);
        }
        foreach (PlayerController player in StartingCatchers) {
            player.gameObject.SetActive(false);
        }
    }

    // Unity method
    void Update() {
        this.timer += Time.deltaTime;
        if (this.isRunning && this.RoundTimer > 0f) {
            this.RoundTimer -= Time.deltaTime;
            CheckRemainingRunnersCount();
        } else {
            this.RoundTimer =  Brain.RoundTime;
        }
        if (this.shouldRestart && this.timer > this.BufferTime) {
            this.shouldRestart = false;
            this.timer = 0f;
            House.restartCounter();
            RestartPlayers();
        }
    }

    // Set all players for playing.
    public void SetPlayers(int runnersValue, int catchersValue) {
        if (runnersValue + catchersValue == RunnersCount + CatchersCount) {
            return;
        }
        SetOneTypeOfPlayers(runnersValue, this.RunnersCount, this.StartingRunners, this.runners);
        SetOneTypeOfPlayers(catchersValue, this.CatchersCount, this.StartingCatchers, this.catchers);

        // Handle events
        foreach (PlayerController catcher in catchers) {
            catcher.CatchRunner += OnCatch;
        }
        foreach (PlayerController runner in runners) {
            runner.RunnerDiesWithoutCatch += OnRunnerSelfKill;
        }
    }

    // Set on type of players for playering.
    public void SetOneTypeOfPlayers(int setValue, int playerCount, PlayerController[] startingPositions, List<PlayerController> players) {
        if (setValue < 0 || (setValue > 0 && setValue <= 2 )) {
            throw new ArgumentException("Population size should be 0 or more than 2.");
        }
        if (setValue > playerCount)
        {
            if (startingPositions.Length < setValue) {
                setValue = startingPositions.Length;
            }
            for (int toBeAdded = setValue - playerCount; toBeAdded > 0; toBeAdded--) {
                GameObject playerCopy = Instantiate(startingPositions[toBeAdded-1].gameObject);
                playerCopy.transform.position = startingPositions[toBeAdded-1].transform.position;
                playerCopy.transform.rotation = startingPositions[toBeAdded-1].transform.rotation;
                PlayerController controllerCopy = playerCopy.GetComponent<PlayerController>();
                players.Add(controllerCopy);
                playerCopy.SetActive(true);
            }
        }
        else if (setValue < playerCount) {
            for (int toBeRemoved = playerCount - setValue; toBeRemoved > 0; toBeRemoved--) {
                PlayerController last = players[playerCount - 1];
                players.RemoveAt(playerCount - 1);
                Destroy(last.gameObject);
            }
        }
    }

    // Restarts all round params.
    public void Restart() {
        StopPlayers();
        this.timer = 0f;
        BestRunnerScore = 0f;
        BestCatcherScore = 0f;
        ResetPlacement();
        this.RemainingRunners = this.RunnersCount;
        this.isRunning = false;
        this.shouldRestart = true;
        House.restartCounter();
    }

    // Reference for list of ingame runners.
    public List<PlayerController> GetRunnersRef() {
        return this.runners;
    }

    // Reference for list of ingame catchers.
    public List<PlayerController> GetCatchersRef() {
        return this.catchers;
    }

    // Reset placement of all players
    private void ResetPlacement() {
        for (int i = 0; i < RunnersCount; ++i)
        {
            runners[i].transform.position = StartingRunners[i].transform.position;
            runners[i].transform.rotation = StartingRunners[i].transform.rotation;
            runners[i].transform.GetChild(0).rotation = StartingRunners[i].transform.GetChild(0).rotation;
            runners[i].SetVisibilityAndCollider(true);
        }
        for (int i = 0; i < CatchersCount; ++i)
        {
            catchers[i].transform.position = StartingCatchers[i].transform.position;
            catchers[i].transform.rotation = StartingCatchers[i].transform.rotation;
            catchers[i].transform.GetChild(0).rotation = StartingCatchers[i].transform.GetChild(0).rotation;
            catchers[i].SetVisibilityAndCollider(true);
        }
    }

    // Reactivate all players and reset round clock.
    private void RestartPlayers() {
        for (int i = 0; i < RunnersCount; ++i)
        {
            runners[i].Restart();
        }
        for (int i = 0; i < CatchersCount; ++i)
        {
            catchers[i].Restart();
        }
        this.RoundTimer =  Brain.RoundTime;
        this.isRunning = true;
    }

    // Stop movement of all players.
    private void StopPlayers() {
        for (int i = 0; i < RunnersCount; ++i)
        {
            runners[i].Stop();
        }
        for (int i = 0; i < CatchersCount; ++i)
        {
            catchers[i].Stop();
        }
    }

    // Stop movement of all players.
    private void KillRemainingCatchers() {
        for (int i = 0; i < CatchersCount; ++i) {
            catchers[i].OnTimeRunsOut();
        }
    }

    // Handles a catch event.
    private void OnCatch() {
        // Get all caught runner by ids
        List<int> ids = GetAllCaughtRunnerIds();
        if (Brain.teamworkBonusForCatchers) {
            DealCatchScores(ids);
        }
        HideCaughtRunners(ids);
        // Reset all caught runner ids in movement scripts
        ResetCaughtIds();
        this.BestCatcherScore = GetHighestCatcherScore();
        this.RemainingRunners--;
        if (this.RemainingRunners == 0) {
            KillRemainingCatchers();
        }
    }

    // Handles a runner's death if it wasn't caught.
    private void OnRunnerSelfKill() {
        this.BestRunnerScore = GetHighestRunnerScore();
        this.RemainingRunners--;
        if (this.RemainingRunners == 0) {
            KillRemainingCatchers();
        }
    }

    // Collect all IDs of the caught runners.
    private List<int> GetAllCaughtRunnerIds() {
        List<int> ids = new List<int>();
        foreach (PlayerController catcher in catchers) {
            if (catcher.Movement.CaughtRunnerID != 0) {
                ids.Add(catcher.Movement.CaughtRunnerID);
            }
        }
        return ids;
    }

    // Give bonus point to catchers for teamwork.
    private void DealCatchScores(List<int> ids) {
        foreach (int id in ids) {
            foreach (PlayerController catcher in catchers) {
                catcher.CollectScoreForCatch(id);
            }
        }
    }

    // Returns with highest score of all catchers.
    private float GetHighestCatcherScore() {
        float score = 0f;
        foreach (PlayerController catcher in catchers) {
            score = catcher.Score > score ? catcher.Score : score;
        }
        return score;
    }

    // Returns with highest score of all runners.
    private float GetHighestRunnerScore() {
        float score = 0f;
        foreach (PlayerController runner in runners) {
            score = runner.Score > score ? runner.Score : score;
        }
        return score;
    }

    // Inactivate all caught runners.
    private void HideCaughtRunners(List<int> ids) {
        foreach (PlayerController runner in runners) {
            if (ids.Contains(runner.ID)) {
                runner.SetVisibilityAndCollider(false);
            }
        }
    }

    // Reset all detected runnerIDs of catchers.
    private void ResetCaughtIds() {
        foreach (PlayerController catcher in catchers) {
            catcher.Movement.CaughtRunnerID = 0;
        }
    }

    // Recheck if remaining runners count is right.
    private void CheckRemainingRunnersCount() {
        int count = 0;
        foreach (PlayerController runner in this.runners) {
            if (runner.Brain.IsAlive) {
                count++;
            }
        }
        this.RemainingRunners = count;
    }
}
