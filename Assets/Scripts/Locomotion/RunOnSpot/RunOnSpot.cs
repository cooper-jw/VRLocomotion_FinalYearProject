using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;
using UnityStandardAssets.ImageEffects;

public class RunOnSpot : MonoBehaviour {

    public enum ControllerButton
    {
        Grip,
        Trigger,
        TouchPad,
    }

    //Inspector Settings:
    [Header("Settings:")]
    [SerializeField]
    private ControllerButton runButton = ControllerButton.Grip;
    public AnimationCurve controllerSwingToMovementCurve  = new AnimationCurve(new Keyframe(0, 0, 1, 1), new Keyframe(1, 1, 1, 1));
    [SerializeField]
    private float controllerSpeedForMaxSpeed = 3.0f;
    [SerializeField]
    private float maxSpeed = 8.0f;
    [Range(0.0f, 2.0f)]
    [SerializeField]
    private float bothControllerCoefficient = 1.0f; //Coefficient for lowering/upping speed when using both controllers.
    [Range(0.0f, 2.0f)]
    [SerializeField]
    private float singleControllerCoefficient = 0.7f;
    [SerializeField]
    private GameObject colliderObject;

    [Space(10)]

    //Simulation sickness settings:
    [Header("Simulation Sickness Settings:")]
    [SerializeField]
    bool fovLimiterEnabled = true;
    [SerializeField]
    float fovLimitSize = 0.65f;
    [SerializeField]
    float fovFadeTime = 0.3f;


    //Controller Variables:
    private GameObject headObject;
    private GameObject leftControllerObject, rightControllerObject;
    private SteamVR_TrackedObject leftControllerTrackedObject, rightControllerTrackedObject;
    private SteamVR_Controller.Device leftController, rightController;
    private int leftControllerIndex, rightControllerIndex;
    // Controller Position Variables
    private Vector3 leftControllerLocalPosition;
    private Vector3 rightControllerLocalPosition;
    private Vector3 leftControllerPreviousLocalPosition;
    private Vector3 rightControllerPreviousLocalPosition;
    //Controller Input Variables:
    private Valve.VR.EVRButtonId runButtonId;
    private bool leftButtonPressed = false;
    private bool rightButtonPressed = false;
    //Animation Curve:
    private AnimationCurve storedSwingCurve;
    // Saved movement
    private float latestArtificialMovement;
    private Quaternion latestArtificialRotation;
    private bool isMoving = false;
    //Fov limiter variables:
    [SerializeField] VignetteAndChromaticAberration fovLimiter;
    float currentFovLimit = 0.0f;
    bool limitingFov = false;
    float fovTimer = 0.0f;

    /*
        Initialisation.
    */
    void Start()
    {
        if(DeviceManager._instance)
        {
            //Setup SteamVR Objects:
            headObject = DeviceManager._instance.GetHeadsetTransform().gameObject;
            leftControllerObject = DeviceManager._instance.GetControllerLeftHandObject();
            rightControllerObject = DeviceManager._instance.GetControllerRightHandObject();
            leftControllerTrackedObject = leftControllerObject.GetComponent<SteamVR_TrackedObject>();
            rightControllerTrackedObject = rightControllerObject.GetComponent<SteamVR_TrackedObject>();
            leftController = DeviceManager._instance.GetControllerLeftHand();
            rightController = DeviceManager._instance.GetControllerRightHand();
            leftControllerIndex = (int)leftControllerTrackedObject.index;
            rightControllerIndex = (int)rightControllerTrackedObject.index;

            //Convert button enum to button ID:
            switch (runButton)
            {
                case ControllerButton.Grip:
                    runButtonId = EVRButtonId.k_EButton_Grip;
                    break;
                case ControllerButton.TouchPad:
                    runButtonId = EVRButtonId.k_EButton_SteamVR_Touchpad;
                    break;
                case ControllerButton.Trigger:
                    runButtonId = EVRButtonId.k_EButton_SteamVR_Trigger;
                    break;
                default:
                    runButtonId = EVRButtonId.k_EButton_Grip;
                    break;
            }

            //Store animation curve incase it gets disabled in the inspector:
            storedSwingCurve = controllerSwingToMovementCurve;

            //Pre-seed controller positions:
            leftControllerLocalPosition = leftControllerObject.transform.localPosition;
            rightControllerLocalPosition = rightControllerObject.transform.localPosition;
            leftControllerPreviousLocalPosition = leftControllerLocalPosition;
            rightControllerPreviousLocalPosition = rightControllerLocalPosition;

            //Get image effect for fov limiting:
            if(fovLimiter == null)
                fovLimiter = DeviceManager._instance.GetHeadsetTransform().gameObject.GetComponentInChildren<VignetteAndChromaticAberration>();

        } else
        {
            Debug.LogError("FATAL: DeviceManager instance is not initialised!");
        }
    }
	
    /*
        Fixed update, this is called every fixed frame.
    */
	void FixedUpdate()
    {
        CheckInput();

        //Save controller positions:
        leftControllerLocalPosition = leftControllerObject.transform.localPosition;
        rightControllerLocalPosition = rightControllerObject.transform.localPosition;

        //If moving limit fov:
        if (leftButtonPressed || rightButtonPressed) limitingFov = true;
        else limitingFov = false;
        //If fov limiter is enabled, calculate:
        if (fovLimiterEnabled) CalculateFovLimit();

        //Calculate movement and add it to the position:
        transform.position += CalculateMovement();

        //Store current positions in previous positions:
        leftControllerPreviousLocalPosition = leftControllerLocalPosition;
        rightControllerPreviousLocalPosition = rightControllerLocalPosition;

        //Move player collider if it exists:
        if (colliderObject != null)
            colliderObject.transform.position = new Vector3(headObject.transform.position.x, 0.0f, headObject.transform.position.z);

    }

    /*
        Updates controller indecies as well as input booleans.
    */
    void CheckInput()
    {
        //LeftController:
        int newLeftIndex = (int)leftControllerTrackedObject.index;

        if(newLeftIndex != -1 && newLeftIndex != leftControllerIndex)
        {
            //LeftController has changed, update:
            leftControllerIndex = newLeftIndex;
            leftController = SteamVR_Controller.Input(newLeftIndex);
        }

        //Check LeftController input
        if (newLeftIndex != -1)
            leftButtonPressed = leftController.GetPress(runButtonId);
        else leftButtonPressed = false;

        //RightController:
        int newRightIndex = (int)rightControllerTrackedObject.index;

        if (newRightIndex != -1 && newRightIndex != rightControllerIndex)
        {
            //RightController has changed, update:
            rightControllerIndex = newRightIndex;
            rightController = SteamVR_Controller.Input(newRightIndex);
        }

        //Check RightController input:
        if (newRightIndex != -1)
            rightButtonPressed = rightController.GetPress(runButtonId);
        else rightButtonPressed = false;
    }

    /*
        Calculates movement and returns the amount to move this frame.
    */
    Vector3 CalculateMovement()
    {
        // Initialize movement variables:
        float movementAmount = 0f;
        Quaternion movementRotation = Quaternion.identity;
        bool hasMoved = CheckMovement(ref movementAmount, ref movementRotation);

        if(hasMoved)
        {
            isMoving = true;

            latestArtificialMovement = movementAmount;
            latestArtificialRotation = movementRotation;

            // Move forward in the X and Z axis only
            Vector3 forwardMovement = movementRotation * Vector3.forward * movementAmount;
            return new Vector3(forwardMovement.x, 0.0f, forwardMovement.z);
        }
        else
        {
            isMoving = false;
            return Vector3.zero;
        }
    }

    /*
        Checks if the player should move and calculates how much and the direction based off controller data.
        Returns true if moving and updates referance perameters.
        Movement - the amount to move this frame
        Rotation - the rotation of the movement
    */
    bool CheckMovement(ref float movement, ref Quaternion rotation)
    {
        if (leftButtonPressed && rightButtonPressed)
        {
            //Rotation is average of the two:
            rotation = AverageControllerRotation();

            //Calculate dif in controller vectors against last frame:
            float leftChange = Vector3.Distance(leftControllerPreviousLocalPosition, leftControllerLocalPosition);
            float rightChange = Vector3.Distance(rightControllerPreviousLocalPosition, rightControllerLocalPosition);
            //Calculate camera rig movement using linear curve:
            float leftMove = calculateMovement(leftChange, controllerSpeedForMaxSpeed, maxSpeed, storedSwingCurve);
            float rightMove = calculateMovement(rightChange, controllerSpeedForMaxSpeed, maxSpeed, storedSwingCurve);
            //As both controllers are being used, use coefficient to calculate movement amount:
            float controllerMovement = (leftMove + rightMove) / 2 * bothControllerCoefficient;

            movement = controllerMovement;

            return true;
        }
        else if (leftButtonPressed)
        {
            //Rotation is leftController rotation:
            rotation = leftControllerObject.transform.rotation;
            //Calculate dif in left vector against last frame:
            float leftChange = Vector3.Distance(leftControllerPreviousLocalPosition, leftControllerLocalPosition);
            //Calculate camera rig movement amount:
            float leftMove = calculateMovement(leftChange, controllerSpeedForMaxSpeed, maxSpeed, storedSwingCurve);
            //As one controller is being used use single coefficient:
            float controllerMovement = leftMove * singleControllerCoefficient;

            movement = controllerMovement;

            return true;
        }
        else if (rightButtonPressed)
        {
            //Rotation is leftController rotation:
            rotation = rightControllerObject.transform.rotation;
            //Calculate dif in left vector against last frame:
            float rightChange = Vector3.Distance(rightControllerPreviousLocalPosition, rightControllerLocalPosition);
            //Calculate camera rig movement amount:
            float rightMove = calculateMovement(rightChange, controllerSpeedForMaxSpeed, maxSpeed, storedSwingCurve);
            //As one controller is being used use single coefficient:
            float controllerMovement = rightMove * singleControllerCoefficient;

            movement = controllerMovement;

            return true;
        }
        else
            return false;
    }

    /*
        Returns the average rotation of the two controllers.
        If only one controller is connected it returns the rotation of only that one.
    */
    Quaternion AverageControllerRotation()
    {
        Quaternion newRotation;

        //Both controllers
        if (leftController != null && rightController != null)
            newRotation = Quaternion.Slerp(leftControllerObject.transform.rotation, rightControllerObject.transform.rotation, 0.5f);
        //LeftController only
        else if (leftController != null && rightController == null)
            newRotation = leftControllerObject.transform.rotation;
        //RightController only
        else if (rightController != null && leftController == null)
            newRotation = rightControllerObject.transform.rotation;
        //No controllers! (Error)
        else
            newRotation = Quaternion.identity;

        return newRotation;
    }

    /*
        Calculates movement based on the change in controller position against the maximum input,
        maximum speed and the animation curve passed in the parameters.
        Returns float of movement amount.
    */
    static float calculateMovement(float change, float maxInput, float maxSpeed, AnimationCurve curve)
    {
        float changeByTime = change / Time.deltaTime;
        return Mathf.Lerp(0, maxSpeed, curve.Evaluate(changeByTime / maxInput)) * Time.deltaTime;
    }

    /*
        Calculates current fov limit using settings from editor
        Then applies using image effects
    */
    void CalculateFovLimit()
    {
        if (fovLimiter == null) return;

        if (limitingFov && fovTimer < fovFadeTime && currentFovLimit != fovLimitSize)
        {
            fovTimer += Time.deltaTime;
            float perc = remap(fovTimer, 0.0f, fovFadeTime, 0.0f, 1.0f);
            currentFovLimit = Mathf.Lerp(0.0f, fovLimitSize, perc);
            fovLimiter.intensity = currentFovLimit;
        }
        else if (limitingFov && fovTimer >= fovFadeTime)
        {
            fovTimer = 0.0f;
            currentFovLimit = fovLimitSize;
            fovLimiter.intensity = fovLimitSize;
        }
        else if (!limitingFov && currentFovLimit > 0.0f && fovTimer < fovFadeTime)
        {
            fovTimer += Time.deltaTime;
            float perc = remap(fovTimer, 0.0f, fovFadeTime, 0.0f, 1.0f);
            currentFovLimit = Mathf.Lerp(fovLimitSize, 0.0f, perc);
            fovLimiter.intensity = currentFovLimit;
        }
        else if (!limitingFov && currentFovLimit != 0.0f)
        {
            fovTimer = 0.0f;
            currentFovLimit = 0.0f;
            fovLimiter.intensity = 0.0f;
        }
    }

    /*
        Remaps a value (value) from one range (from1-from2) to another range (to1 - to2)
    */
    public static float remap(float value, float from1, float from2, float to1, float to2)
    {
        return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
    }
}
