using UnityEngine;
using UnityEngine.UI;


// Controlls UI elements.
public class UIController : MonoBehaviour {
    // Set in Unity Editor.
    [SerializeField]
    private Text[] IngameCounters;

    [SerializeField]
    private Text GenerationCounter;

    // Unity method
    void Awake() {
        if (GameStateManager.Instance != null) {
            GameStateManager.Instance.UIController = this;
        }
    }

    // Unity method
    void Update() {
        this.IngameCounters[0].text = FieldManager.Instance.BestRunnerScore.ToString();
        this.IngameCounters[1].text = FieldManager.Instance.BestCatcherScore.ToString();
        this.IngameCounters[2].text = FieldManager.Instance.RemainingRunners.ToString();
        this.IngameCounters[3].text = House.Counter.ToString();
        this.IngameCounters[4].text = FieldManager.Instance.RoundTimer.ToString();
        this.GenerationCounter.text = EvolutionManager.Instance.GenerationCount.ToString();
    }
}
