using UnityEngine;

public class TriggerForwarder : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Forward trigger events to MazeGame on parent Robot
        var game = GetComponentInParent<MazeGame>();
        if (game != null)
        {
            game.SendMessage("OnTriggerEnter", other, SendMessageOptions.DontRequireReceiver);
        }
    }
}
