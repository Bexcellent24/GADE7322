using UnityEngine;

public class WaterWorldManager : MonoBehaviour
{
    [Header("World")]
    [SerializeField] private Transform planetCenter;
    [SerializeField] private float waterRadius = 10f;
    [SerializeField] private Vector3 goalPoleDir = Vector3.down;

    [Header("Navigation")]
    [SerializeField] private MarchingCubesPlanet planet;

    private static WaterWorldManager _instance;

    public Transform PlanetCenter => planetCenter;
    public float WaterRadius => waterRadius;
    public Vector3 GoalPoleDir => goalPoleDir;
    public MarchingCubesPlanet Planet => planet;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        
        // Auto-find planet if not assigned
        if (!planet) planet = FindObjectOfType<MarchingCubesPlanet>();
        if (!planetCenter) planetCenter = transform; // Fallback to self
    }
    

    public static WaterWorldManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WaterWorldManager>();
                if (_instance == null)
                {
                    Debug.LogError("No WaterWorldManager instance found in scene!");
                }
            }
            return _instance;
        }
    }
}