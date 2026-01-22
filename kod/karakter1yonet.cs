using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class karakter1yonet : MonoBehaviour
{
    [Header("Hareket")]
    public float hiz = 6f;
    public float ziplamaGucu = 4f;

    [Header("Başlangıç Yürüyüşü")]
    [Tooltip("Oyun başlarken karakter otomatik olarak hedef Z konumuna yürüsün mü?")]
    public bool otomatikBaslangicYuruyus = true;
    [Tooltip("Başlangıçta yürünecek hedef Z konumu (ör: -5)")]
    public float baslangicHedefZ = -5f;
    [Tooltip("Başlangıç yürüyüşü hız çarpanı (1 = normal hız)")]
    public float baslangicHizCarpani = 1f;

    [Header("Zemin Kontrolü")]
    public LayerMask zeminKatmani;
    public float zeminMesafe = 0.15f;

    [Header("Ölüm Ayarları")]
    public float deathAnimationDuration = 1.5f; // Ölüm animasyonu süresi
    public float deathSinkDistance = 2f;        // Animasyon sonrası aşağı inecek mesafe
    public float deathSinkDuration = 0.5f;      // İniş süresi

    Rigidbody rb;
    Animator animator;
    bool yerde;
    bool ziplamaYapiliyor = false;
    karakter1yumruk punchScript;
    KarakterSesleri karakterSesleri;
    bool isDead = false;
    Coroutine deathRoutine;
    Coroutine lightningRoutine;
    bool baslangicYuruyor = false;

    [Header("Özel Saldırı Sesi")]
    public AudioClip yildirimSound;
    public AudioSource yildirimAudioSource;
    public float specialDamageDelay = 5f;

    [Header("Mangal Fırlatma")]
    public float mangalDetectionRange = 3f; // Mangalı algılama mesafesi
    public float mangalThrowDelay = 0.8f; // Fırlatma animasyonundan sonra gecikme süresi (saniye)
    private ThrowableMangal nearbyMangal = null; // Yakındaki mangal

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true; 

        // Yumruk scriptini bul
        punchScript = GetComponent<karakter1yumruk>();
        if (punchScript == null)
        {
            punchScript = GetComponentInChildren<karakter1yumruk>();
        }
        
        // KarakterSesleri scriptini bul
        karakterSesleri = GetComponent<KarakterSesleri>();
        if (karakterSesleri == null)
        {
            karakterSesleri = GetComponentInChildren<KarakterSesleri>();
            if (karakterSesleri != null)
            {
                Debug.Log($"KarakterSesleri scripti child objede bulundu: {karakterSesleri.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("KarakterSesleri scripti ana objede bulundu");
        }
        
        if (karakterSesleri == null)
        {
            Debug.LogWarning("KarakterSesleri scripti bulunamadı! Sesler çalmayacak.");
        }
        
        // Health component varsa ölüm eventini dinle
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.onDied.AddListener(OnDeath);
        }

        if (yildirimAudioSource == null)
        {
            yildirimAudioSource = GetComponent<AudioSource>();
        }

        // Oyun başında otomatik yürüyüşü başlat
        if (otomatikBaslangicYuruyus)
        {
            baslangicYuruyor = true;
        }
    }
    
    // Karakter öldüğünde çağrılacak fonksiyon
    void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeathSequence());
    }

    void Update()
    {
        // Ölüyse hiçbir şey yapma
        if (isDead) return;
        
        // --- ZEMİN KONTROLÜ ---
        Vector3 start = transform.position + Vector3.up * 0.05f;
        yerde = Physics.Raycast(start, Vector3.down, zeminMesafe, zeminKatmani);

        if (yerde && ziplamaYapiliyor)
        {
            ziplamaYapiliyor = false;
        }

        // --- HAREKET GİRİŞLERİ / OTOMATİK YÜRÜYÜŞ ---
        bool ileriBasili;
        bool geriBasili;

        // Başlangıç otomatik yürüyüşü aktifse
        if (baslangicYuruyor)
        {
            float deltaZ = baslangicHedefZ - transform.position.z;

            // Hedefe ulaştıysa durdur
            if (Mathf.Abs(deltaZ) <= 0.05f)
            {
                baslangicYuruyor = false;

                ileriBasili = false;
                geriBasili = false;

                if (animator != null)
                {
                    animator.SetBool("yuru", false);
                    animator.SetBool("geriyuru", false);
                    animator.SetBool("bosta", true);
                }

                // Bu frame'de saldırı/zıplama input'una devam edebilmek için buradan çıkma
            }
            else
            {
                float yon = Mathf.Sign(deltaZ); // 1: +Z, -1: -Z

                ileriBasili = yon < 0f;   // Bu karakterde ileri = -Z (LeftArrow)
                geriBasili  = yon > 0f;   // Geri = +Z (RightArrow)

                if (animator != null)
                {
                    animator.SetBool("yuru", ileriBasili);
                    animator.SetBool("geriyuru", geriBasili);
                    animator.SetBool("yerde", yerde);
                    animator.SetBool("bosta", false);
                }

                // Diğer input'ları (yumruk, tekme, zıplama) başlangıç yürüyüşü bitene kadar kapat
                return;
            }
        }
        else
        {
            ileriBasili = Input.GetKey(KeyCode.LeftArrow);  
            geriBasili  = !ileriBasili && Input.GetKey(KeyCode.RightArrow); 
        }

        // --- SALDIRI KONTROLLERİ ---

        // 1. YUMRUK (Ş Tuşu - Unity'de Semicolon)
        if (Input.GetKeyDown(KeyCode.Semicolon))
        {
            if (animator != null) animator.SetTrigger("yumruk");
            
            // Yumruk sesi çal
            if (karakterSesleri != null)
            {
                karakterSesleri.YumrukSesiCal();
                Debug.Log("Yumruk sesi çalındı!");
            }
            
            // DÜZELTİLEN KISIM BURASI: false gönderiyoruz (Yani tekme değil, yumruk)
            if (punchScript != null) StartCoroutine(DelayedAttackHit(0.3f, false));
        }

        // 2. TEKME (I Tuşu)
        else if (Input.GetKeyDown(KeyCode.I))
        {
            if (animator != null) animator.SetTrigger("tekme"); 
            
            // Tekme sesi çal
            if (karakterSesleri != null)
            {
                karakterSesleri.TekmeSesiCal();
                Debug.Log("Tekme sesi çalındı!");
            }

            // DÜZELTİLEN KISIM BURASI: true gönderiyoruz (Yani bu bir tekme)
            if (punchScript != null) StartCoroutine(DelayedAttackHit(0.4f, true)); 
        }

        // 3. ÖZEL YILDIRIM SALDIRISI (K Tuşu)
        if (Input.GetKeyDown(KeyCode.K))
        {
            TriggerLightningAttack();
        }

        // 4. MANGAL FIRLATMA (J Tuşu)
        if (Input.GetKeyDown(KeyCode.J))
        {
            ThrowNearbyMangal();
        }

        // --- ZIPLAMA ---
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (yerde && !ziplamaYapiliyor)
            {
                ziplamaYapiliyor = true;
                if (animator != null) animator.SetTrigger("ziplama");
                
                // Unity sürümüne göre velocity ayarı
                Vector3 v = rb.linearVelocity; // Eğer hata verirse burayı rb.velocity yap
                v.y = ziplamaGucu;
                rb.linearVelocity = v;         // Eğer hata verirse burayı rb.velocity yap
            }
        }

        // --- ANIMATOR GÜNCELLEME ---
        if (animator != null)
        {
            animator.SetBool("yuru", ileriBasili);
            animator.SetBool("geriyuru", geriBasili);
            animator.SetBool("yerde", yerde);
            animator.SetBool("bosta", !ileriBasili && !geriBasili);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Vector3 v = rb.linearVelocity; // Eğer hata verirse rb.velocity yap
        v.x = 0f;

        // Başlangıç otomatik yürüyüşü varsa, input yerine hedefe doğru hareket et
        if (baslangicYuruyor)
        {
            float deltaZ = baslangicHedefZ - transform.position.z;

            if (Mathf.Abs(deltaZ) <= 0.05f)
            {
                v.z = 0f;
                baslangicYuruyor = false;
            }
            else
            {
                float yon = Mathf.Sign(deltaZ); // 1: +Z, -1: -Z
                v.z = yon * hiz * baslangicHizCarpani;
            }
        }
        else
        {
            float z = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) z = -1f; 
            else if (Input.GetKey(KeyCode.RightArrow)) z = 1f; 

            v.z = z * hiz;
        }

        rb.linearVelocity = v;         // Eğer hata verirse rb.velocity yap
    }

    void ThrowNearbyMangal()
    {
        // Yakındaki tüm objeleri kontrol et
        Collider[] colliders = Physics.OverlapSphere(transform.position, mangalDetectionRange);
        
        ThrowableMangal closestMangal = null;
        float closestDistance = mangalDetectionRange;

        foreach (Collider col in colliders)
        {
            ThrowableMangal mangal = col.GetComponent<ThrowableMangal>();
            if (mangal != null && mangal.CanBeThrown())
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestMangal = mangal;
                }
            }
        }

        if (closestMangal != null)
        {
            // Fırlatma animasyonunu tetikle (varsa)
            if (animator != null)
            {
                animator.SetTrigger("firlat");
            }

            // Kısa bir gecikme ile mangalı fırlat
            StartCoroutine(DelayedThrow(closestMangal, mangalThrowDelay));
            
            Debug.Log("Mangal fırlatma animasyonu başlatıldı!");
        }
        else
        {
            Debug.Log("Yakında fırlatılabilecek mangal bulunamadı!");
        }
    }

    IEnumerator DelayedThrow(ThrowableMangal mangal, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (mangal != null)
        {
            // Karakterin baktığı yöne fırlat
            Vector3 throwDirection = transform.forward;
            
            // Eğer karakter belirli bir yöne hareket ediyorsa o yönde fırlat
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                throwDirection = -transform.forward; // -Z yönü
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                throwDirection = transform.forward; // +Z yönü
            }

            mangal.ThrowMangal(throwDirection);
        }
    }

    void TriggerLightningAttack()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator bulunamadı! Yıldırım animasyonu oynatılamıyor.");
            return;
        }

        PlayLightningSound();

        animator.ResetTrigger("yildirim");
        animator.SetTrigger("yildirim");

        Debug.Log("Karakter1: Yıldırım saldırısı tetiklendi.");
    }

    void PlayLightningSound()
    {
        if (yildirimAudioSource == null)
        {
            Debug.LogWarning("Yıldırım sesini çalacak AudioSource atanmamış.");
            return;
        }

        if (yildirimSound == null)
        {
            Debug.LogWarning("Yıldırım ses klibi atanmamış.");
            return;
        }

        yildirimAudioSource.PlayOneShot(yildirimSound);
    }
    
    // --- HASAR GECİKMESİ VE TÜR SEÇİMİ ---
    IEnumerator DelayedAttackHit(float delay, bool isKick)
    {
        yield return new WaitForSeconds(delay); // Vuruş anını bekle
        
        if (punchScript != null)
        {
            if (isKick)
            {
                // Eğer tekme ise Tekme hasarını çalıştır (15 Hasar)
                punchScript.OnKickHit();
            }
            else
            {
                // Eğer yumruk ise Yumruk hasarını çalıştır (10 Hasar)
                punchScript.OnPunchHit();
            }
        }
    }

    IEnumerator DeathSequence()
    {
        if (animator != null)
        {
            animator.ResetTrigger("olum");
            animator.Play("olum", 0, 0f); // Geçiş beklemeden ölüm animasyonuna atla
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Animasyon süresince fizik devre dışı
        }

        yield return new WaitForSeconds(deathAnimationDuration);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.down * deathSinkDistance;
        float elapsed = 0f;

        while (elapsed < deathSinkDuration)
        {
            float t = elapsed / deathSinkDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        Debug.Log("Karakter1 öldü ve yere indi.");

        this.enabled = false; // İş bittikten sonra scripti kapat
    }
}