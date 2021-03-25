using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// practice from Catlike Coding Unity Tutorial: 
// https://catlikecoding.com/unity/tutorials/basics/

public class Graph : MonoBehaviour
{

    [SerializeField]
    private Transform pointPrefab;

    [SerializeField, Range(10,100)] // enforces range on below variable
    private int resolution = 10;

    private Transform[] points;

    private void Awake()
    {
        float step = 2f / resolution;
        var scale = Vector3.one * step;
        var position = Vector3.zero;

        points = new Transform[resolution];

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = Instantiate(pointPrefab);
            point.SetParent(transform);

            position.x = (i + 0.5f) * step - 1f;

            point.localPosition = position;
            point.localScale = scale;

            points[i] = point;
        }
    }

    private void Update()
    {
        float time = Time.time;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            Vector3 position = point.localPosition;

            position.y = Mathf.Sin(Mathf.PI * (position.x + time));

            point.localPosition = position;
        }
    }
}
