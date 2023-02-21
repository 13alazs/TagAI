using UnityEngine;
using UnityEngine.SceneManagement;


// Highest controlling class.
public class GameStateManager : MonoBehaviour {
    // Name of the scene in which the loadable field is in.
    [SerializeField]
    public string FieldName;

    // UIController should set this to itself.
    public UIController UIController {
        get;
        set;
    }

    // Instance of this.
    public static GameStateManager Instance {
        get;
        private set;
    }

    // Unity method
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("Should be exactly one GameStateManagers in a Scene.");
            return;
        }
        Instance = this;
        SceneManager.LoadScene("UI", LoadSceneMode.Additive);
        SceneManager.LoadScene(FieldName, LoadSceneMode.Additive);
    }

    // Unity method
    void Start () {
        EvolutionManager.Instance.StartEvolution();
	}

    // Unity method
    void Update() {
        if (Input.GetKey("escape")) {
            Application.Quit();
        }
    }
}
