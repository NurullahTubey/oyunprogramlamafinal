using UnityEngine;

public class ThrowableMangal : MonoBehaviour
{
    [Header("Fırlatma Ayarları")]
    public float throwForce = 20f;
    public float throwAngle = 30f;
    public float rotationSpeed = 360f;
    public float maxLifetime = 8f; // Maksimum yaşam süresi

    [Header("Hasar Ayarları")]
    public float damage = 20f;
    public LayerMask enemyLayer;

    [Header("Patlama Ayarları")]
    public GameObject explosionEffect; // Patlama particle efekti
    public AudioClip explosionSound; // Patlama sesi
    public float explosionForce = 500f; // Parçaların fırlama gücü
    public float explosionRadius = 2f; // Patlama yarıçapı
    public int fragmentCount = 8; // Kaç parçaya ayrılacak
    public float minFragmentSize = 0.05f; // Minimum parça boyutu
    public float maxFragmentSize = 0.15f; // Maksimum parça boyutu

    private Rigidbody rb;
    private bool isThrown = false;
    private bool canBeThrown = true;
    private bool hitEnemy = false;
    private float throwTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Rigidbody ayarları
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Hızlı hareketlerde çarpışma kontrolü
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Daha yumuşak hareket
        
        // Başlangıçta mangal hareketsiz
        rb.isKinematic = true;
        rb.useGravity = false;

        // Collider kontrolü
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("Mangal objesinde Collider yok! Lütfen Collider ekleyin.");
        }
    }

    void Update()
    {
        // Fırlatıldıysa döndür
        if (isThrown)
        {
            transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
            
            // Maksimum süre dolmuşsa yok et
            throwTime += Time.deltaTime;
            if (throwTime >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public void ThrowMangal(Vector3 throwDirection)
    {
        if (!canBeThrown || isThrown)
            return;

        isThrown = true;
        canBeThrown = false;

        // Fizik aktif et
        rb.isKinematic = false;
        rb.useGravity = true;

        // Fırlatma açısını hesapla
        float angleInRadians = throwAngle * Mathf.Deg2Rad;
        Vector3 throwVelocity = new Vector3(
            throwDirection.x * throwForce * Mathf.Cos(angleInRadians),
            throwForce * Mathf.Sin(angleInRadians),
            throwDirection.z * throwForce * Mathf.Cos(angleInRadians)
        );

        rb.linearVelocity = throwVelocity;
        throwTime = 0f;

        Debug.Log($"Mangal fırlatıldı! Yön: {throwDirection}");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrown)
            return;

        Debug.Log($"Mangal {collision.gameObject.name} objesine çarptı!");

        // Düşmana çarptıysa hasar ver ve yok ol
        Health health = collision.gameObject.GetComponent<Health>();
        if (health != null && !hitEnemy)
        {
            health.TakeDamage(damage);
            hitEnemy = true;
            Debug.Log($"Mangal {collision.gameObject.name} objesine {damage} hasar verdi!");
            
            // PATLAMA!
            Explode();
            return;
        }

        // Zemine veya başka şeye çarptıysa hafifçe zıplasın (bounce)
        if (!hitEnemy && collision.gameObject.layer == LayerMask.NameToLayer("Default") || collision.gameObject.CompareTag("Ground"))
        {
            // Hafif bir zıplama ekle
            Vector3 bounceVelocity = rb.linearVelocity;
            bounceVelocity.y = Mathf.Abs(bounceVelocity.y) * 0.4f; // Yukarı doğru hafif zıplama
            rb.linearVelocity = bounceVelocity;
        }
    }

    public bool CanBeThrown()
    {
        return canBeThrown && !isThrown;
    }

    void Explode()
    {
        Vector3 explosionPos = transform.position;

        // 1. Patlama efekti oluştur (Particle System)
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, explosionPos, Quaternion.identity);
            Destroy(explosion, 3f); // 3 saniye sonra efekti yok et
        }

        // 2. Patlama sesi çal
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPos, 1f);
        }

        // 3. Mangal parçaları oluştur
        CreateFragments(explosionPos);

        // 4. Ana mangal objesini yok et
        Destroy(gameObject);
    }

    void CreateFragments(Vector3 center)
    {
        // Mangalın renderer ve mesh bilgilerini al
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null || meshFilter == null)
        {
            Debug.LogWarning("Mangal objesi Mesh Renderer veya Mesh Filter içermiyor, parçalar oluşturulamıyor.");
            return;
        }

        Material originalMaterial = meshRenderer.material;
        float originalScale = transform.localScale.x;

        // Basit küp parçalar oluştur (her yöne)
        for (int i = 0; i < fragmentCount; i++)
        {
            // Küçük küp parça oluştur
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.name = "MangalParca_" + i;
            
            // Rastgele pozisyon (ana objenin etrafında)
            Vector3 randomOffset = Random.insideUnitSphere * 0.3f;
            fragment.transform.position = center + randomOffset;
            
            // Küçült
            float fragmentSize = originalScale * Random.Range(minFragmentSize, maxFragmentSize);
            fragment.transform.localScale = Vector3.one * fragmentSize;
            
            // Malzemeyi kopyala
            fragment.GetComponent<Renderer>().material = originalMaterial;
            
            // Rigidbody ekle ve fırlat
            Rigidbody fragRb = fragment.GetComponent<Rigidbody>();
            if (fragRb == null)
                fragRb = fragment.AddComponent<Rigidbody>();
            
            // Rastgele yöne fırlat
            Vector3 explosionDir = (fragment.transform.position - center).normalized;
            explosionDir += Random.insideUnitSphere * 0.5f; // Biraz rastgelelik ekle
            fragRb.AddForce(explosionDir * explosionForce);
            
            // Rastgele dönme ekle
            fragRb.AddTorque(Random.insideUnitSphere * explosionForce * 0.5f);
            
            // 3-5 saniye sonra parçaları yok et
            Destroy(fragment, Random.Range(3f, 5f));
        }

        Debug.Log($"{fragmentCount} adet mangal parçası oluşturuldu!");
    }
}
