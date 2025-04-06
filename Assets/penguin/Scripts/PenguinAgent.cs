using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
public class PenguinAgent : Agent
{
    [Tooltip("How fast the agent moves forward")]
    public float speed = 5f;

    [Tooltip("How fast the agent turns")]
    public float turnSpeed = 100f;

    [Tooltip("The prefab of the red heart that appears when baby is fed")]
    public GameObject redHeartPrefab;

    [Tooltip("The prefab of the regurgitated fish when the baby is fed")]
    public GameObject regurgitatedFishPrefab;

    // The area the agent is in
    private PenguinArea penguinArea;

    // The agent's regidbody
    new private Rigidbody rigidbody;

    // The baby penguin prefab
    private GameObject baby;

    // Track if penguin's stomach is full
    private bool isFull;

    /// <summary>
    /// Call when the agent is waken
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        penguinArea = GetComponentInParent<PenguinArea>();
        rigidbody = GetComponent<Rigidbody>();
        baby = penguinArea.babyPenguin;
    }
    /// <summary>
    /// Call when an action is received
    /// ActionBuffers parameters:
    ///     int[] discreteActions:
    ///         Index[0]: (0 = do nothing, 1 = move forward)
    ///         Index[1]: (0 = do nothing, 1 = turn left, 2 = turn right)
    /// Rewards:
    ///     -1/MaxStep for each step taken to encourage movement in agent
    /// </summary>
    /// <param name="actions">The action received</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Convert the first action to forward movement
        float forwardAmount = actions.DiscreteActions[0];

        // Convert the second action to turning left or right
        float turnAmount = actions.DiscreteActions[1];

        // Apply forces to agent
        rigidbody.AddForce(transform.forward * speed * forwardAmount * Time.deltaTime);
        rigidbody.AddTorque(transform.up * turnAmount * turnSpeed * Time.deltaTime);

        // Apply penalty for step
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }


    /// <summary>
    /// Call when the agent is heuristically controlled
    /// </summary>
    /// <param name="actionsOut">A vector action array that will be passed to <see cref="OnActionReceived"/> </param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Detect forward movement
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        else discreteActionsOut[0] = 0;

        // Detect rotation
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 2;
        else discreteActionsOut[1] = 0;
    }

    /// <summary>
    /// When a new episode starts, reset area and agent
    /// </summary>
    public override void OnEpisodeBegin()
    {
        isFull = false;
        penguinArea.ResetArea();
    }

    /// <summary>
    /// Collect all non-raycast observations
    /// </summary>
    /// <param name="sensor">The vector sensor to add observations</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Whether the agent has eaten a fish (1 observation)
        sensor.AddObservation(isFull);

        // The distance to the baby penguin (1 observation)
        sensor.AddObservation(Vector3.Distance(transform.position, baby.transform.position));

        // The direction to the baby (3 observation)
        sensor.AddObservation(Vector3.Normalize(baby.transform.position - transform.position));

        // The direction the penguin is facing (3 observation)
        sensor.AddObservation(transform.forward);

        // Total observations = 1 + 1 + 3 + 3 = 8
    }

    /// <summary>
    /// Call when the agent collide with something
    /// </summary>
    /// <param name="other">The collider of the other object</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("fish")) 
        {
            // Try to eat the fish
            EatFish(other.gameObject);
        }

        if (other.CompareTag("baby"))
        {
            // Try to feed the baby
            RegurgitateFish();
        }
    }

    /// <summary>
    /// Attempt to eat the fish if the penguin is not already full
    /// </summary>
    /// <param name="gameObject">The fish to eat</param>
    private void EatFish(GameObject gameObject)
    {
        // Is the penguin already full
        if (isFull) return;

        isFull = true;

        // Destroy the fish
        penguinArea.RemoveSpecificFish(gameObject);

        AddReward(1f);
    }

    /// <summary>
    /// Attempt to regurgitate the fish to feed the baby
    /// </summary>
    private void RegurgitateFish()
    {
        // Is the penguin's stomach empty
        if (!isFull) return;
        isFull = false;
        // Regurgitate the fish
        GameObject regurgitatedFish = Instantiate(regurgitatedFishPrefab, transform.position, Quaternion.identity, transform.parent);
        Destroy(regurgitatedFish, 4f);

        // Spawn the heart to show baby penguin is happy
        GameObject heart = Instantiate(redHeartPrefab, baby.transform.position + Vector3.up, Quaternion.identity, transform.parent);
        Destroy(heart, 4f);

        AddReward(1f);

        if(penguinArea.FishRemaining <= 0) EndEpisode();
    }
}

