using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Ses çalabilmek için AudioSource ekler
public class karakter1yumruk : MonoBehaviour
{
    [Header("Yumruk Ayarları")]
    public float punchDamage = 10f;
    public float punchRange = 2.5f;
    public AudioClip punchSound; // Yumruk isabet sesi

    [Header("Tekme Ayarları")]
    public float kickDamage = 15f; // İstenen hasar değeri
    public float kickRange = 3.0f; // Tekme genelde yumruktan biraz daha uzundur
    public AudioClip kickSound;  // Tekme isabet sesi

    [Header("Özel Saldırı Ayarları")]
    public float specialDamage = 30f;
    public float specialRange = 4f;          // Yakın çevre özel saldırı menzili (gerekirse)
    public float specialImpactRadius = 1f;   // Yıldırım çarpma yarıçapı (daha dar tutulur)
    
    [Header("Referanslar")]
    public Transform attackPoint;   // Vuruşun çıkış noktası (Eski punchPoint)
    public Health targetHealth;     // Hedef (Varsa)
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Ses kaynağını al
        audioSource = GetComponent<AudioSource>();
        
        // Vuruş noktası yoksa oluştur
        if (attackPoint == null)
        {
            GameObject punchObj = new GameObject("AttackPoint");
            attackPoint = punchObj.transform;
            attackPoint.parent = transform;
            attackPoint.localPosition = new Vector3(0, 1f, 0.5f); 
        }
    }
    
    // --- DIŞARIDAN ÇAĞRILACAK METOTLAR ---

    // Yumruk atıldığında bu çağrılacak
    public void OnPunchHit()
    {
        // Sesi ÖNCE çal (hasar vermese bile)
        PlaySound(punchSound);
        // Sonra hasar kontrolü yap
        DealDamage(punchDamage, punchRange, "YUMRUK");
    }

    // Tekme atıldığında bu çağrılacak
    public void OnKickHit()
    {
        // Sesi ÖNCE çal (hasar vermese bile)
        PlaySound(kickSound);
        // Sonra hasar kontrolü yap
        DealDamage(kickDamage, kickRange, "TEKME");
    }

    public void OnSpecialHit()
    {
        DealDamage(specialDamage, specialRange, "OZEL");
    }
    
    // Yıldırım gibi uzaktan gelen özel vuruşlar için
    public void OnSpecialHitAtPosition(Vector3 center)
    {
        float radius = Mathf.Max(0.1f, specialImpactRadius);
        DealDamageAtPosition(specialDamage, radius, "OZEL", center);
    }
    
    // --- ORTAK HASAR FONKSİYONU ---
    void DealDamage(float damage, float range, string attackType)
    {
        // 1. Hedef önceden belliyse (TargetHealth atalıysa)
        if (targetHealth != null)
        {
            float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
            
            if (distance <= range)
            {
                ApplyDamage(targetHealth, damage, attackType);
            }
            else
            {
                Debug.LogWarning($"❌ {attackType} boşa gitti! Mesafe: {distance:F2}");
            }
        }
        // 2. Hedef belli değilse (Alan taraması yap)
        else
        {
            Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, range);
            bool vurusBasarili = false;

            foreach (Collider hitCollider in hitColliders)
            {
                // Kendine vurmayı engelle
                if (hitCollider.gameObject == gameObject || hitCollider.transform.IsChildOf(transform))
                    continue;

                Health health = hitCollider.GetComponent<Health>();
                if (health != null)
                {
                    ApplyDamage(health, damage, attackType);
                    vurusBasarili = true;
                }
            }

            if (!vurusBasarili) Debug.Log($"{attackType} ile vurulacak kimse bulunamadı.");
        }
    }

    // Belirli bir dünya pozisyonu etrafında (ör: yıldırım düşme noktası) hasar uygular
    void DealDamageAtPosition(float damage, float range, string attackType, Vector3 center)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, range);
        bool vurusBasarili = false;

        foreach (Collider hitCollider in hitColliders)
        {
            // Kendine vurmayı engelle
            if (hitCollider.gameObject == gameObject || hitCollider.transform.IsChildOf(transform))
                continue;

            Health health = hitCollider.GetComponent<Health>();
            if (health != null)
            {
                ApplyDamage(health, damage, attackType);
                vurusBasarili = true;
            }
        }

        if (!vurusBasarili)
        {
            Debug.Log($"{attackType} (uzak) ile vurulacak kimse bulunamadı.");
        }
    }

    // Hasarı uygulayan yardımcı fonksiyon
    void ApplyDamage(Health health, float damage, string attackType)
    {
        // Hasar ver
        health.TakeDamage(damage);
        Debug.Log($"✅ {attackType} İSABET ETTİ! Hasar: {damage} | Hedef: {health.name}");
    }
    
    // Ses çalan yardımcı fonksiyon
    void PlaySound(AudioClip sound)
    {
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }
        else if (sound == null)
        {
            Debug.LogWarning("Ses dosyası atanmamış!");
        }
    }
    
    // Menzili sahnede çiz
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, punchRange); // Yumruk menzili
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, kickRange);  // Tekme menzili
        }
    }
}