using System.Collections.Generic;
using UnityEngine;

public class BoidFlock : MonoBehaviour
{
    public static BoidFlock Instance { get; private set; }

    [Header("Planet")]
    [SerializeField] Transform planetCenter;

    [Header("Boid Weights")]
    [SerializeField] float separationWeight = 1.8f;
    [SerializeField] float alignmentWeight  = 0.9f;
    [SerializeField] float cohesionWeight   = 0.9f;
    [SerializeField] float goalWeight       = 1.2f;
    [SerializeField] float surfaceStickWeight = 3.0f; 

    [Header("Neighborhood")]
    [SerializeField] float neighborRadius = 5f;
    [SerializeField] float separationRadius = 2.2f;

    public Transform PlanetCenter => planetCenter;
    public float NeighborRadius => neighborRadius;
    public float SeparationRadius => separationRadius;

    public float SeparationW => separationWeight;
    public float AlignmentW  => alignmentWeight;
    public float CohesionW   => cohesionWeight;
    public float GoalW       => goalWeight;
    public float SurfaceW    => surfaceStickWeight;

    readonly List<BoidEnemy> agents = new();

    void Awake() => Instance = this;

    public void Register(BoidEnemy e) { if (!agents.Contains(e)) agents.Add(e); }
    public void Unregister(BoidEnemy e) { agents.Remove(e); }

    public void GetNeighbors(BoidEnemy self, List<BoidEnemy> outList)
    {
        outList.Clear();
        var r2 = NeighborRadius * NeighborRadius;
        var p = self.transform.position;
        foreach (var a in agents)
        {
            if (a == self) continue;
            if ((a.transform.position - p).sqrMagnitude <= r2) outList.Add(a);
        }
    }
}