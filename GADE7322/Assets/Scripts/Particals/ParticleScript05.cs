using UnityEngine;

public class ParticleScript05 : MonoBehaviour
{
    [SerializeField] private Vector3 speed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(speed * Time.deltaTime);
    }
}
