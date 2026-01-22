using UnityEngine;

public class AtesTopuController : MonoBehaviour
{
    public float hiz = 10f;
    public float yokOlmaSuresi = 3f;
    public int hasarMiktari = 30;
    public bool kamerayaBak = true; // Ateş topu her zaman aynı boyutta görünsün mü?
    private Transform goruntuKok; // Çok parçalı prefabların sadece görsel kısmını döndür
    [SerializeField] private float billboardYawOffset = -90f; // Prefabın önünü kameraya döndürmek için ofset
    
    private Rigidbody rb3d;
    private Collider triggerCollider;
    private Vector3 hizYon = Vector3.zero;
    private bool hizAyarlandi;
    private Camera mainCamera;
    private GameObject sahibi;
    private Collider[] sahibinColliderlari;

    void Awake()
    {
        rb3d = GetComponent<Rigidbody>();
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            if (triggerCollider is SphereCollider sphere)
            {
                sphere.radius = 0.35f;
            }
            Debug.LogWarning("[AtesTopuController] Collider yoktu, otomatik SphereCollider eklendi.");
        }
        triggerCollider.isTrigger = true;
        mainCamera = Camera.main;
        EnsureGoruntuKok();
        
        // UYARI: Eğer Rigidbody yoksa otomatik ekle
        if (rb3d == null)
        {
            Debug.LogWarning("Ateş topunda Rigidbody yok! Otomatik ekleniyor...");
            rb3d = gameObject.AddComponent<Rigidbody>();
        }
        
        if (rb3d != null)
        {
            rb3d.useGravity = false; // Yerçekimi kapalı
            rb3d.linearDamping = 0f; // Hız kaybı YOK
            rb3d.angularDamping = 0f; // Dönüş kaybı YOK
            rb3d.constraints = RigidbodyConstraints.FreezeRotationX | 
                              RigidbodyConstraints.FreezeRotationY | 
                              RigidbodyConstraints.FreezeRotationZ; // Hiç dönmesin
            rb3d.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Hızlı objeler için
        }
    }

    void Start()
    {
        if (!hizAyarlandi)
        {
            Vector3 defaultYon = Vector3.forward; // Varsayılan +Z
            SetDirection(defaultYon);
        }

        Destroy(gameObject, yokOlmaSuresi);
    }

    public void Launch(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("AtesTopuController.Launch'a sifir yon verildi, default kullanılacak.");
            worldDirection = Vector3.forward;
        }

        SetDirection(worldDirection);
    }

    public void InitializeOwner(GameObject owner)
    {
        sahibi = owner;
        if (sahibi == null || triggerCollider == null)
        {
            return;
        }

        sahibinColliderlari = sahibi.GetComponentsInChildren<Collider>(true);
        foreach (var col in sahibinColliderlari)
        {
            if (col != null)
            {
                Physics.IgnoreCollision(triggerCollider, col, true);
            }
        }
    }

    void SetDirection(Vector3 direction)
    {
        hizYon = direction.normalized;
        hizAyarlandi = true;

        Debug.Log($"AtesTopuController: Yön ayarlandı = {hizYon}, Hız = {hiz}");

        if (rb3d != null)
        {
            rb3d.linearVelocity = hizYon * hiz;
            Debug.Log($"Rigidbody velocity ayarlandı: {rb3d.linearVelocity}");
        }
        else
        {
            Debug.LogError("Rigidbody bulunamadı!");
        }
    }

    void FixedUpdate()
    {
        // Velocity'yi sürekli sabit tut - fizik etkileşimlerini engelle
        if (rb3d != null && hizAyarlandi)
        {
            Vector3 currentVel = rb3d.linearVelocity;
            Vector3 targetVel = hizYon * hiz;
            
            // Eğer velocity değiştiyse, geri zorla
            if (Vector3.Distance(currentVel, targetVel) > 0.1f)
            {
                rb3d.linearVelocity = targetVel;
                Debug.LogWarning($"[ATES TOPU] Velocity düzeltildi! Eski: {currentVel}, Yeni: {targetVel}");
            }
        }
    }

    void Update()
    {
        // Kameraya bak (Billboard effect) - Perspektif yanılsamasını azaltır
        if (kamerayaBak && mainCamera != null)
        {
            EnsureGoruntuKok();
            Quaternion hedefRot = Quaternion.LookRotation(-mainCamera.transform.forward, mainCamera.transform.up);
            goruntuKok.rotation = hedefRot * Quaternion.Euler(0f, billboardYawOffset, 0f);
        }
        
        // Rigidbody yoksa manuel hareket
        if (rb3d == null && hizYon.sqrMagnitude > 0.0001f)
        {
            transform.position += hizYon * (hiz * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (sahibi != null && other.transform.IsChildOf(sahibi.transform))
        {
            return; // kendi sahibine çarpma
        }

        HandleHit(other.gameObject);
    }

    void HandleHit(GameObject other)
    {
        Debug.Log($"[ATES TOPU CARPTI] Obje: {other.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.layer)}");
        
        var health = other.GetComponentInParent<Health>() ?? other.GetComponentInChildren<Health>();
        if (health != null)
        {
            Debug.Log("Düşmana isabet etti!");
            health.TakeDamage(hasarMiktari);
            Debug.Log($"Hedefe {hasarMiktari} hasar verildi. Current: {health.currentHealth}");
            Destroy(gameObject);
        }
        else if (other.CompareTag("Duvar") || other.CompareTag("Zemin"))
        {
            Debug.Log("Duvara/Zemine çarptı, yok ediliyor!");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Bilinmeyen objeye çarptı, yok edilMEDİ.");
        }
    }

        void OnDisable()
        {
            RestoreSahipCollisions();
        }

        void OnDestroy()
        {
            RestoreSahipCollisions();
        }

    Transform BulGoruntuKokAdayi()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null && r.transform != transform)
            {
                return r.transform;
            }
        }

        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            if (ps != null && ps.transform != transform)
            {
                return ps.transform;
            }
        }

        if (transform.childCount > 0)
        {
            return transform.GetChild(0);
        }

        return null;
    }

    void EnsureGoruntuKok()
    {
        if (goruntuKok != null)
        {
            return;
        }

        goruntuKok = BulGoruntuKokAdayi();
        if (goruntuKok != null && goruntuKok != transform)
        {
            Debug.Log($"[AtesTopuController] goruntuKok otomatik {goruntuKok.name} olarak ayarlandı.");
            return;
        }

        goruntuKok = transform;
        Debug.LogWarning("[AtesTopuController] goruntuKok bulunamadı, root kullanılacak. Prefabda görsel child oluşturmayı düşünün.");
    }

    void RestoreSahipCollisions()
    {
        if (triggerCollider == null || sahibinColliderlari == null)
        {
            return;
        }

        foreach (var col in sahibinColliderlari)
        {
            if (col != null)
            {
                Physics.IgnoreCollision(triggerCollider, col, false);
            }
        }

        sahibinColliderlari = null;
        sahibi = null;
    }
}