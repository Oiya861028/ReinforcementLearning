using System;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [Tooltip("The swim speed of the fish")]
    public float fishSpeed;

    private float randomizedSpeed = 0f;
    private float nextActionTime = -1f;
    public Vector3 targetPosition;

    /// <summary>
    /// Call every 0.02 second
    /// </summary>
    private void FixedUpdate()
    {
        if(fishSpeed > 0f)
        {
            Swim();
        }
    }

    private void Swim()
    {
        if (Time.time > nextActionTime)
        {
            // Pick a new randomized speed
            randomizedSpeed = UnityEngine.Random.Range(0.5f, 1.5f) * fishSpeed;

            // Pick a new target position
            targetPosition = PenguinArea.ChooseRandomPosition(transform.position, 100f, 260f, 2f, 13f);

            // Rotate toward the target
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);

            // Calculate the time to get there 
            nextActionTime = Time.time + Vector3.Distance(transform.position, targetPosition) / randomizedSpeed;
        }
        else // Move the fish toward the current target position
        {
            // Make sure the fish doesn't swim past the target
            Vector3 moveVector = randomizedSpeed * transform.forward * Time.fixedDeltaTime;
            if (moveVector.sqrMagnitude <= (transform.position - targetPosition).sqrMagnitude)
            {
                transform.position += moveVector;
            }
            else 
            {
                transform.position = targetPosition;
                nextActionTime += Time.fixedTime;
            }
        }
    }
}