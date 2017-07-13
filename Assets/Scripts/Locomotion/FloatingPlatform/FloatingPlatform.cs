using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class FloatingPlatform : MonoBehaviour {
    //Constants:
    public const float TiltDeadZone = 15.0f;
    public const float Tilt = 20.0f;
    public static readonly Vector3 ControllerRotationOffset = new Vector3(-45.0f, 90.0f, 0.0f);
    public static Vector3 RotationSpeed = new Vector3(0.0f, 50.0f, 0.0f); //only used when comfort mode is disabled
    public const float NavMeshSpeedMax = 15;

    [Header("Settings:")]
    [SerializeField] private bool toggleRollCage = false;
    [SerializeField] private DeviceManager.Controllers inputController = DeviceManager.Controllers.Right;
    [SerializeField] private bool comfortMode = true;
    [SerializeField] private float accelerationMultiplier = 1.0f;
    [SerializeField] private float maxSpeed = 10.0f;
    [SerializeField] private GameObject rollcageHolder;
    public NavMeshAgent agent;

    //Controller Variables:
    private GameObject controllerObject;
    private SteamVR_TrackedObject controllerTrackedObject;
    private SteamVR_Controller.Device controller;
    private int controllerIndex;
    private bool isHairTrigger = false;

    private Vector3 vel;
    private bool isRollCageEnabled = false;

    public Vector3 GetVelocity()
    {
        return vel;
    }

    // Use this for initialization
    void Start () {
        if (DeviceManager._instance)
        {
            if(inputController == DeviceManager.Controllers.Left)
            {
                //Setup left controller for input:
                controllerObject = DeviceManager._instance.GetControllerLeftHandObject();
                controllerTrackedObject = controllerObject.GetComponent<SteamVR_TrackedObject>();
                controller = DeviceManager._instance.GetControllerLeftHand();
                controllerIndex = (int)controllerTrackedObject.index;
            }
            else
            {
                //Setup right controller for input:
                controllerObject = DeviceManager._instance.GetControllerRightHandObject();
                controllerTrackedObject = controllerObject.GetComponent<SteamVR_TrackedObject>();
                controller = DeviceManager._instance.GetControllerRightHand();
                controllerIndex = (int)controllerTrackedObject.index;
            }

            agent.speed = NavMeshSpeedMax;
            SetComfortMode(comfortMode);
        } else Debug.LogError("FATAL: DeviceManager instance is not initialised!");
	}
	
    //Update called each frame
	void FixedUpdate () {
        if (toggleRollCage) ToggleRollCage();

        Vector3 acc = Vector3.zero;

        CheckInput();
        CalculateAcceleration(ref acc);

        acc *= accelerationMultiplier;

        // deceleration
        acc += -1.5f * vel;

        //Calculate new position using acceleration and velocity:
        Vector3 newPos = (0.5f * acc * Mathf.Pow(Time.fixedDeltaTime, 2.0f)) +
                                            (vel * Time.fixedDeltaTime) +
                                            transform.position;
        vel = (acc * Time.fixedDeltaTime) + vel;

        //Apply new position:
        this.transform.position = newPos;

        if (acc != Vector3.zero)
            agent.ResetPath();
    }


    /*
        Calculate acceleration based upon controller input
    */
    private void CalculateAcceleration(ref Vector3 acc)
    {
        if(!isHairTrigger)
        {
            //If the trigger isn't being held, we shouldn't be accelerating so return:
            return;
        }

        Vector3 controllerRot = eulerAngleToNegEuler(controllerObject.transform.eulerAngles);
        Vector3 epsilonRot = controllerRot - ControllerRotationOffset;

        float angleX = remapAngle(epsilonRot.x);
        float angleZ = -remapAngle(epsilonRot.z);

        if (controllerRot.y > -45 && controllerRot.y <= 45)
        {
            // Forward & Back
            acc.z = angleX;

            // Left & Right
            if (comfortMode)
                acc.x = angleZ;
            else if (angleZ != 0)
                UncomfortableRotate(angleZ);
        }
        else if (controllerRot.y > 45 && controllerRot.y <= 135)
        {
            // Forward & Back
            acc.x = angleX;

            // Left & Right
            if (comfortMode)
                acc.z = -angleZ;
            else if (angleZ != 0)
                UncomfortableRotate(angleZ);
        }
        else if (controllerRot.y > -135 && controllerRot.y <= -45)
        {
            // Forward & Back
            acc.x = -angleX;

            // Left & Right
            if (comfortMode)
                acc.z = angleZ;
            else if (angleZ != 0)
                UncomfortableRotate(angleZ);
        }
        else
        {
            // print("back");
            // Forward & Back
            acc.z = -angleX;

            // Left & Right
            if (comfortMode)
                acc.x = -angleZ;
            else if (angleZ != 0)
                UncomfortableRotate(angleZ);
        }
    }

    private void UncomfortableRotate(float angle)
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Euler(angle * RotationSpeed * Time.fixedDeltaTime);
        transform.rotation = deltaRotation;
    }

    /*
        Toggles platform roll cage
    */
    private void ToggleRollCage()
    {
        if(isRollCageEnabled)
        {
            //Disable:
            if (rollcageHolder != null) rollcageHolder.SetActive(false);
            toggleRollCage = isRollCageEnabled = false;
        } else
        {
            //Enable:
            if (rollcageHolder != null) rollcageHolder.SetActive(true);
            toggleRollCage = false;
            isRollCageEnabled = true;
        }
    }

    /*
       Updates controller indecies as well as input booleans.
    */
    void CheckInput()
    {
        if (inputController == DeviceManager.Controllers.Left)
        {
            //LeftController:
            int newLeftIndex = (int)controllerTrackedObject.index;

            if (newLeftIndex != -1 && newLeftIndex != controllerIndex)
            {
                //LeftController has changed, update:
                controllerIndex = newLeftIndex;
                controller = SteamVR_Controller.Input(newLeftIndex);
            }

            //Check LeftController input
            if (newLeftIndex != -1)
                isHairTrigger = controller.GetHairTrigger();
            else isHairTrigger = false;
        }
        else
        {
            //RightController:
            int newRightIndex = (int)controllerTrackedObject.index;

            if (newRightIndex != -1 && newRightIndex != controllerIndex)
            {
                //RightController has changed, update:
                controllerIndex = newRightIndex;
                controller = SteamVR_Controller.Input(newRightIndex);
            }

            //Check RightController input:
            if (newRightIndex != -1)
                isHairTrigger = controller.GetHairTrigger();
            else isHairTrigger = false;
        }
    }

    private void SetComfortMode(bool value)
    {
        comfortMode = value;
        agent.updateRotation = !comfortMode;
    }

    /*
        Inverts euler angle given as parameter
    */
    public static Vector3 eulerAngleToNegEuler(Vector3 euler)
    {
        euler.x = (euler.x > 180) ? euler.x - 360 : euler.x;
        euler.y = (euler.y > 180) ? euler.y - 360 : euler.y;
        euler.z = (euler.z > 180) ? euler.z - 360 : euler.z;
        return euler;
    }

    /*
        Remaps an angle based upon Tilt and angle constants defined 
    */
    public static float remapAngle(float angle)
    {
        angle = (angle > 180) ? angle - 360 : angle;

        if (Mathf.Abs(angle) < TiltDeadZone) angle = 0;
        else angle = Mathf.Clamp(angle, -Tilt, Tilt);

        angle = remap(angle, -Tilt, Tilt, -1.0f, 1.0f);
        return angle;
    }

    /*
        Remaps a value (value) from one range (from1-from2) to another range (to1 - to2)
    */
    public static float remap(float value, float from1, float from2, float to1, float to2)
    {
        return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
    }
}
