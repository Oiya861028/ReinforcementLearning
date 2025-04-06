using UnityEngine;
using Unity.MLAgents;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
public class PenguinArea: MonoBehaviour
{
    [Tooltip("The agent inside the area")]
    public PenguinAgent penguinAgent;

    [Tooltip("The baby penguin inside the area")]
    public GameObject babyPenguin;

    [Tooltip("The TextmeshPro that shows the cumulative reward of the agent")]
    public TextMesh cumulativeRewardText;

    [Tooltip("Prefab of a live fish")]
    public Fish fishPrefab;

    /// <summary>
    /// The number of fish currently in the area
    /// </summary>
    public int FishRemaining
    {
        get 
        { 
            return fishList.Count; 
        }
    }

    // A list of all the fish in the area
    private List<GameObject> fishList;

    /// <summary>
    /// Resets the area, including fish and penguin placesment
    /// </summary>
    public void ResetArea()
    {
        RemoveAllFish();
        placePenguin();
        placeBaby();
        SpawnFish(4, .5f);
    }

    /// <summary>
    /// Remove a specific fish from the area when it is eaten
    /// </summary>
    /// <param name="fishObject">The fish to remove</param>
    public void RemoveSpecificFish(GameObject fishObject)
    {
        fishList.Remove(fishObject);
        Destroy(fishObject);
    }

    /// <summary>
    /// Choose a random position on the X-Z plane within a partial donut shape
    /// </summary>
    /// <param name="center">The center of the donut</param>
    /// <param name="minAngle">Minimum angle of the wedge</param>
    /// <param name="maxAngle">Maximum angle of the wedge</param>
    /// <param name="minRadius">Minimum radius of the wedge</param>
    /// <param name="maxRadius">Maximum radius of the wedge</param>
    /// <returns>Random Vector3 position within the X-Z plane specified</returns>
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius) 
    {
        float angle = minAngle;
        float radius = minRadius;

        if (maxAngle > minAngle)
        {
            // Pick a random angle
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        if (maxRadius > minRadius)
        {
            // Pick a random radius
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        // Center radius + forwardVector rotated around the Y axis by "angle" degrees, multiplies by "radius"
        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    private void RemoveAllFish()
    {
        if (fishList != null)
        {
            for (int i = 0; i < fishList.Count; i++) 
            {
                if (fishList[i] != null)
                {
                    Destroy(fishList[i]);
                }
            }
            fishList.Clear();
        }
        fishList = new List<GameObject>();
    }

    /// <summary>
    /// placess the penguin in the area randomly
    /// </summary>
    private void placePenguin()
    {
        // Resets the penguin's linear and angular velocity
        Rigidbody rigidbody = penguinAgent.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        
        // Randomly places the penguin in the area
        Vector3 position = ChooseRandomPosition(Vector3.zero, 0f, 360f, 0f, 9f) + Vector3.up * 0.5f; // + Vector3.up * 0.5f to make sure it doesn't clip through ground
        penguinAgent.transform.position = position;

        // Randomly rotate the penguin
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        penguinAgent.transform.rotation = rotation;
    }

    /// <summary>
    /// places the baby penguin in the area, making sure it's on land
    /// </summary>
    private void placeBaby()
    {
        // Resets the linear and angular velocity
        Rigidbody rigidbody = babyPenguin.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        
        // Randomly places the baby on land
        Vector3 position = ChooseRandomPosition(Vector3.zero, -45f, 45f, 0f, 9f) + Vector3.up * 0.5f; // + Vector3.up * 0.5f to make sure it doesn't clip through ground
        babyPenguin.transform.position = position;

        // Set rotation
        babyPenguin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    /// <summary>
    /// Spawn some fish in the area and set their swim speed
    /// </summary>
    /// <param name="count">Amount of fish to spawn</param>
    /// <param name="fishSpeed">The swim speed</param>
    private void SpawnFish(int count, float fishSpeed)
    {
        for (int i=0; i < count; i++)
        {
            // Set random position and rotation within the area
            Vector3 position = ChooseRandomPosition(Vector3.zero, 100f, 260f, 2f, 13f) + Vector3.up * 0.5f; 
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            // Spawn the fish, make it a child of the area, and keep track of the fish
            fishList[i] = Instantiate<GameObject>(fishPrefab.GameObject(), position, rotation, transform.parent);

            // Set the fish swim speed
            fishList[i].GetComponent<Fish>().fishSpeed = fishSpeed;
        }
    }

    /// <summary>
    /// Call when the game start
    /// </summary>
    private void Start()
    {
        ResetArea();
    }

    /// <summary>
    /// Call every frame
    /// </summary>
    private void Update()
    {
        // Updates the cumulative reward text
        cumulativeRewardText.text = penguinAgent.GetCumulativeReward().ToString("0.00");
    }
}
