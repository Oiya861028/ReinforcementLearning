using System;
using System.Linq;
using System.Threading;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// A Hummingbird Machine Learning Agent
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;
    
    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform of the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or playing mode")]
    public bool trainingMode;

    //The rigidbody of the agent
    new private Rigidbody rigidbody;

    //The flower area the agent is in
    private FlowerArea flowerArea;

    //The nearest flower to the agent
    private Flower nearestFlower;

    //Allow for smoother pitch changes
    private float smoothPitchChange = 0f;

    //Allow for smoother yaw changes
    private float smoothYawChange = 0f;

    //Maximum angle the bird can pitch up or down
    private const float MaxPitchAngle = 80f;

    //Maximum beak tip distance to accept nectar collision
    private const float BeakTipRadius = .008f;

    //Whether agent is frozen (intentionally not flying)
    private bool isFrozen = false;

    /// <summary>
    /// Track the amount of nectar agent collected
    /// </summary>
    public float nectarObtained {get; private set;}

    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();
        if(!trainingMode) MaxStep = 0;
    }

    /// <summary>
    /// Reset the agent when episode begin
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            //Only reset flower in training mode
            flowerArea.ResetFlowers();
        }

        //Reset nectar
        nectarObtained = 0f;

        //Zero out velocity so that movement stop before new episode begins
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        //Default to spawning in front of a flower
        bool inFrontOfFlower = true;
        if(trainingMode) 
        {
            //Spawn in front of a flower 50% of the time
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        //Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate nearest flower now that the agent has moved
        UpdateNearestFlower();
    }
    
    /// <summary>
    /// Called when an action is received from the player input or the neural network
    /// ActionBuffers contains two Accessors: ContiuousActions and DiscreteAction
    ///     - ContiuousActions store a (ActionSegment) list of float 
    ///         - actions.ContiousActions[i] represents:
    ///         - Index[0]: move vector x (-1 = left, 1 = right)
    ///         - Index[1]: move vector y (-1 = down, 1 = up)
    ///         - Index[2]: move vector z (-1 = backward, 1 = forward)
    ///         - Index[3]: pitch angle (-1 = pitch down, 1 = pitch up)
    ///         - Index[4]: yaw angle (-1 = turn left, 1 = turn right)
    ///         
    ///     - We will not be using DiscreteActions here
    /// </summary>
    /// <param name="actions">The actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Don't take actions if frozne
        if (isFrozen) return;

        // Calculate Movement Vector
        Vector3 move = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);

        // Add force in the direction of the move vector
        rigidbody.AddForce(move * moveForce);

        // Get the current rotation
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // Calculate the pitch and yaw rotations
        float pitchChange = actions.ContinuousActions[3];
        float yawChange = actions.ContinuousActions[4];

        // calculate smooth rotation
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Update Rotations
        float pitch = rotationVector.x + smoothPitchChange * pitchSpeed * Time.fixedDeltaTime;
        float yaw = rotationVector.y + smoothYawChange * yawSpeed * Time.fixedDeltaTime;

        //Clamp pitch to avoid flipping upside down 
        if (pitch > 180) pitch -= 360;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // If the nearestFlower is not defined yet, pass in empty observations and return
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }
        // Observe the agent's local rotation (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        // Get a vector from the beak tip to the nearest flower
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        // Observe the normalized vector pointing to the nearest flower (3 observations)
        sensor.AddObservation(toFlower.normalized);

        // Observe the dot product that indicate whether the beaktip is in front of the flower (1 observation)
        // (+1 means it's directly in front of the flower, -1 means it's directly behind the flower)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVectar.normalized));

        // Observe the dot product that indicate whether the beak is facing the flower (1 observation)
        // (+1 means it's directly facing toward the flower, -1 means it's directly facing away from the flower)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVectar.normalized));

        // Observe the relative distance from beaktip to the flower respective to the AreaDiameter (1 observation)
        sensor.AddObservation(toFlower.magnitude/FlowerArea.AreaDiameter);

        // 10 total observations
    }

    /// <summary>
    /// When Behavior Type is set to "Heuristic Only" on the agent's Behavior Parameters,
    /// this function will be called. Its return value will be fed into
    /// <see cref="OnActionReceived"> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">An output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Create placeholder for movement/turning
        Vector3 forward = Vector3.zero;
        Vector3 right = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // Convnert all keyboard input into movement and turning
        // All value between -1 and 1

        // Forward/Backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // Left/Right
        if (Input.GetKey(KeyCode.A)) right = transform.right;
        else if (Input.GetKey(KeyCode.S)) right = -transform.up;

        // Up/Down
        if (Input.GetKey(KeyCode.W)) up = transform.up;
        else if (Input.GetKey(KeyCode.S)) up = -transform.up;

        // Mouse pitch and yaw
        pitch = Input.GetAxis("Mouse X");
        yaw = Input.GetAxis("Mouse Y");

        // Combine movement vector and normalize
        Vector3 combined = (forward + right + up).normalized;

        // Add all actions to actionOut
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;
        continuousActionsOut[3] = pitch;
        continuousActionsOut[4] = yaw;
        // TODO: Check this actually work
    }
    /// <summary>
    /// Freeze the agent and prevent it from doing anything
    /// </summary>
    public void FreezeAgent() 
    {
        Debug.Assert(!trainingMode, "Freeze/Unfreeze not supported in training mode");
        isFrozen = true;
        rigidbody.Sleep();
    }
    /// <summary>
    /// Unfreeze the agent and resume action
    /// </summary>
    public void UnfreezeAgent() 
    {
        Debug.Assert(!trainingMode, "Freeze/Unfreeze not supported in training mode");
        isFrozen = false;
        rigidbody.WakeUp();
    }
    /// <summary>
    /// Move the agent to a safe random position (i.e. not colliding with anything)
    /// If in front of flower, also point beak at the direction of flower
    /// </summary>
    /// <param name="inFrontOfFlower">Whether to choose a path in front of flower</param>
    /// <exception cref="NotImplementedException"></exception>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;  // Prevent infinite loop
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        //Loop until a safe position is found or we run out of attempts 
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining -= 1;
            if (inFrontOfFlower)
            {
                //Pick a random flower
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                //Position 10 to 20 cm in front of the flower
                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVectar * distanceFromFlower;

                //Point beak at flower (bird head is center of transform)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                // Pick a random height from the ground
                float height = UnityEngine.Random.Range(1.2f, 2.5f);
                // Pick a random radius from the center
                float radius = UnityEngine.Random.Range(2f, 7f);
                // Pick a random direction rotated around the y-axis
                Quaternion direction = Quaternion.Euler(0, UnityEngine.Random.Range(-180f, 180f), 0);
                //Combine height, radius to form the potentialPosition
                potentialPosition = Vector3.up * height + direction * Vector3.forward * radius;

                //Choose and set a random pitch and yaw
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0);
            }

            //Check to see if the agent will collide with anything
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);


            //Safe position has been found if no colliders are overlapped
            safePositionFound = colliders.Length == 0;
        }
        
        Debug.Assert(safePositionFound, "Could not found a safe position to spawn");
        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }
    
    /// <summary>
    /// Update the nearest flower to the agent
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void UpdateNearestFlower()
    {
        float distanceToCurrentNearestFlower = 0f;
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                //No current nearest flower, and this flower has nectar, so set to this flower
                nearestFlower = flower;
                distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.FlowerCenterPosition, beakTip.position);
            }
            else if (flower.HasNectar) 
            {
                //Calculate distance to this flower and current nearest flower and compare it
                float distanceToFlower = Vector3.Distance(flower.FlowerCenterPosition, beakTip.position);
                if (!nearestFlower.HasNectar || distanceToFlower>distanceToCurrentNearestFlower)
                {  
                    nearestFlower = flower;
                    distanceToCurrentNearestFlower = distanceToFlower; 
                }
            }
        }
    }
    
    /// <summary>
    /// Call when agent collide with Trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterOrStay(other);
    }
    
    /// <summary>
    /// Call when agent's collider stay inside Trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnterOrStay(other);
    }

    /// <summary>
    /// Called when agent collided with Trigger or stay within Trigger
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnterOrStay(Collider collider)
    {
        // Check if agent has collided with nectar collider
        if (collider.CompareTag("nectar"))
        {
            Vector3 closetPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // Check if the closestPoint is indeed close to beakTip
            // Note: A collision with anything but the beakTip should not count
            if (Vector3.Distance(closetPointToBeakTip, beakTip.position) < BeakTipRadius)
            {
                // Look up flower for this nectar collider
                Flower flower = flowerArea.GetFlowerFromNectar(collider);
                nectarObtained += flower.Feed(.05f);

                // Attempt to take .01 nectar
                // Note: this is per fixed time stamp, so it happens every .02 second, or 50x per second
                float nectarReceived = flower.Feed(.01f);

                nectarObtained += nectarReceived;

                // If in training mode, calculate reward
                if(trainingMode)
                {
                    // Calculate reward for pointing straight at the flower
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -flower.FlowerUpVectar.normalized));
                    AddReward(.01f + bonus); // 0.01f for encouraging it to keep staying inside the collider
                }

                // If flower is empty, then update NearestFlower
                if(!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Called when agent collided with something solid
    /// </summary>
    /// <param name="collision">The collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if(trainingMode && collision.collider.CompareTag("boundary"))
        {
            // Collided with area boundary, give a negative reward
            AddReward(-.5f);
        }
    }
    
    /// <summary>
    /// Update every frame
    /// </summary>
    private void Update()
    {
        // Draw a line from the agent to the nearest flower
        if (nearestFlower != null) 
            Debug.DrawLine(transform.position, nearestFlower.FlowerCenterPosition, Color.green);

    }
    
    /// <summary>
    /// Update every .02 second
    /// </summary>
    private void FixedUpdate()
    {
        // Avoids scneraio where nearestFlower pollen is stolen by opponent and not updated
        if(nearestFlower != null && !nearestFlower.HasNectar)
            UpdateNearestFlower();
    }
}
