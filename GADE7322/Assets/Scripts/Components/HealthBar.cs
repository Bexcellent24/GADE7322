using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;        

    [Header("Target")]
    [SerializeField] private Health targetHealth;    

    private Transform target;
    private Camera mainCam;

    private void Awake()
    {
        if (targetHealth != null)
            Initialize(targetHealth);

        mainCam = Camera.main;
    }

    public void Initialize(Health health)
    {
        targetHealth = health;
        target = health.transform;

        // Subscribe to health events
        targetHealth.OnHealthChanged += UpdateBar;

        UpdateBar();
    }

    private void LateUpdate()
    {
      /*  if (target == null || mainCam == null) return;

        
        Vector3 lookDir = mainCam.transform.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.001f) 
        {
            transform.rotation = Quaternion.LookRotation(-lookDir); 
            
        } */
    }

    private void UpdateBar()
    {
        if (fillImage != null && targetHealth != null)
            fillImage.fillAmount = (float)targetHealth.Current / targetHealth.Max;
    }
    
    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateBar;
        }
    }
}