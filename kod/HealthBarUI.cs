using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Assign the RedFill Image here")]
    public Image healthFill;
    
    [Header("Assign the Health Component here")]
    public Health targetHealth;

    void Start()
    {
        Debug.Log($"HealthBarUI başlatıldı. healthFill: {healthFill != null}, targetHealth: {targetHealth != null}");
        
        if (targetHealth != null)
        {
            // Subscribe to health changes
            targetHealth.onHealthChanged.AddListener(UpdateBar);
            Debug.Log($"Health event'ine abone olundu: {targetHealth.name}");
            
            // Initialize bar
            UpdateBar(targetHealth.currentHealth, targetHealth.maxHealth);
        }
        else
        {
            Debug.LogError("HealthBarUI: targetHealth atanmamış! Inspector'dan Health componentini sürükleyin.");
        }
        
        if (healthFill == null)
        {
            Debug.LogError("HealthBarUI: healthFill atanmamış! Inspector'dan Fill Image'ı sürükleyin.");
        }
    }

    void UpdateBar(float currentHealth, float maxHealth)
    {
        if (healthFill != null)
        {
            float newFill = currentHealth / maxHealth;
            healthFill.fillAmount = newFill;
            Debug.Log($"✅ Health Bar GÜNCELLENDİ: {currentHealth}/{maxHealth} = {newFill} (fillAmount: {healthFill.fillAmount})");
        }
        else
        {
            Debug.LogError("healthFill NULL! Güncelleme yapılamadı!");
        }
    }
}
