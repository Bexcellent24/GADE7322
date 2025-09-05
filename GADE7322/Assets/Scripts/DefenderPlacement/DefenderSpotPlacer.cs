using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class DefenderSpotPlacer : MonoBehaviour
{
    [Header("Planet / Water")]
    public Transform planetCenter;
    [Tooltip("Don't touch - determines what is 'above water'")]
    public float waterRadius = 10f;
    [Tooltip("True = shoreline placements, False = peak placements")]
    public bool requireAboveWater = false;

    [Header("Land")]
    [Tooltip("Scans vertexes for highest points")]
    public MeshFilter landMeshFilter;
    [Tooltip("Backup reference if mesh above isn't assigned^^^")]
    public MarchingCubesPlanet planet;

    [Header("Sampling / Selection")]
    [Tooltip("Minimum height above water when RequireAboveWater = true.")]
    public float minAltitude = 0.25f;
    [Tooltip("Angular separation of the tower spots, higher = more spread across the planet")]
    [Range(2f, 45f)] public float clusterArcDeg = 12f;
    [Tooltip("Number of tower spots to spawn")]
    public int maxSpots = 8;
    [Tooltip("Lower = slower but more accurate peak detection, Higher = faster but less accurate peak detection")]
    public int vertexStep = 4;

    [Header("Spot Appearance")]
    [Tooltip("Assign the VFX / Prefab to indicate where towers can be placed")]
    public GameObject spotPrefab;
    
    [Tooltip("If nothing above is assigned ^^ assigns a default ring")]
    public float defaultRingRadius = 0.5f;
    [Tooltip("Default rings thickness")]
    public float defaultRingThickness = 0.06f;
    [Tooltip("How high the indicator is above the ground")]
    public float lift = 0.05f;

    [Header("Debug")]
    [Tooltip("Keep true to log meta key data in the console")]
    public bool verboseLogs = true;

    [Header("Flattening")] [Tooltip("Radius around each spot to flatten")]
    public float flattenRadius = .5f;
    [Tooltip("Strength of flattening (1 = fully flat, <1 = softer)")]
    [Range(0f, 1f)] public float flattenStrength = 1f;

    //List containing all the tower spots available during runtime
    [HideInInspector] public List<DefenderSpot> spots = new();
    
    //Checks if planetCenter is assigned else defaults it to 0,0,0  
    Vector3 Center => planetCenter ? planetCenter.position : Vector3.zero;

    //Helper struct that grades potential spots based on their altitude above water  
    struct Candidate { public Vector3 pos; public float score; }
    
    //Right click script header to re-run method - pretty neat
    [ContextMenu("Regenerate Spots")]
    public void GenerateSpots()
    {
        //Clears old spots and the list to make room for the new candidates
        foreach (var s in spots) if (s) DestroyImmediate(s.gameObject);
        spots.Clear();

        //Assign center locally
        var center = Center;
        
        //Create candidate list and assign capacity
        var candidates = new List<Candidate>(1024);
        
        //Candidate counter
        int spotCounter = 0;

        //Mesh placeholder 
        Mesh mesh = null;
        
        //Local-to-world transform matrix that acts as a placeholder to avoid nulls
        Matrix4x4 l2w = Matrix4x4.identity;

        //Validates if there is a landmesh provided 
        if (landMeshFilter && landMeshFilter.sharedMesh)
        {
            //Stores mesh and mesh matrix to be converted into world space
            mesh = landMeshFilter.sharedMesh;
            l2w = landMeshFilter.transform.localToWorldMatrix;
            
            //Informs us that mesh has been assigned
            if (verboseLogs) 
                Debug.Log("[DefenderSpotPlacer] Using Land MeshFilter directly.");
        }
        
        //If no mesh is provided - auto assigns the marching cubes planet
        else
        {
            //Grabs the planet manually - not ideal though
            if (!planet) 
                planet = FindObjectOfType<MarchingCubesPlanet>();
            
            if (planet)
            {
                //Then gets the mesh filter on planet
                var mf = planet.GetComponent<MeshFilter>();
                
                if (mf && mf.sharedMesh)
                {
                    mesh = mf.sharedMesh;
                    l2w = mf.transform.localToWorldMatrix;
                    if (verboseLogs) 
                        Debug.Log("[DefenderSpotPlacer] Using MarchingCubesPlanet MeshFilter.");
                }
            }
        }

        if (mesh)
        {
            //Populates the spots with the passed data provided
            spotCounter = CollectCandidates_FromMeshVertices(center, mesh, l2w, candidates);
            
            //Useful meta data
            if (verboseLogs)
            {
                if (requireAboveWater)
                    Debug.Log($"[DefenderSpotPlacer] Vertex pass: {spotCounter} candidates (alt ≥ {minAltitude}).");
                else
                    Debug.Log($"[DefenderSpotPlacer] Vertex pass: {spotCounter} candidates (top by radius).");
            }
        }
        
        else
        {
            if (verboseLogs) 
                Debug.LogWarning("[DefenderSpotPlacer] No mesh found.");
        }

        if (spotCounter == 0)
        {
            //Logs if there were no spots found, if this pops up try decreasing minAltitude
            if (verboseLogs) 
                Debug.LogWarning("[DefenderSpotPlacer] 0 candidates.");
            return;
        }

        //Sorts the scores and ranks spots (In case there are more candidates than there are maxSpots)
        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        //Spreads out spot candidates using dot product threshold to reject candidates within theclusterArcDeg value
        float cosMin = Mathf.Cos(clusterArcDeg * Mathf.Deg2Rad);
        var chosen = new List<Candidate>(maxSpots);
        foreach (var c in candidates)
        {
            Vector3 cdir = (c.pos - center).normalized;
            bool tooClose = false;
            foreach (var k in chosen)
            {
                float dot = Vector3.Dot(cdir, (k.pos - center).normalized);
                if (dot > cosMin) { tooClose = true; break; }
            }
            if (!tooClose)
            {
                chosen.Add(c);
                if (chosen.Count >= maxSpots) break;
            }
        }

        
        foreach (var c in chosen)
        {
            //Prepares the chosen spots
            Vector3 up = (c.pos - center).normalized;
            Vector3 place = c.pos + up * lift;
            Vector3 fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
            
            if (fwd.sqrMagnitude < 1e-4f) 
                fwd = Vector3.Cross(up, Vector3.right);
            
            Quaternion rot = Quaternion.LookRotation(fwd.normalized, up);
            FlattenTerrainAroundSpot(place, up, flattenRadius, flattenStrength);
            GameObject go;
            if (spotPrefab) 
                go = Instantiate(spotPrefab, place, rot, transform);
            
            //Placeholder rings if VFX / Prefab is not assigned
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "TowerSpot (Auto)";
                go.transform.SetParent(transform, true);
                go.transform.SetPositionAndRotation(place, rot);
                go.transform.localScale = new Vector3(defaultRingRadius, defaultRingThickness, defaultRingRadius);
                
                var mr = go.GetComponent<MeshRenderer>();
                
                if (mr) 
                    //Sets the spot ring colour
                    mr.sharedMaterial.color = new Color(0.2f, 1f, 0.4f, 0.85f);
                
                //Gets the GO's collider and automatically sets isTrigger to false
                var col = go.GetComponent<Collider>(); 
                
                //Automatically sets GO's isTrigger to false
                if (col) 
                    col.isTrigger = false;
            }

            //Looks for the GO's TowerSpotMarker component
            var spot = go.GetComponent<DefenderSpot>();
            
            //Adds the script if not present
            if (!spot) 
                spot = go.AddComponent<DefenderSpot>();
            
            //Calculates the radial distance
            float altitude = (c.pos - center).magnitude - waterRadius;
            
            //Stores the spot
            spots.Add(spot);
        }

        if (verboseLogs) 
            Debug.Log($"[DefenderSpotPlacer] Spawned {spots.Count} tower spots.");
    }

    int CollectCandidates_FromMeshVertices(Vector3 center, Mesh mesh, Matrix4x4 l2w, List<Candidate> outList)
    {
        //Local counter for candidates pushed 
        int added = 0;
        
        //Local space vertex positions
        var verts = mesh.vertices;
        
        //Sub-sampling applies the vertexStep logic mentioned at the top^^^^^
        int step = Mathf.Max(1, vertexStep);
        
        //Cache bool - essentially just more optimal than reading the global
        //requireAboveField bool field at the top^^^^^ every iteration
        bool gateByWater = requireAboveWater;

        for (int i = 0; i < verts.Length; i += step)
        {
            // Convert local vertex to world space using the l2w matrix 
            Vector3 wp = l2w.MultiplyPoint3x4(verts[i]);
            
            // Radial distance from planet center to this vertex in world space
            float r = (wp - center).magnitude;

            if (gateByWater)
            {
                //Calculates altitude
                float alt = r - waterRadius;
                
                //Checks if candidate passes the minAltitude requirement
                if (alt >= minAltitude)
                {
                    //Score  = altitude
                    outList.Add(new Candidate { pos = wp, score = alt }); 
                    added++;
                }
            }
            
            else
            {
                //The same as above but without the requireAboveWater restriction
                outList.Add(new Candidate { pos = wp, score = r });
                added++;
            }
        }
        //Returns all candidates the passed the 'inspection' 
        return added;
    }
    
    void FlattenTerrainAroundSpot(Vector3 center, Vector3 up, float radius, float strength)
    {
        if (!landMeshFilter || !landMeshFilter.sharedMesh)
        {
            Debug.Log("Failed to flatten terrain around spot!");
            return;
        }

        Mesh mesh = landMeshFilter.mesh; // clone of sharedMesh
        Vector3[] verts = mesh.vertices;

        // Transform vertices to world
        Matrix4x4 l2w = landMeshFilter.transform.localToWorldMatrix;
        Matrix4x4 w2l = landMeshFilter.transform.worldToLocalMatrix;

        // Flatten plane = passes through center, oriented by up
        Plane plane = new Plane(up, center);

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 wp = l2w.MultiplyPoint3x4(verts[i]);
            float dist = Vector3.Distance(wp, center);

            if (dist < radius)
            {
                // Project to plane
                plane.Raycast(new Ray(wp, -up), out float enter);
                Vector3 flatPos = wp + -up * enter;

                // Blend between original and flat position
                wp = Vector3.Lerp(wp, flatPos, strength);

                verts[i] = w2l.MultiplyPoint3x4(wp);
            }
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var collider = landMeshFilter.GetComponent<MeshCollider>();
        if (collider) collider.sharedMesh = null; // force update
        if (collider) collider.sharedMesh = mesh;
    }
    
    public IEnumerator GenerateCoroutine()
    {
        GenerateSpots();
        yield return null;
    }

}
