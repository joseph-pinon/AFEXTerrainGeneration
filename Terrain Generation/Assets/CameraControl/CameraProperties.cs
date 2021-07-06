using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "CameraProperties", order = 1)]
public class CameraProperties : ScriptableObject
{
    [Header("Sensitivity Settings")]
    [Range(0f, 1f)]
    public float sensitivity;
    [Range(0f, 1f)]
    public float rotationSensitivity;
    [Range(0f, 1f)]
    public float scrollSensitivity;
    [Range(0f, 1f)]
    public float linearScrollSensitivity;
    public float borderThickness;
    
    [Header("Speed Settings")]
    public float panSpeed;
    public float rotationSpeed;
    public float scrollSpeed;
    public float linearScrollSpeed;

    //Speed Boost Settings
    public float boostMult;
    
    //Camera Restrictions
    [Header("Restrictions")]
    public Vector2 linearZoomMinMax;
    public Vector2 verticalZoomMinMax;
    
    //Modes
    [Header("Modes")]
    public bool mousePan;
    public bool mouseRotate;
    public bool sphericalZoom;
    public bool linearZoom;
    public bool focusMode;

    //Smooth Time Values
    [Header("Smooth Time Values")]
    public float smoothTimePan;
    public float smoothTimeZoom;
    public float smoothTimeRot;
}
