using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullNectarColor = new Color(1.0f, 0f, 0.3f);
    [Tooltip("The color when the flower is empty")]
    public Color emptyNectarColor = new Color(0.5f, 0f, 1.0f);
    
    /// <summary>
    /// The trigger collider for nectars
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;

    // The solid collider for representing the flower petals
    private Collider FlowerCollider;

    // The flower's material
    private Material FlowerMaterial;

    /// <summary>
    /// A vectar pointing straight out of the flower
    /// </summary>
    public Vector3 FlowerUpVectar {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the flower
    /// </summary>
    public Vector3 FlowerCenterPosition {
        get
        {
        return nectarCollider.transform.position;
        }
    }
    /// <summary>
    /// The amount of nectar remaining
    /// </summary>
    public float NectarAmount{get; private set;}
    /// <summary>
    /// Whether the flower has any nectar remaining
    /// </summary>
    public bool HasNectar {
        get 
        {
            return NectarAmount > 0f;
        }
    }

    /// <summary>
    /// Attempts to remove nectar from the flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove</param>
    /// <returns>The actual amount successfully removed</returns>
    public float Feed(float amount) 
    {
        // Tracks how much nectar was successfully taken (cannot take more than is available)
        float nectarTaken = Math.Clamp(amount, 0f, NectarAmount);
        
        // Subtract the nectar
        NectarAmount -= nectarTaken;

        if(NectarAmount <= 0) 
        {
            // There is no nectar remaining
            NectarAmount = 0;

            // Disable the flower and nectar collider
            FlowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            // Change the flower color to show it's empty
            FlowerMaterial.SetColor("_BaseColor", emptyNectarColor);
        }
        return nectarTaken;
    }
    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        // Refill
        NectarAmount = 1f;
        // Enable Colliders
        FlowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);
        // Set flower color to show it's full
        FlowerMaterial.SetColor("_BaseColor", fullNectarColor);
    }
    /// <summary>
    /// Call when the flower wakes up
    /// </summary>
    public void Awake()
    {
        // Find the mesh renderer and get it's main material
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        FlowerMaterial = meshRenderer.material;

        // Find flower and nectar collider
        FlowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
