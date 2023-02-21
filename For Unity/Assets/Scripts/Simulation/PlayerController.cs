using UnityEngine;


// This is a general controller class for a player,
// containing every data and logic that is needed for a player in the game.
public class PlayerController : MonoBehaviour {
    // Sprites for animation
    [SerializeField]
    private Sprite SpriteRightRun1_Runner;
    [SerializeField]
    private Sprite SpriteRightRun2_Runner;
    [SerializeField]
    private Sprite SpriteLeftRun1_Runner;
    [SerializeField]
    private Sprite SpriteLeftRun2_Runner;
    [SerializeField]
    private Sprite SpriteUpRun1_Runner;
    [SerializeField]
    private Sprite SpriteUpRun2_Runner;
    [SerializeField]
    private Sprite SpriteDownRun1_Runner;
    [SerializeField]
    private Sprite SpriteDownRun2_Runner;
    [SerializeField]
    private Sprite SpriteRightStill_Runner;
    [SerializeField]
    private Sprite SpriteLeftStill_Runner;
    [SerializeField]
    private Sprite SpriteUpStill_Runner;
    [SerializeField]
    private Sprite SpriteDownStill_Runner;

    [SerializeField]
    private Sprite SpriteRightRun1_Catcher;
    [SerializeField]
    private Sprite SpriteRightRun2_Catcher;
    [SerializeField]
    private Sprite SpriteLeftRun1_Catcher;
    [SerializeField]
    private Sprite SpriteLeftRun2_Catcher;
    [SerializeField]
    private Sprite SpriteUpRun1_Catcher;
    [SerializeField]
    private Sprite SpriteUpRun2_Catcher;
    [SerializeField]
    private Sprite SpriteDownRun1_Catcher;
    [SerializeField]
    private Sprite SpriteDownRun2_Catcher;
    [SerializeField]
    private Sprite SpriteRightStill_Catcher;
    [SerializeField]
    private Sprite SpriteLeftStill_Catcher;
    [SerializeField]
    private Sprite SpriteUpStill_Catcher;
    [SerializeField]
    private Sprite SpriteDownStill_Catcher;

    // Used for smooth animation
    private int spriteTickTime = 10;
    private bool spriteTick = true;

    // Used for unique ID generation
    private static int staticID = 0;

    // Returns the next unique id in the sequence. There is no 0 ID on purpose.
    private static int UniqueID {
        get {
            return ++staticID;
        }
    }

    // Returns id of this instance.
    public int ID {
        get;
        private set;
    }

    // Determine maximum time of a round.
    public float RoundTime {
        get;
        private set;
    }

    // Returns catch score or lifespan.
    public float Score {
        get;
        private set;
    }

    // AI datas for the player.
    public Brain Brain {
        get;
        set;
    }

    // Runner or Catcher.
    public GameObjectEnum PlayerType = GameObjectEnum.Runner;

    // Movement component for player
    public PlayerMovement Movement {
        get;
        private set;
    }

    // SpriteRenderer of this player used for visibility settings and animation.
    public SpriteRenderer SpriteRendererOfBody {
        get;
        private set;
    }

    // SpriteRenderer of this players vision used for visibility settings.
    public Vision Vision {
        get;
        private set;
    }

    // Collider of player used.
    public CircleCollider2D Collider {
        get;
        private set;
    }

    // Event for catching a runner. This should trigger score dealing on FieldManager if rule is set.
    public event System.Action CatchRunner;

    // Event for a runner dieing without being caught.
    // Could be because of hitting a wall or house.
    public event System.Action RunnerDiesWithoutCatch;

    // sensor components of player
    private Sensor[] sensors;
    // clock for checking when round is finished
    private float timeSinceRoundStart;


    // Unity method
    void Awake() {
        this.Movement = GetComponent<PlayerMovement>();
        this.SpriteRendererOfBody = GetComponent<SpriteRenderer>();
        this.Vision = GetComponentsInChildren<Vision>()[0];
        this.Collider = GetComponent<CircleCollider2D>();
        this.sensors = GetComponentsInChildren<Sensor>();
    }

    // Unity method
    void Start() {
        this.Movement.HitHouse += OnHouseContact;
        this.Movement.HitRunner += OnRunnerContact;
        this.Movement.HitCatcher += OnCatcherContact;
        this.Movement.HitWall += OnWallContact;

        //Set id and name to be unique
        this.ID = UniqueID;
        this.name = "Player (" + this.ID + ")";

        SetVisibilityAndCollider(true);
        this.RoundTime = Brain.RoundTime;
        this.timeSinceRoundStart = 0f;
        this.Score = 0f;
    }

    // Unity method
    void Update() {
        this.timeSinceRoundStart += Time.deltaTime;
    }

    // Unity method
    void FixedUpdate() {
        // Get readings from sensors
        double[] sensorOutput = new double[sensors.Length * 3];
        for (int i = 0; i < this.sensors.Length; i++) {
            sensorOutput[i * 3] = this.sensors[i].Distance;
            sensorOutput[i * 3 + 1] = this.sensors[i].GetConvertedTypeData();
            sensorOutput[i * 3 + 2] = this.sensors[i].RunnerID;
        }

        // Get control inputs
        double[] controlInputs = this.Brain.Network.ProcessInputs(sensorOutput);
        this.Movement.SetInputs(controlInputs);
        
        // invoke and animate movement
        SetSprite(this.Movement.GetFacing());

        // check if round still should be running
        if (this.timeSinceRoundStart > this.RoundTime) {
            OnTimeRunsOut();
        }
    }

    // Restarts this player, making it movable and visible again.
    public void Restart() {
        this.timeSinceRoundStart = 0f;
        this.Score = 0f;
        this.enabled = true;
        this.Movement.enabled = true;
        this.Brain.Reset();
        SetVisibilityAndCollider(true);
    }

    // Round is over, player gives itself score. Runners get maximum score. Everybody dies.
    public void OnTimeRunsOut() {
        if (this.PlayerType == GameObjectEnum.Runner) {
            if (Brain.isScoringTimeBased) {
                this.Score = this.RoundTime;
            }
        }
        SetVisibilityAndCollider(false);
        Brain.Die(this.Score);
        Stop();
    }

    // Runner should die. It might also get score depending on preset rules.
    private void OnHouseContact() {
        if (this.PlayerType == GameObjectEnum.Runner) {
            if (Brain.shouldSeeHouseToScore) {
                if (SeeHouse()) {
                    if (Brain.isScoringTimeBased) {
                        this.Score += this.RoundTime;
                        this.Score += this.RoundTime - this.timeSinceRoundStart;
                    } else {
                        this.Score = this.RoundTime;
                    }
                } else {
                    House.Counter--;
                }
            } else {
                if (Brain.isScoringTimeBased) {
                    this.Score += this.RoundTime;
                    this.Score += this.RoundTime - this.timeSinceRoundStart;
                } else {
                    this.Score = this.RoundTime;
                }
            }
            RunnerDiesWithoutCatch();
            SetVisibilityAndCollider(false);
            Brain.Die(this.Score);
            Stop();
        }
    }

    // Catchers should get points. This also might give other catchers proximity points.
    private void OnRunnerContact() {
        if (this.PlayerType == GameObjectEnum.Catcher) {
            CatchRunner();
            if (!Brain.teamworkBonusForCatchers) {
                this.Score += 10;
            }
        }
    }

    // Runners should die and get score according current time since roundstart.
    private void OnCatcherContact() {
        if (this.PlayerType == GameObjectEnum.Runner) {
            if (Brain.isScoringTimeBased) {
                this.Score = this.timeSinceRoundStart;
            }
            // FieldManager will hide it, stopping and dieing is enough
            Brain.Die(this.Score);
            Stop();
        }
    }

    // Any player who touches a wall should die.
    private void OnWallContact() {
        if (this.PlayerType == GameObjectEnum.Runner) {
            if (Brain.isScoringTimeBased) {
                this.Score = this.timeSinceRoundStart;
            }
            RunnerDiesWithoutCatch();
        }
        SetVisibilityAndCollider(false);
        Brain.Die(this.Score);
        Stop();
    }

    // Add points to score based on sensor distance from caught runner.
    // If sensor do not detect that runner, then add 0.
    public float CollectScoreForCatch(int runnerID) {
        float distance = GetClosestSensoryRead(runnerID);
        float bonusPoints = distance == -1f ? 0f : (Sensor.MAX_DIST - distance); 
        this.Score += bonusPoints;
        return bonusPoints;
    }

    // Returns closest sensor distance read from a runner identified by id.
    // If runner is not in field of vision then returns -1.
    private float GetClosestSensoryRead(int runnerID) {
        float distance = Sensor.MAX_DIST;
        for (int i = 0; i < this.sensors.Length; i++) {
            if (sensors[i].RunnerID == runnerID && distance > sensors[i].Distance) {
                distance = sensors[i].Distance;
            }
        }
        return distance == Sensor.MAX_DIST ? -1f : distance;
    }

    // Sets visibility and collider of player.
    // Also sets visibilty of vision indicators according to preset rules.
    public void SetVisibilityAndCollider(bool active) {
        this.SpriteRendererOfBody.enabled = active;
        this.Vision.ShowVision(active ? Brain.shouldShowVision : false);
        for (int i = 0; i < this.sensors.Length; i++) {
            this.sensors[i].ShowSensor(active ? Brain.shouldShowSensor : false);
        }
        this.Collider.enabled = active;
    }

    // Stops and disabled player.
    public void Stop() {
        Movement.enabled = false;
        Movement.Stop();
        this.enabled = false;
    }

    // Check if any sensor detects House gameobject.
    private bool SeeHouse() {
        for (int i = 0; i < this.sensors.Length; i++) {
            if (this.sensors[i].Type == GameObjectEnum.House) {
                return true;
            }
        }
        return false;
    }

    // This cycles the animation based on movement and current facing.
    private void SetSprite(FacingEnum facing) {
        if (spriteTickTime == 0) {
            spriteTick = !spriteTick;
            spriteTickTime = 10;
        } else {
            spriteTickTime--;
        }
        if (PlayerType == GameObjectEnum.Runner) {
            switch (facing) {
                case FacingEnum.Up:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteUpRun1_Runner;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteUpRun2_Runner;
                    }
                    break;
                case FacingEnum.Right:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteRightRun1_Runner;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteRightRun2_Runner;
                    }
                    break;
                case FacingEnum.Left:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteLeftRun1_Runner;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteLeftRun2_Runner;
                    }
                    break;
                case FacingEnum.Down:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteDownRun1_Runner;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteDownRun2_Runner;
                    }
                    break;
                case FacingEnum.UpStill:
                    SpriteRendererOfBody.sprite = SpriteUpStill_Runner;
                    break;
                case FacingEnum.RightStill:
                    SpriteRendererOfBody.sprite = SpriteRightStill_Runner;
                    break;
                case FacingEnum.LeftStill:
                    SpriteRendererOfBody.sprite = SpriteLeftStill_Runner;
                    break;
                case FacingEnum.DownStill:
                    SpriteRendererOfBody.sprite = SpriteDownStill_Runner;
                    break;
            }
        } else {
            switch (facing) {
                case FacingEnum.Up:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteUpRun1_Catcher;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteUpRun2_Catcher;
                    }
                    break;
                case FacingEnum.Right:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteRightRun1_Catcher;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteRightRun2_Catcher;
                    }
                    break;
                case FacingEnum.Left:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteLeftRun1_Catcher;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteLeftRun2_Catcher;
                    }
                    break;
                case FacingEnum.Down:
                    if (spriteTick){
                        SpriteRendererOfBody.sprite = SpriteDownRun1_Catcher;
                    } else {
                        SpriteRendererOfBody.sprite = SpriteDownRun2_Catcher;
                    }
                    break;
                case FacingEnum.UpStill:
                    SpriteRendererOfBody.sprite = SpriteUpStill_Catcher;
                    break;
                case FacingEnum.RightStill:
                    SpriteRendererOfBody.sprite = SpriteRightStill_Catcher;
                    break;
                case FacingEnum.LeftStill:
                    SpriteRendererOfBody.sprite = SpriteLeftStill_Catcher;
                    break;
                case FacingEnum.DownStill:
                    SpriteRendererOfBody.sprite = SpriteDownStill_Catcher;
                    break;
            }
        }
    }
}
