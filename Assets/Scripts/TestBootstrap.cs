using UnityEngine;

public class TestBootstrap : MonoBehaviour
{
    public ActDefinition testAct;

    void Start()
    {
        if (testAct == null)
        {
            Debug.LogWarning("[TestBootstrap] No testAct assigned.");
            return;
        }
        GameManager.Instance.StartGame(testAct);
    }
}