using UnityEngine;
using Unity.MLAgents;
using TMPro;
using System.Collections.Generic;
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

    // A list of all the fish in the area
    private List<Fish> fishList;
}
