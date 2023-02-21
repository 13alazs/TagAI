using UnityEngine;


// Only job is to hide or show field of vision.
public class Vision : MonoBehaviour {
    // Sprite renderer of field of vision
    private SpriteRenderer spriteRenderer;

    // Unity method
    void Awake() {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Hide or show vision indicator.
    public void ShowVision(bool show) {
        this.spriteRenderer.enabled = show;
    }
}
