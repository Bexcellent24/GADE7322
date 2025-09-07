using UnityEngine;

public class ParticleScript05 : MonoBehaviour
{
    [SerializeField] private Vector3 speed;
    
    void Update()
    {
        transform.Rotate(speed * Time.deltaTime);
    }
}
