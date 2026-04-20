using System.Collections.Generic;
using UnityEngine;

public class AimLineRenderer : MonoBehaviour
{
    [Header("Dot Settings")]
    public int dotCount = 10;
    public float dotSpacing = 0.2f;
    public GameObject dotPrefab;

    private List<GameObject> dots = new List<GameObject>();

    public void Show(Vector2 origin, Vector2 dragVector, float maxDrag)
    {
        float t = dragVector.magnitude / maxDrag;
        Vector2 shotDirection = -dragVector.normalized;
        int activeDots = Mathf.RoundToInt(t * dotCount);

        while (dots.Count < activeDots)
            dots.Add(Instantiate(dotPrefab, transform));

        for (int i = 0; i < dots.Count; i++)
        {
            if (i < activeDots)
            {
                dots[i].SetActive(true);
                dots[i].transform.position = origin + shotDirection * (i + 1) * dotSpacing;
            }
            else
            {
                dots[i].SetActive(false);
            }
        }
    }

    public void Hide()
    {
        foreach (var dot in dots)
            if (dot != null) dot.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var dot in dots)
            if (dot != null) Destroy(dot);
        dots.Clear();
    }
}