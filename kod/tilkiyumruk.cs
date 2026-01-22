using UnityEngine;

public class tilkiyumruk : MonoBehaviour
{
    [Header("Yumruk Ayarları")]
    public float punchDamage = 10f;
    public float punchRange = 2.5f;  // Menzil
    
    [Header("Tekme Ayarları")]
    public float kickDamage = 15f;
    public float kickRange = 4f;
    
    [Header("Referanslar")]
    public Transform punchPoint;  // Yumruğun çıkış noktası (el pozisyonu)
    public Health targetHealth;  // Rakibin Health componenti (Inspector'dan sürükle)
    
    [Header("Efektler")]
    public GameObject hitEffectPrefab;  // Vuruş efekti (Inspector'dan sürükle)
    
    [Header("Sesler")]
    public AudioClip punchSound;  // Yumruk sesi
    public AudioClip kickSound;   // Tekme sesi
    
    private Animator animator;
    private AudioSource audioSource;
    [Header("Karakter2 Ayarları")]
    public Animator karakter2Animator; // Inspector'dan karakter2'nin Animator'ını sürükleyin
    public string fireballStateName = "fireball"; // Animator'daki state adı
    
    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        // AudioSource yoksa ekle
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Menzil çok düşükse otomatik artır
        if (punchRange < 2.5f)
        {
            Debug.LogWarning($"Yumruk menzili çok düşük ({punchRange}), 8'e yükseltiliyor!");
            punchRange = 2.5f;
        }
        
        if (kickRange < 4f)
        {
            Debug.LogWarning($"Tekme menzili çok düşük ({kickRange}), 8'e yükseltiliyor!");
            kickRange = 5f;
        }
        
        // Punch point yoksa karakterin önünde bir nokta oluştur
        if (punchPoint == null)
        {
            GameObject punchObj = new GameObject("PunchPoint");
            punchPoint = punchObj.transform;
            punchPoint.parent = transform;
            punchPoint.localPosition = new Vector3(0, 1f, 0.5f); // Karakterin önünde
        }
    }

    void Update()
    {
        // Q tuşuna basılınca karakter2'nin fireball animasyonunu oynat
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (karakter2Animator != null)
            {
                karakter2Animator.Play(fireballStateName);
                Debug.Log("karakter2: fireball animasyonu tetiklendi.");
            }
            else
            {
                Debug.LogWarning("karakter2Animator atanmamış! Inspector'da ayarlayın.");
            }
        }
    }
    
    // Yumruk hasarı
    public void OnPunchHit()
    {
        // Sesi ÖNCE çal (hasar vermese bile)
        PlaySound(punchSound);
        // Sonra hasar kontrolü yap
        DealDamage(punchDamage, punchRange, "YUMRUK");
    }
    
    // Tekme hasarı
    public void OnKickHit()
    {
        // Sesi ÖNCE çal (hasar vermese bile)
        PlaySound(kickSound);
        // Sonra hasar kontrolü yap
        DealDamage(kickDamage, kickRange, "TEKME");
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
    
    // Genel hasar verme fonksiyonu
    void DealDamage(float damage, float range, string attackType)
    {
        Debug.Log($"========== {attackType} ÇAĞRILDI! ==========");
        Debug.Log($"Karakter: {gameObject.name}");
        Debug.Log($"Hasar: {damage}, Menzil: {range}");
        
        // Eğer targetHealth direkt atanmışsa onu kullan
        if (targetHealth != null)
        {
            // Mesafe kontrolü
            float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
            Debug.Log($"Hedef: {targetHealth.name}, Mesafe: {distance:F2}");
            
            if (distance <= range)
            {
                Debug.Log($"MENZİL İÇİNDE! Hasar veriliyor...");
                targetHealth.TakeDamage(damage);
                Debug.Log($"✅ {attackType}! {targetHealth.name} → {damage} hasar aldı! Kalan can: {targetHealth.currentHealth}");
                
                // Vuruş efekti oluştur
                SpawnHitEffect(targetHealth.transform.position);
            }
            else
            {
                Debug.LogWarning($"❌ ÇOK UZAK! Mesafe: {distance:F2}, Gerekli: {range}");
            }
        }
        else
        {
            Debug.LogWarning("TargetHealth atanmamış! Etrafta arama yapılıyor...");
            // targetHealth atanmamışsa etraftaki tüm Health componentlerini ara
            Collider[] hitColliders = Physics.OverlapSphere(punchPoint.position, range);
            Debug.Log($"Etrafta {hitColliders.Length} obje bulundu");
            
            foreach (Collider hitCollider in hitColliders)
            {
                Debug.Log($"Bulunan obje: {hitCollider.name}");
                
                // Kendine vurmayı engelle
                if (hitCollider.gameObject == gameObject || hitCollider.transform.IsChildOf(transform))
                {
                    Debug.Log($"  -> Kendisi, atlandı");
                    continue;
                }
                    
                // Health componenti var mı kontrol et
                Health health = hitCollider.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Debug.Log($"✅ {attackType}! {hitCollider.name} → {damage} hasar aldı!");
                    
                    // Vuruş efekti oluştur
                    SpawnHitEffect(hitCollider.transform.position);
                }
                else
                {
                    Debug.Log($"  -> Health component yok");
                }
            }
        }
    }
    
    // Vuruş efekti oluştur
    void SpawnHitEffect(Vector3 targetPosition)
    {
        if (hitEffectPrefab != null)
        {
            // İki karakter arasındaki orta noktada efekt çıkar
            Vector3 attackerPos = transform.position;
            Vector3 midPoint = (attackerPos + targetPosition) / 2f;
            
            // Göğüs hizasında (Y ekseninde ayarla)
            Vector3 effectPosition = new Vector3(midPoint.x, targetPosition.y + 1f, midPoint.z);
            
            // Efekti oluştur ve saldırganın yönüne döndür
            Vector3 direction = (targetPosition - attackerPos).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            GameObject effect = Instantiate(hitEffectPrefab, effectPosition, rotation);
            
            // Efekti 2 saniye sonra yok et
            Destroy(effect, 2f);
            
            Debug.Log($"💥 Vuruş efekti oluşturuldu: {effectPosition}");
        }
        else
        {
            Debug.LogWarning("Hit Effect Prefab atanmamış!");
        }
    }
    
    // Debug için yumruk menzilini göster
    void OnDrawGizmosSelected()
    {
        if (punchPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(punchPoint.position, punchRange);
        }
        else if (transform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + new Vector3(0, 1f, 0.5f), punchRange);
        }
    }
}
