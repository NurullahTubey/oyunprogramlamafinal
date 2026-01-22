using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class karakter2kontrolleri : MonoBehaviour
{
    [Header("Hareket")]
    public float hiz = 6f;
    public float ziplamaGucu = 4f;

    [Header("Başlangıç Yürüyüşü")]
    [Tooltip("Oyun başlarken karakter otomatik olarak hedef Z konumuna yürüsün mü?")]
    public bool otomatikBaslangicYuruyus = true;
    [Tooltip("Başlangıçta yürünecek hedef Z konumu (ör: 5)")]
    public float baslangicHedefZ = 5f;
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
    tilkiyumruk punchScript;
    KarakterSesleri karakterSesleri;
    bool isDead = false;
    Coroutine deathRoutine;
    bool baslangicYuruyor = false;
    
    [Header("Ateş Topu")]
    public GameObject atesTopuPrefab;
    public Transform atesTopuSpawnPoint;
    [Tooltip("Spawn noktasına Y ekseninde eklenecek yükseklik offset'i")]
    public float atesTopuYukseklikOffseti = 0.5f;
    [Tooltip("Q tuşuna basıldıktan kaç saniye sonra ateş topu oluşsun (animasyon için)")]
    public float atesTopuGecikme = 3f;
    [Tooltip("Ateş topu hedefe kilitlensin mi? False ise karakterin baktığı yöne gider.")]
    public bool atesTopuHedefeKilitsin = false;
    public Transform atesTopuHedefi;
    [Tooltip("Manuel yön: 1 = ekrana doğru (diğer karaktere), -1 = ekrandan uzağa")]
    public float atesTopuYonu = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        
        punchScript = GetComponent<tilkiyumruk>();
        if (punchScript == null)
        {
            punchScript = GetComponentInChildren<tilkiyumruk>();
            if (punchScript != null)
            {
                Debug.Log($"tilkiyumruk scripti child objede bulundu: {punchScript.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("tilkiyumruk scripti ana objede bulundu");
        }
        
        if (punchScript == null)
        {
            Debug.LogError("tilkiyumruk scripti hiçbir yerde bulunamadı!");
        }
        
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
            Debug.LogWarning("KarakterSesleri scripti bulunamadı! Yumruk sesi çalmayacak.");
        }
        
        rb.freezeRotation = true;
        
        // Health component varsa ölüm eventini dinle
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.onDied.AddListener(OnDeath);
            Debug.Log("Karakter2: Health component bulundu, ölüm event'i dinleniyor.");
        }
        else
        {
            Debug.LogWarning("Karakter2: Health component bulunamadı! Ölüm animasyonu çalışmayacak.");
        }

        if (atesTopuSpawnPoint == null)
        {
            var t = transform.Find("AtesNoktasi");
            if (t != null)
            {
                atesTopuSpawnPoint = t;
                Debug.Log($"Ateş topu spawn noktası otomatik bulundu: {t.name}");
            }
        }
        
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log("Karakter2 Animator parametreleri:");
            foreach (var param in animator.parameters)
            {
                Debug.Log($"- {param.name} ({param.type})");
            }
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
        
        Vector3 start = transform.position + Vector3.up * 0.05f;
        yerde = Physics.Raycast(start, Vector3.down, zeminMesafe, zeminKatmani);

        if (yerde && ziplamaYapiliyor)
        {
            ziplamaYapiliyor = false;
        }

        Debug.DrawRay(start, Vector3.down * zeminMesafe, yerde ? Color.green : Color.red);

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
            }
            else
            {
                float yon = Mathf.Sign(deltaZ); // 1: +Z, -1: -Z

                ileriBasili = yon > 0f;   // Bu karakterde ileri = +Z (D)
                geriBasili  = yon < 0f;   // Geri = -Z (A)

                if (animator != null)
                {
                    animator.SetBool("yuru", ileriBasili);
                    animator.SetBool("geriyuru", geriBasili);
                    animator.SetBool("yerde", yerde);
                    animator.SetBool("bosta", false);
                }

                // Başlangıç yürüyüşü bitene kadar input tabanlı saldırı & zıplama kapalı
                return;
            }
        }
        else
        {
            ileriBasili = Input.GetKey(KeyCode.D);
            geriBasili  = !ileriBasili && Input.GetKey(KeyCode.A);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (animator != null)
            {
                animator.SetTrigger("yumruk");
                Debug.Log("Yumruk animasyonu tetiklendi!");
            }
            
            if (karakterSesleri != null)
            {
                karakterSesleri.YumrukSesiCal();
                Debug.Log("Yumruk sesi çalındı!");
            }
            else
            {
                Debug.LogWarning("KarakterSesleri scripti yok, ses çalamıyor!");
            }
            
            if (punchScript != null)
            {
                Debug.Log("punchScript bulundu, 0.3 saniye sonra hasar verilecek...");
                StartCoroutine(DelayedPunchHit());
            }
            else
            {
                Debug.LogError("punchScript NULL! tilkiyumruk component bulunamadı!");
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (animator != null)
            {
                animator.SetTrigger("tekme");
                Debug.Log("Tekme animasyonu tetiklendi!");
            }
            
            if (karakterSesleri != null)
            {
                karakterSesleri.TekmeSesiCal();
                Debug.Log("Tekme sesi çalındı!");
            }
            else
            {
                Debug.LogWarning("KarakterSesleri scripti yok, tekme sesi çalamıyor!");
            }

            if (punchScript != null)
            {
                Debug.Log("Tekme için hasar verilecek...");
                StartCoroutine(DelayedKickHit());
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (animator != null)
            {
                animator.Play("fireball");
                Debug.Log("Fireball animasyonu tetiklendi!");
            }
            else
            {
                Debug.LogWarning("Animator bulunamadı!");
            }
            
            // Ateş topu sesi çal
            if (karakterSesleri != null)
            {
                karakterSesleri.AtesTopuSesiCal();
                Debug.Log("Ateş topu sesi çalındı!");
            }
            else
            {
                Debug.LogWarning("KarakterSesleri scripti yok, ateş topu sesi çalamıyor!");
            }

            // Ateş topu oluşturma işlemini geciktir
            StartCoroutine(DelayedFireball());
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log($"W basıldı! Yerde mi: {yerde}, Zıplıyor mu: {ziplamaYapiliyor}");
            
            if (yerde && !ziplamaYapiliyor)
            {
                ziplamaYapiliyor = true;
                
                if (animator != null)
                {
                    if (animator.parameters.Length > 0)
                    {
                        bool found = false;
                        foreach (var param in animator.parameters)
                        {
                            if (param.type == UnityEngine.AnimatorControllerParameterType.Trigger)
                            {
                                if (param.name == "zipla" || param.name == "ziplama" || param.name.ToLower().Contains("zip"))
                                {
                                    animator.SetTrigger(param.name);
                                    Debug.Log($"Ziplama trigger bulundu ve aktif edildi: {param.name}");
                                    found = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!found)
                        {
                            Debug.LogWarning("Ziplama trigger'ı bulunamadı! Animator'da 'zipla' veya 'ziplama' trigger'ı ekleyin.");
                        }
                    }
                }
                
                Vector3 v = rb.linearVelocity;
                v.y = ziplamaGucu;
                rb.linearVelocity = v;
            }
            else
            {
                Debug.Log("Havadayken veya zıplarken tekrar zıplanamaz!");
            }
        }

        animator.SetBool("yuru", ileriBasili);
        animator.SetBool("geriyuru", geriBasili);
        animator.SetBool("yerde", yerde);
        
        bool bosta = !ileriBasili && !geriBasili;
        animator.SetBool("bosta", bosta);
    }

    void FixedUpdate()
    {
        if (isDead) return;
        
        Vector3 v = rb.linearVelocity;
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
            if (Input.GetKey(KeyCode.D)) z = 1f;
            else if (Input.GetKey(KeyCode.A)) z = -1f;

            v.z = z * hiz;
        }

        rb.linearVelocity = v;
    }

    Vector3 HesaplaAtesTopuYonu(Vector3 spawnPos)
    {
        if (atesTopuHedefeKilitsin && atesTopuHedefi != null)
        {
            Vector3 hedefYon = atesTopuHedefi.position - spawnPos;
            if (hedefYon.sqrMagnitude > 0.001f)
            {
                hedefYon.y = 0;
                return hedefYon.normalized;
            }
        }

        return new Vector3(0, 0, atesTopuYonu).normalized;
    }
    
    System.Collections.IEnumerator DelayedPunchHit()
    {
        Debug.Log("Coroutine başladı, 0.3 saniye bekleniyor...");
        yield return new WaitForSeconds(0.3f);
        Debug.Log("Bekleme bitti, TriggerPunchHit çağrılıyor...");
        TriggerPunchHit();
    }
    
    System.Collections.IEnumerator DelayedKickHit()
    {
        Debug.Log("Tekme coroutine başladı, 0.4 saniye bekleniyor...");
        yield return new WaitForSeconds(0.4f);
        Debug.Log("Tekme vuruşu gerçekleşiyor...");
        TriggerKickHit();
    }
    
    System.Collections.IEnumerator DelayedFireball()
    {
        Debug.Log($"[FIREBALL] {atesTopuGecikme} saniye bekleniyor (animasyon)...");
        yield return new WaitForSeconds(atesTopuGecikme);
        Debug.Log("[FIREBALL] Bekleme bitti, ateş topu oluşturuluyor!");
        
        if (atesTopuPrefab != null)
        {
            Vector3 spawnPos;
            
            if (atesTopuSpawnPoint != null)
            {
                spawnPos = atesTopuSpawnPoint.position + new Vector3(0, atesTopuYukseklikOffseti, 0);
                Debug.Log($"[FIREBALL] Spawn noktası: {atesTopuSpawnPoint.name}, Y: {spawnPos.y}");
            }
            else
            {
                spawnPos = transform.position + new Vector3(0, atesTopuYukseklikOffseti, 0);
                Debug.Log($"[FIREBALL] Karakter pozisyonu kullanılıyor, Y: {spawnPos.y}");
            }

            Quaternion spawnRot = Quaternion.Euler(0f, -90f, 0f);
            GameObject ates = Instantiate(atesTopuPrefab, spawnPos, spawnRot);

            var controller = ates.GetComponent<AtesTopuController>();
            if (controller != null)
            {
                Vector3 atisYonu = HesaplaAtesTopuYonu(spawnPos);
                controller.InitializeOwner(gameObject);
                controller.Launch(atisYonu);
                Debug.Log($"[FIREBALL] Ateş topu fırlatıldı! Yön: {atisYonu}");
            }
            else
            {
                Debug.LogWarning("[FIREBALL] AtesTopuController yok!");
            }
        }
        else
        {
            Debug.LogWarning("[FIREBALL] atesTopuPrefab atanmadı!");
        }
    }
    
    void TriggerPunchHit()
    {
        Debug.Log("TriggerPunchHit çağrıldı!");
        if (punchScript != null)
        {
            Debug.Log("punchScript bulundu, OnPunchHit çağrılıyor...");
            punchScript.OnPunchHit();
        }
        else
        {
            Debug.LogError("punchScript NULL! tilkiyumruk component eksik!");
        }
    }
    
    void TriggerKickHit()
    {
        Debug.Log("TriggerKickHit çağrıldı!");
        if (punchScript != null)
        {
            Debug.Log("Tekme hasarı veriliyor...");
            punchScript.OnKickHit();
        }
    }
    
    System.Collections.IEnumerator DeathSequence()
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

        Debug.Log("Karakter2 öldü ve yere indi.");

        this.enabled = false; // İş bittikten sonra scripti kapat
    }
}