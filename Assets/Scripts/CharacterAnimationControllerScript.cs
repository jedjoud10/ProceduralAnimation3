using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Makes procedural animations for the player
public class CharacterAnimationControllerScript : MonoBehaviour
{
    [Header("Player")]
    public BasicCharacterController controller;//The basic movement controller
    public Transform playerModel;
    [Header("Rotation")]
    public Vector2 AccelerationTiltFactor;//How much to multiply the acceleration tilt
    public Vector2 VelocityTiltFactor;//How much to multiply the velocity tilt
    public float AccelerationTiltSmoothness;//How much to smooth the acceleration to apply to the playerm model to make it tilt
    public float RotationSmoothness;//How much to smooth the rotation of the player model
    [Header("Root Bone")]
    public Transform RootBone;
    public float RootBoneRotationSmoothness;//How much to smooth the local rotation of the root bone
    [Header("Feet")]
    public Transform LeftFootRayOrigin;//Position where we will shoot a ray down from the left foot
    public Transform RightFootRayOrigin;//Position where we will shoot a ray down from the right foot
    public Transform LeftFootPole;//Foot pole where the foot is going to look at (IK)
    public Transform RightFootPole;//Foot pole where the foot is going to look at (IK)
    public float FeetRotationSmoothing;//How much to smooth the local rotation of the feet

    public Transform LeftFootTarget;//The target object that will be offseted and rotated procedurally
    public Transform RightFootTarget;//The target object that will be offseted and rotated procedurally

    public float DistanceBetweenGaitPoints;//Distance between two gaits's PlayerVelocity point

    public List<ProceduralAnimationGaitStruct> Gaits;
    private ProceduralAnimationGaitStruct CurrentGait;//Current Gait

    private RaycastHit hit;//Hit for the ray cast (Made it a variable to save on performance)
    private float LeftFootHeightOffset;//How much to offset the LeftFootTargetPos in the Y axis
    private float RightFootHeightOffset;//How much to offset the RightFootTargetPos in the Y axis
    private Vector3 smoothAcceleration;//Smoothed out acceleration of player
    private Vector3 LeftFootTargetPos;//The position of the target of the left foot (IK)
    private Vector3 RightFootTargetPos;//The position of the target of the right foot (IK)

    //Struct handling all of the parameters when walking/running etc...
    [System.Serializable]
    public class ProceduralAnimationGaitStruct
    {
        public string Name;//Name of this Gait
        public Vector2 PlayerVelocity;//The velocity of the player that we are going to interpolate from
        public float FeetSpeed;//Speed of how fast to ondulate the feet height offset
        public float FeetHeightFactor;//How much to move the feet up and down (Multiplication)
        public float FeetPosSmoothing;//How much to smooth between the feet's old position and the new position
        public Vector3 RootBoneRotation;//The offset that will be applied to the root bone's rotation
        public Vector3 LeftFootOriginOffset;//How much to offset the local position of the left foot ray origin
        public Vector3 RightFootOriginOffset;//How much to offset the local position of the right foot ray origin
        public Vector3 LeftFootPoleOffset;//How much to offset the local position of the left foot pole
        public Vector3 RightFootPoleOffset;//How much to offset the local position of the left foot pole
        public Vector3 LeftFootRotationOffset;//How much to offset the local rotation of the left foot
        public Vector3 RightFootRotationOffset;//How much to offset the local rotation of the right foot
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentGait = Gaits[0];//Set first gait to be the idle gait
    }

    // Update is called once per frame
    void Update()
    {
        //Acceleration tilt
        smoothAcceleration = Vector3.Lerp(smoothAcceleration, controller.acceleration, Time.deltaTime * AccelerationTiltSmoothness);//Smooth out the acceleration
        playerModel.localRotation = Quaternion.Slerp(playerModel.localRotation, Quaternion.Euler(controller.velocity.z * VelocityTiltFactor.y + smoothAcceleration.z * AccelerationTiltFactor.y, 0, controller.velocity.x * VelocityTiltFactor.x + smoothAcceleration.x * -AccelerationTiltFactor.x), RotationSmoothness * Time.deltaTime);//Create acceleration tilt
        
        RootBone.localRotation = Quaternion.Slerp(RootBone.localRotation, Quaternion.Euler(CurrentGait.RootBoneRotation), RootBoneRotationSmoothness * Time.deltaTime);
    }
    //Late Update is after the animation is done 
    private void LateUpdate()
    {
        SelectGait();//Select the appropriate gait
        CalculateFeetHeight();
        //Calculate feet height offsets

        FootLogic(LeftFootRayOrigin, LeftFootPole, LeftFootTarget, ref LeftFootTargetPos, LeftFootHeightOffset, CurrentGait.LeftFootOriginOffset, CurrentGait.LeftFootPoleOffset, CurrentGait.LeftFootRotationOffset, CurrentGait);
        FootLogic(RightFootRayOrigin, RightFootPole, RightFootTarget, ref RightFootTargetPos, RightFootHeightOffset, CurrentGait.RightFootOriginOffset, CurrentGait.RightFootPoleOffset, CurrentGait.RightFootRotationOffset, CurrentGait);

        LeftFootTarget.position = LeftFootTargetPos;
        RightFootTarget.position = RightFootTargetPos;
    }
    //Moves the feet offset up and down
    private void CalculateFeetHeight() 
    {
        LeftFootHeightOffset = Mathf.Sin(Time.time * CurrentGait.FeetSpeed) * CurrentGait.FeetHeightFactor;
        RightFootHeightOffset = Mathf.Sin(Time.time * CurrentGait.FeetSpeed + Mathf.PI) * CurrentGait.FeetHeightFactor;
    }
    //Vector equals with a precision
    public bool V3Equal(Vector3 a, Vector3 b)
    {
        return Vector3.SqrMagnitude(a - b) < 0.1;
    }
    //All the logic for a single foot
    private void FootLogic(Transform RayOrigin, Transform FootPole, Transform FootTarget, ref Vector3 FootTargetPos, float HeightOffset, Vector3 FeetOriginOffset, Vector3 FeetPoleOffset, Vector3 FeetTargetRotationOffset, ProceduralAnimationGaitStruct Gait)
    {
        RayOrigin.localPosition = FeetOriginOffset;//Set new and updated local position for the current ray origin object
        FootPole.localPosition = FeetPoleOffset;//Pole offset in local 
        Vector3 newFootTargetPos = FootTargetPos;//The new and updated foot target position
        if (Physics.Raycast(RayOrigin.position, Vector3.down * 10, out hit))
        {
            Debug.DrawLine(RayOrigin.position, hit.point);
            newFootTargetPos.y = hit.point.y;//Reset Y axis pos
            if (HeightOffset > 0)//Move the feet target potition only when it is in air
            {
                newFootTargetPos = hit.point;//Set new foot target pos
                Vector3 distanceVector = FootTargetPos - newFootTargetPos; distanceVector.y = 0;
                if (distanceVector.sqrMagnitude > 0.01f)//Can we offset the feet in the Y axis or not ?
                {
                    newFootTargetPos.y += HeightOffset;//Offset Y axis by the height value
                }
            }
            FootTargetPos = Vector3.Lerp(FootTargetPos, newFootTargetPos, Time.deltaTime * Gait.FeetPosSmoothing);//Set new left foot target pos with smoothing
            FootTarget.localRotation = Quaternion.Slerp(FootTarget.localRotation, Quaternion.Euler(FeetTargetRotationOffset), FeetRotationSmoothing * Time.deltaTime);
        }

    }
    //Loops over the gaits and selects the current one
    private void SelectGait() 
    {
        //Gets the max distance
        //Debug.Log("Current player velocity : " + controller.localVelocityPlane);
        for(int i = 0; i < Gaits.Count; i++)//Loop
        {
            //Condition to select the gait
            ProceduralAnimationGaitStruct gait = Gaits[i];
            Vector2 input = controller.localVelocityPlane / controller.speed;
            input.x = Mathf.Clamp(input.x * 1, -DistanceBetweenGaitPoints, DistanceBetweenGaitPoints);//Clamp and multiply (Multiplication is for responsivness)
            input.y = Mathf.Clamp(input.y * 1, -DistanceBetweenGaitPoints, DistanceBetweenGaitPoints);//Clamp and multiply (Multiplication is for responsivness)
            input = Vector2.ClampMagnitude(input, 1);
            float dist = 1 - (Vector2.Distance(input, gait.PlayerVelocity));//Inverted and normalized distance
            
            CurrentGait = LerpGaits(CurrentGait, gait, dist);//Lineraly interpolates using the distance to 2D gait points
            
        }
    }    
    //Interpolates between two gaits
    private ProceduralAnimationGaitStruct LerpGaits(ProceduralAnimationGaitStruct a, ProceduralAnimationGaitStruct b, float t) 
    {
        ProceduralAnimationGaitStruct outputGait = new ProceduralAnimationGaitStruct();
        //Debug.Log("Interpolant is : " + t);
        //Interpolation
        outputGait.Name = a.Name;
        outputGait.FeetSpeed = Mathf.Lerp(a.FeetSpeed, b.FeetSpeed, t);
        outputGait.FeetHeightFactor = Mathf.Lerp(a.FeetHeightFactor, b.FeetHeightFactor, t);
        outputGait.FeetPosSmoothing = Mathf.Lerp(a.FeetPosSmoothing, b.FeetPosSmoothing, t);
        outputGait.RootBoneRotation = Vector3.Lerp(a.RootBoneRotation, b.RootBoneRotation, t);
        outputGait.LeftFootOriginOffset = Vector3.Lerp(a.LeftFootOriginOffset, b.LeftFootOriginOffset, t);
        outputGait.RightFootOriginOffset = Vector3.Lerp(a.RightFootOriginOffset, b.RightFootOriginOffset, t);
        outputGait.LeftFootPoleOffset = Vector3.Lerp(a.LeftFootPoleOffset, b.LeftFootPoleOffset, t);
        outputGait.RightFootPoleOffset = Vector3.Lerp(a.RightFootPoleOffset, b.RightFootPoleOffset, t);
        outputGait.LeftFootRotationOffset = Vector3.Lerp(a.LeftFootRotationOffset, b.LeftFootRotationOffset, t);
        outputGait.RightFootRotationOffset = Vector3.Lerp(a.RightFootRotationOffset, b.RightFootRotationOffset, t);
        if (t > 0.5f) 
        {
            outputGait.Name = b.Name;//No interpolation with names
        }

        return outputGait;
    }
    //Draw gizmos
    private void OnDrawGizmos()
    {
        //Draw feet target positions gizmos
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(LeftFootTarget.position, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(RightFootTarget.position, 0.5f);
    }
}
