using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flower
/// </summary>
public class FlowerArea : MonoBehaviour
{
    // The diameter of the area where the agent and flower can be
    // used for observing relative distance from agent to flower
    public const float AreaDiameter = 20f;

    // The list of all flower plants in the flower area (A flower plant consists of multiple flowers)
    private List<GameObject> flowerPlants;

    // A lookup dictionary for looking up a flower from a nectar collider
    private Dictionary<Collider, Flower> nectarFlowerDictionary;
    /// <summary>
    /// The list of all flowers in the flower area
    /// </summary>
    public List<Flower> Flowers {get; private set;}

    /// <summary>
    /// Reset the flowers and flower plants
    /// </summary>
    public void ResetFlowers()
    {
        // Rotate each flower plant and the Y axis and subtly around X and Z
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5);
            float zRotation = UnityEngine.Random.Range(-5f, 5);
            float yRotation = UnityEngine.Random.Range(-360f, 360f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation,yRotation,zRotation);
        }

        // Reset each flower
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }
    
    /// <summary>
    /// Gets a <see cref="Flower"/> that a nectar collider belongs to 
    /// </summary>
    /// <param name="collider">The nectar collider</param>
    /// <returns>The corresponiding flower</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        if(nectarFlowerDictionary.ContainsKey(collider)){
            return nectarFlowerDictionary[collider];
        }
        else{
            throw new Exception("Invalid collider, something is wrong with GetFlowerFromNectar");
        }
    }
    /// <summary>
    /// Call when area wakes up
    /// </summary>
    public void Awake()
    {
        // Initialize varaibles
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }
    /// <summary>
    /// Call when game start
    /// </summary>
    public void Start()
    {
        // Find all the flowers that are children of this GameObject/Transform
        FindChildFlowers(transform);
    }
    /// <summary>
    /// Find all child of this transform that are flowers
    /// </summary>
    /// <param name="parent"></param>
    private void FindChildFlowers(Transform parent)
    {
        //Two base cases, one is when there is no child, second is when a flower is found
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if(child.CompareTag("flower_plant"))
            {
                //Found FlowerPlant, adding to flowerPlants list
                flowerPlants.Add(child.gameObject);
                FindChildFlowers(child);
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();
                if(flower!=null)
                {
                    //Found flower, adding into flowers list
                    Flowers.Add(flower);
                    
                    //Add to dictionary as well
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);
                }
                else
                {
                    //Check deeper
                    FindChildFlowers(child);
                }
            }
        }
    }
}
