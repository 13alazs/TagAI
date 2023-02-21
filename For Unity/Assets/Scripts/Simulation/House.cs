using UnityEngine;
using System.Collections;


// Class representing a safehouse.
public class House : MonoBehaviour {
    // Counts how many runners are safe at this moment.
    public static int Counter {
        get;
        set;
    }

    // Unity method
    void OnCollisionEnter2D(Collision2D collidingObject) {
        if (collidingObject.gameObject.tag == "Runner")
            Counter++;
    }

    // Restart counter for next round.
    public static void restartCounter() {
        Counter = 0;
    }
}
