using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public AnimationCurve curve;
    public AnimationCurve curve2;
    public float strength;
    public float cycleTime;
    float currentTime;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = Vector3.zero;
        currentTime = 0;
        
    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;
        Vector3 pos = transform.position;
        float t = currentTime/cycleTime;
        
        pos.x = strength * curve.Evaluate(t);
        pos.z = strength * curve2.Evaluate(t);
        
        if (currentTime >= cycleTime){
            currentTime = 0;
        }

        transform.position = pos;
        
    }
}
