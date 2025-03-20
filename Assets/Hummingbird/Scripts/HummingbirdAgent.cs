using System;
using System.Linq;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
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
}
