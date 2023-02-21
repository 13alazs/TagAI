using UnityEngine;
using System.Collections;


// Handles player movement and collison.
public class PlayerMovement : MonoBehaviour {
    // Field of vision object sprite, to be set in Unity editor.
    [SerializeField]
    private GameObject vision;

    // Event for hitting runner.
    public event System.Action HitRunner;

    // Event for hitting catcher.
    public event System.Action HitCatcher;

    // Event for hitting house.
    public event System.Action HitHouse;

    // Event for hitting wall.
    public event System.Action HitWall;

    // Speed constants
    private const float FORWARD_SPEED = 400f;
    private const float SIDE_SPEED = 300f;
    private const float TURN_SPEED = 200f;

    // Velocity and rotation variables
    private float forwardVelocity;
    private float sideVelocity;
    private Quaternion rotation;

    // Input values
    private double forwardInput;
    private double turnInput;
    private double sideInput;

    // ID of last caught runner based on collision detection.
    public int CaughtRunnerID {
        get;
        set;
    }

    // The current inputs for setting forwardvelocity, rotation and sidevelocity in this order.
    public double[] CurrentInputs {
        get { return new double[] { forwardInput, turnInput, sideInput }; }
    }
 

    // Unity method
    void Start() {
        this.CaughtRunnerID = 0;
    }

    // Unity method
	void FixedUpdate() {
        ApplyInput();
        TransformPosition();
	}

    // Unity method
    void OnCollisionEnter2D(Collision2D collidingObject) {
        if (collidingObject.gameObject.tag == "Runner") {
            this.CaughtRunnerID = collidingObject.gameObject.GetComponent<PlayerController>().ID;
            HitRunner();
        }
        if (collidingObject.gameObject.tag == "Catcher") {
            HitCatcher();
        }
        if (collidingObject.gameObject.tag == "House") {
            HitHouse();
        }
        if (collidingObject.gameObject.tag == "Wall") {
            HitWall();
        }
    }

    // Applies the currently set inputs.
    private void ApplyInput() {
        LimitInputs();
        this.forwardVelocity = (float) this.forwardInput * FORWARD_SPEED * Time.deltaTime;
        this.sideVelocity = (float) this.sideInput * SIDE_SPEED * Time.deltaTime;
        CalculateRotation();
    }

    // All input values should be between -1 and 1.
    private void LimitInputs() {
        this.forwardInput = LimitInput(this.forwardInput);
        this.turnInput = LimitInput(this.turnInput);
        this.sideInput = LimitInput(this.sideInput);
    }

    // Set input value to be between -1 and 1.
    private double LimitInput(double input) {
        if (input > 1)
            return 1;
        else if (input < -1)
            return -1;
        return input;
    }

    // Sets the input values for movement calculations. Order: forward, turn, side.
    public void SetInputs(double[] input) {
        this.forwardInput = input[0];
        this.turnInput = input[1];
        // Override side input if sidestep is not enabled.
        if (Brain.canSidestep) {
            this.sideInput = input[2];
        } else {
            this.sideInput = 0;
        }
    }

    // Calculate new rotation by given turnInput and current rotation.
    private void CalculateRotation() {
        this.rotation = this.vision.transform.rotation;
        this.rotation *= Quaternion.AngleAxis((float)turnInput * TURN_SPEED * Time.deltaTime, new Vector3(0, 0, 1));
    }

    // Calculates the rotation and movement of player and transform its position accordingly.
    private void TransformPosition() {
        Vector3 forwardMovement = new Vector3(0, 1, 0);
        Vector3 sideMovement = new Vector3(1, 0, 0);
        this.vision.transform.rotation = this.rotation;
        // set where player is facing to
        forwardMovement = this.rotation * forwardMovement;
        sideMovement = this.rotation * sideMovement;

        this.transform.position += ((forwardMovement * this.forwardVelocity) + (sideMovement * this.sideVelocity)) * Time.deltaTime;
    }

    // Stops all current movement of player.
    public void Stop() {
        this.forwardVelocity = 0;
        this.sideVelocity = 0;
        this.rotation = Quaternion.AngleAxis(0, new Vector3(0, 0, 1));
    }

    // Returns facing of player determined by rotation of vision.
    public FacingEnum GetFacing() {
        float angle = this.vision.transform.localEulerAngles.z;
        if (this.forwardInput == 0 && this.sideInput == 0) {
            if (angle >= 45f && angle <= 135f){
                return FacingEnum.LeftStill;
            }
            if (angle >= 135f && angle <= 225f){
                return FacingEnum.DownStill;
            }
            if (angle >= 225f && angle <= 315f){
                return FacingEnum.RightStill;
            }
            if ((angle >= 315f && angle <= 360f) || (angle >= 0f && angle <= 45f)){
                return FacingEnum.UpStill;
            }
        }
        if (angle >= 45f && angle <= 135f){
            return FacingEnum.Left;
        }
        if (angle >= 135f && angle <= 225f){
            return FacingEnum.Down;
        }
        if (angle >= 225f && angle <= 315f){
            return FacingEnum.Right;
        }
        if ((angle >= 315f && angle <= 360f) || (angle >= 0f && angle <= 45f)){
            return FacingEnum.Up;
        }
        return FacingEnum.DownStill;
    }
}
