using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraMovement : MonoBehaviour
{
    public CameraProperties cameraProperties;
    
    //States
    private bool isBoosting;

    //Smooth Amounts
    private Vector3 smoothAmountMoved;
    private Vector3 smoothAmountLinearZoomed;
    private float smoothAmountZoomed;
    private float smoothRotationAngle;

    //Reference Velocities
    private float zoomVelocity;
    private float rotationVelocity;
    private Vector3 panningVelocity;
    private Vector3 linearZoomVelocity;

    //Transforms
    public Transform mainTarget;
    public Transform alternateTarget;
    private Transform currentTarget;
    public new Transform camera;

    Vector2 panningLimit;

    void Start(){
        MapController mapController = (MapController)FindObjectOfType(typeof(MapController));
        mapController.mapGeneratedEvent += OnMapGenerated;

        
    }
    
    void Update(){
        //Misc
        IsBoosting();
        UpdateTarget();
        
        //Movement Control
        if (!cameraProperties.focusMode){
            LinearCameraMovement();
        }
       
       
        //Rotation Control
        RotationCameraMovement();
        
        //Zoom Control
        if (cameraProperties.linearZoom){
            LinearZoom();
        }
        else{
            MouseZoom();
        }
    }
    void LateUpdate(){
        if (cameraProperties.focusMode){
            ChaseCameraMovement();
        }
    }
    void CalculatePanningLimit(){
        MapController mapController = (MapController)FindObjectOfType(typeof(MapController));
        panningLimit.x = 119 * mapController.width;
        panningLimit.y = 119 * mapController.height;

    }

    public void OnMapGenerated(){
        CalculatePanningLimit();
    }
    

    void UpdateTarget(){
        currentTarget = (cameraProperties.focusMode)? alternateTarget: mainTarget;
    }
    
    void ChaseCameraMovement(){
        ///Go from current Position to target position
        float distance = transform.position.y;
        Vector3 targetPosition = currentTarget.position-(camera.forward * distance);
        transform.position = targetPosition;
        
        //while (transform.position != targetPosition)
           // transform.position = Vector3.MoveTowards(transform.position, targetPosition, .01f*Time.deltaTime);
            //yield return null;
        
    }
    void LinearCameraMovement(){
        
        //Key Panning Controls
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        
        //Mouse Panning Controls
        if (cameraProperties.mousePan){
            if (Input.mousePosition.y >= Screen.height-cameraProperties.borderThickness){
                moveY = 1;
            }
            else if(Input.mousePosition.y <= cameraProperties.borderThickness){
                moveY = -1;
            }
            if (Input.mousePosition.x >= Screen.width-cameraProperties.borderThickness){
                moveX = 1;
            }
            else if(Input.mousePosition.x <= cameraProperties.borderThickness){
                moveX = -1;
            }
        }
        
        //Rig Translation
        Vector3 direction = new Vector3 (moveX, 0, moveY);
        Vector3 velocity = getCameraVelocity(direction.normalized);

        Vector3 targetAmountMoved = velocity * Time.deltaTime;
        smoothAmountMoved = Vector3.SmoothDamp(smoothAmountMoved, targetAmountMoved, ref panningVelocity, cameraProperties.smoothTimePan);
        transform.Translate(smoothAmountMoved, Space.Self);
        
        //Clamp Position based of limits
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(transform.position.x, -panningLimit.x, panningLimit.x);
        pos.z = Mathf.Clamp(transform.position.z, -panningLimit.y, panningLimit.y);
        transform.position = pos;
    }
    void RotationCameraMovement(){
        //Think about where to update Target Position
        UpdateTargetPosition();
        
        float rawRotationAngle = 0;

        //Keyboard Input
        if(Input.GetKey(KeyCode.Q)){ 
            rawRotationAngle = cameraProperties.rotationSpeed * cameraProperties.rotationSensitivity * Time.deltaTime; 
        }
        if (Input.GetKey(KeyCode.E)){
            rawRotationAngle = -cameraProperties.rotationSpeed * cameraProperties.rotationSensitivity * Time.deltaTime;
        }
        if ((Input.GetKey(KeyCode.E)) && (Input.GetKey(KeyCode.Q))){
            rawRotationAngle = 0;
        }
            
        //Mouse Input
        if (cameraProperties.mouseRotate){
            if (Input.GetMouseButton(2)){
                rawRotationAngle = Input.GetAxis("Mouse X")* cameraProperties.rotationSpeed * cameraProperties.rotationSensitivity* Time.deltaTime;
            }
        }
        smoothRotationAngle = Mathf.SmoothDamp(smoothRotationAngle, rawRotationAngle, ref rotationVelocity, cameraProperties.smoothTimeRot);
        transform.RotateAround(currentTarget.position, Vector3.up, smoothRotationAngle);
    }

    void UpdateTargetPosition(){
        RaycastHit hit;
        Physics.Raycast(transform.position, camera.forward, out hit, Mathf.Infinity);
        Debug.DrawRay(transform.position, camera.forward * hit.distance, Color.red);
        mainTarget.position = hit.point;
    }

    void IsBoosting(){
        isBoosting = (Input.GetKey(KeyCode.LeftShift))? true: false;
    }
    
    Vector3 getCameraVelocity(Vector3 direction){
        Vector3 velocity;
        velocity = (isBoosting)? direction * cameraProperties.panSpeed * cameraProperties.sensitivity * cameraProperties.boostMult: direction * cameraProperties.sensitivity * cameraProperties.panSpeed;
        return velocity;
    }

    void LinearZoom(){
        float rawZoom = Input.GetAxis("Mouse ScrollWheel");
        
        Vector3 direction = camera.forward;
        Vector3 velocity = rawZoom * cameraProperties.linearScrollSpeed * cameraProperties.linearScrollSensitivity * direction;
        Vector3 targetAmountZoomed = velocity * Time.deltaTime;
        smoothAmountLinearZoomed = Vector3.SmoothDamp(smoothAmountLinearZoomed, targetAmountZoomed, ref linearZoomVelocity, cameraProperties.smoothTimeZoom);
        
        Vector3 newPos = transform.position + smoothAmountLinearZoomed;
        float squaredDistanceToTarget = (newPos-currentTarget.position).sqrMagnitude;
        Vector3 [] MinMaxPositions = GetMinMaxPositions();
        
        if (squaredDistanceToTarget > cameraProperties.linearZoomMinMax.y*cameraProperties.linearZoomMinMax.y){
            transform.position = MinMaxPositions[1];
            smoothAmountLinearZoomed = Vector3.zero;
        }
        else if (squaredDistanceToTarget < cameraProperties.linearZoomMinMax.x * cameraProperties.linearZoomMinMax.x){
            transform.position = MinMaxPositions[0];
            smoothAmountLinearZoomed = Vector3.zero;
        }
        else{
            transform.Translate(smoothAmountLinearZoomed,Space.World);
        }
    }

    Vector3[] GetMinMaxPositions(){
        Vector3 direction = (currentTarget.position-transform.position).normalized;
        Vector3 maxPosition = currentTarget.position - (direction * cameraProperties.linearZoomMinMax.y);
        Vector3 minPosition = currentTarget.position - (direction * cameraProperties.linearZoomMinMax.x);
        Vector3 [] positions = new Vector3 [2];

        //Assign Max/Min positions
        positions[0] = minPosition;
        positions[1] = maxPosition;
        return positions;
    }

    void OnDrawGizmos(){
        if (currentTarget != null){
            Vector3 [] positions = GetMinMaxPositions();
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(positions[0], 1);
            Gizmos.DrawSphere(positions[1], 1);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(mainTarget.position,1);
        }
    }

    void MouseZoom(){
        float rawZoom = Input.GetAxis("Mouse ScrollWheel");
    
        float velocity = rawZoom * cameraProperties.scrollSpeed * cameraProperties.scrollSensitivity;
        float targetAmountZoomed = velocity * Time.deltaTime;
        smoothAmountZoomed = Mathf.SmoothDamp(smoothAmountZoomed, targetAmountZoomed, ref zoomVelocity, cameraProperties.smoothTimeZoom);
        transform.Translate(new Vector3 (0,smoothAmountZoomed,0));
        
        //Clamp Zoom Heights
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, cameraProperties.verticalZoomMinMax.x, cameraProperties.verticalZoomMinMax.y);
        transform.position = pos;

        //Prevent Velocity if at max/min
        if ((pos.y == cameraProperties.verticalZoomMinMax.x)||(pos.y == cameraProperties.verticalZoomMinMax.y)){
            smoothAmountZoomed = 0;
        }
        
        //Spherical Zoom Addon
        if (cameraProperties.sphericalZoom){
            float t = (pos.y-cameraProperties.verticalZoomMinMax.x)/(cameraProperties.verticalZoomMinMax.y-cameraProperties.verticalZoomMinMax.x);
            Quaternion startRotation = Quaternion.Euler(45,0,0);
            Quaternion endRotation = Quaternion.Euler(90,0,0); 
            camera.localRotation = Quaternion.Lerp(startRotation,endRotation,t);
        }
        
    }
}