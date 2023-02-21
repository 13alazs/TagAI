using UnityEngine;


// Capable of reading the distance and type of nearest obstacle in a direction within range.
public class Sensor : MonoBehaviour {
    // Set in Unity editor.
    [SerializeField]
    private SpriteRenderer sensorEnd;

    // Current distance of detected gameobject in percentage.
    public float Distance {
        get;
        private set;
    }

    // Current detected gameobject type.
    public GameObjectEnum Type {
        get;
        private set;
    }

    // Current detected ID of runner. Zero if no runner is detected.
    public int RunnerID {
        get;
        private set;
    }

    // Max and min reading distances
    public static float MAX_DIST = 30f;
    private const float MIN_DIST = 0.01f;


    // Unity method
    void FixedUpdate() {
        // Calculate direction of sensor
        Vector2 direction = this.sensorEnd.transform.position - this.transform.position;
        direction.Normalize();

        // Send raycasts into direction of sensor
        RaycastHit2D hitData =  Physics2D.Raycast(this.transform.position, direction, MAX_DIST);
        SetOutputs(hitData);

        // Set position of sensorEnd
        this.sensorEnd.transform.position = (Vector2) this.transform.position + direction * this.Distance;
	}

    // Set output values depending on detected raycasthits. Id should be 0 if no runner is detected.
    private void SetOutputs(RaycastHit2D hitData) {
        // collider is null if nothing detected
        if (hitData.collider == null) {
            this.Distance = MAX_DIST;
            this.Type = GameObjectEnum.None;
            this.RunnerID = 0;
        } else {
            this.Distance = hitData.distance;
            switch (hitData.transform.gameObject.tag) {
                case "Runner":
                    this.Type = GameObjectEnum.Runner;
                    this.RunnerID = hitData.transform.gameObject.GetComponent<PlayerController>().ID;
                    break;
                case "Catcher":
                    this.Type = GameObjectEnum.Catcher;
                    this.RunnerID = 0;
                    break;
                case "House":
                    this.Type = GameObjectEnum.House;
                    this.RunnerID = 0;
                    break;
                case "Wall":
                    this.Type = GameObjectEnum.Wall;
                    this.RunnerID = 0;
                    break;
            }
        }
        if (this.Distance < MIN_DIST) {
            this.Distance = MIN_DIST;
        }
    }

    // Converts the detected type to a number between -1 and 1 so it can be processed by neural network.
    public float GetConvertedTypeData() {
        switch (this.Type) {
            case GameObjectEnum.Runner:
                return 1f;
            case GameObjectEnum.Catcher:
                return -1f;
            case GameObjectEnum.Wall:
                return 0.5f;
            case GameObjectEnum.House:
                return -0.5f;
            default:
                return 0f;
        }
    }

    // Hide or show sensorEnd.
    public void ShowSensor(bool show) {
        this.sensorEnd.gameObject.SetActive(show);
    }
}
