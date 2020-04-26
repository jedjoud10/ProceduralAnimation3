using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicCharacterController : MonoBehaviour
{
    public float cameraSensivity;//How fast to rotate the player and camera from the mouse input
    public Transform playerCamera;
    public float speed;//Speed of the player
    public float gravity;//Gravity force applied to player
    public float jump;//Jump force when player is jumping
    private CharacterController cr;
    [HideInInspector]
    public Vector3 movement;//Delta movement applied to the CharacterController
    [HideInInspector]
    public Vector2 input;//Input movement from the keyboard (WASD)
    [HideInInspector]
    public Vector3 velocity;//The current velocity of the player (World velocity but in local space)
    [HideInInspector]
    public Vector3 acceleration;//The acceleration of the player
    [HideInInspector]
    public Vector2 localVelocityPlane;//The heading direction of the player in local space
    [HideInInspector]
    public bool inAir;//If the character controller is in air
    private float CameraXAxisRotation;//The current rotation of the camera (Up-Down)
    private float CameraYAxisRotation;//The current rotation of the camera (Left-Right)
    private float PlayerYAxisRotation;//The current rotation of the player (Left-Right)
    public float MaxHeadRotation;//The maximum left and right head rotation before we rotate the body
    // Start is called before the first frame update
    void Start()
    {
        cr = GetComponent<CharacterController>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        input.x = Input.GetAxis("LeftRight"); input.y = Input.GetAxis("ForwardBackward");
        input = Vector2.ClampMagnitude(input, 1f);
        if (cr.isGrounded) 
        {
            movement.y = 0;
            if (Input.GetAxis("Jump") > 0) movement.y = jump;
        }
        inAir = !cr.isGrounded;
        movement.x = input.x * speed; movement.z = input.y * speed;
        movement.y -= gravity * Time.deltaTime;//Gravity as an acceleration

        //Camera rotation
        CameraYAxisRotation += Input.GetAxis("Mouse X") * cameraSensivity;//Rotate the player head around
        CameraXAxisRotation += Input.GetAxis("Mouse Y") * cameraSensivity;
        CameraXAxisRotation = Mathf.Clamp(CameraXAxisRotation, -90, 90);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, PlayerYAxisRotation, 0), 5f * Time.deltaTime);//Sets the player rotation
        if(CameraYAxisRotation - PlayerYAxisRotation > MaxHeadRotation || CameraYAxisRotation - PlayerYAxisRotation < -MaxHeadRotation) 
        {
            PlayerYAxisRotation = CameraYAxisRotation;
        }
        if(input.magnitude > 0.1f) 
        {
            PlayerYAxisRotation = CameraYAxisRotation;
        }
        //Velocity and movement stuff
        movement = transform.TransformDirection(movement);//Transform the world delta movement to local delta movement
        acceleration = transform.InverseTransformDirection(cr.velocity) - velocity;//Calculate acceleration
        CalculateLocalVelocityPlane();
        cr.Move(movement * Time.deltaTime);//Move the character controller
    }
    private void CalculateLocalVelocityPlane() 
    {
        velocity = cr.velocity;//World space velocity
        localVelocityPlane.x = velocity.x; localVelocityPlane.y = velocity.z;//Set world space heading direction
        velocity = transform.InverseTransformDirection(velocity);
        localVelocityPlane.y = velocity.z;
        localVelocityPlane.x = velocity.x;
    }
    private void LateUpdate()
    {
        playerCamera.rotation = Quaternion.Euler(CameraXAxisRotation, CameraYAxisRotation + 180, 0);//Set camera rotation
        CalculateLocalVelocityPlane();
    }
}
