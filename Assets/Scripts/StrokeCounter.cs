using UnityEngine;
using UnityEngine.Events;

public class StrokeCounter : MonoBehaviour
{
    public int maxStrokes = 5;

    public UnityEvent onStrokeTaken;
    public UnityEvent onMaxStrokesReached;

    private int strokeCount = 0;
    public int StrokeCount => strokeCount;

    public void RegisterStroke()
    {
        if (strokeCount >= maxStrokes) return;

        strokeCount++;
        onStrokeTaken.Invoke();

        if (strokeCount >= maxStrokes)
        {
            DebugHUD.Log("Out of strokes!");
            onMaxStrokesReached.Invoke();
        }
    }

    public void Reset()
    {
        strokeCount = 0;
    }
}