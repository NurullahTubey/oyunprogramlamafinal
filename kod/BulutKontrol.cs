using UnityEngine;

public class BulutKontrol : MonoBehaviour
{
    public GameObject asilKarakter; // Ana karakteri Inspector'dan atayın
    public GameObject dusman; // Düşman karakteri Inspector'dan atayın
    public float baslangicYuksekligi = 20f; // Bulutun başlangıç yüksekliği (gökyüzü)
    public float hedefYukseklik = 5f; // Bulutun düşmanın üzerindeki yüksekliği
    public float inisHizi = 2f; // Bulutun iniş hızı
    public float yatayHareketHizi = 5f; // Bulutun yatay hareket hızı
    public Vector3 bulutOffset = Vector3.zero; // Bulutun düşmana göre offset'i (Inspector'dan ayarlayın)
    
    [Header("Takip Ayarları")]
    [Tooltip("Bulutlar rakibi ne kadar süre takip etsin (saniye). 0 veya negatif: hep takip eder.")]
    public float takipBirakmaSuresi = 2f; // Bu süreden sonra bulut rakibi takip etmeyi bırakır
    
    [Header("Yıldırım Ayarları")]
    public GameObject yildirimPrefab; // Yıldırım prefab'ını Inspector'dan atayın
    public int yildirimSayisi = 5; // Kaç yıldırım fırlatılacak
    public float yildirimAraligi = 0.3f; // Yıldırımlar arası süre
    public float yildirimBulutAltOffset = 0.5f; // Bulutun altından ne kadar aşağıda spawn olsun
    public Vector3 yildirimRotationOffset = new Vector3(-90f, 0f, 0f); // Yıldırımın başlangıç rotasyonu
    public float geriDonusY = 15f; // Bulutların tekrar yükseleceği Y değeri
    public float geriDonusHizi = 2f; // Bulutun yukarı çıkış hızı
    public float geriDonusBekleme = 1f; // Son yıldırımdan sonra ne kadar beklesin
    
    [Header("Ses Ayarları")]
    public AudioClip sesDosyasi; // K tuşuna basınca oynatılacak ses
    public float sesGecikme = 0.5f; // K'ya bastıktan kaç saniye sonra ses çalsın
    private AudioSource audioSource;

    private bool inisBasladi = false;
    private bool yildirimBasladi = false;
    private bool geriDonusBasladi = false;
    private float sonrakiYildirim = 0f;
    private int firlatilanYildirim = 0;
    private Renderer[] tumRendererlar;
    private Vector3 pivotOffset;
    private Vector3 geriDonusHedef;
    private float geriDonusBaslamaZamani = -1f;
    private float sesOynatamaZamani = -1f;
    private float takipBaslangicZamani = -1f;
    private Vector3 sonDusmanPozisyonu;

    void Start()
    {
        tumRendererlar = GetComponentsInChildren<Renderer>();
        if (tumRendererlar == null || tumRendererlar.Length == 0)
        {
            Debug.LogWarning("BulutKontrol: BulutGrubu üzerinde Renderer bulunamadı.", this);
            return;
        }

        Bounds birlesikBounds = tumRendererlar[0].bounds;
        for (int i = 1; i < tumRendererlar.Length; i++)
        {
            birlesikBounds.Encapsulate(tumRendererlar[i].bounds);
        }

        pivotOffset = transform.position - birlesikBounds.center;

        foreach (Renderer r in tumRendererlar)
        {
            r.enabled = false;
        }
        
        // AudioSource component'ini al veya ekle
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // Ana karakter K tuşuna bastığında
        if (asilKarakter != null && dusman != null && Input.GetKeyDown(KeyCode.K))
        {
            if (tumRendererlar == null || tumRendererlar.Length == 0)
            {
                Debug.LogWarning("BulutKontrol: Renderer bulunamadığı için iniş başlatılamadı.", this);
                return;
            }

            inisBasladi = false;
            yildirimBasladi = false;
            geriDonusBasladi = false;
            firlatilanYildirim = 0;
            geriDonusBaslamaZamani = -1f;
            takipBaslangicZamani = Time.time;
            
            // Ses oynatma zamanını ayarla
            if (sesDosyasi != null)
            {
                sesOynatamaZamani = Time.time + sesGecikme;
            }

            foreach (Renderer r in tumRendererlar)
            {
                r.enabled = true;
            }

            Vector3 dusmanPozisyonu = dusman.transform.position;
            sonDusmanPozisyonu = dusmanPozisyonu;
            Vector3 baslangicMerkez = dusmanPozisyonu + bulutOffset + Vector3.up * baslangicYuksekligi;
            transform.position = baslangicMerkez + pivotOffset;
            inisBasladi = true;
        }
        
        // Ses oynatma kontrolü
        if (sesOynatamaZamani > 0f && Time.time >= sesOynatamaZamani)
        {
            if (sesDosyasi != null && audioSource != null)
            {
                audioSource.PlayOneShot(sesDosyasi);
            }
            sesOynatamaZamani = -1f;
        }

        // İniş hareketi
        if (inisBasladi)
        {
            if (dusman == null)
            {
                inisBasladi = false;
                return;
            }

            // Bulutun rakibi takip etmesi / etmeyi bırakması
            float takipGecenSure = (takipBaslangicZamani > 0f) ? Time.time - takipBaslangicZamani : 0f;
            Vector3 dusmanPozisyonu;

            // takipBirakmaSuresi <= 0 ise her zaman takip et
            if (takipBirakmaSuresi <= 0f || takipGecenSure <= takipBirakmaSuresi)
            {
                dusmanPozisyonu = dusman.transform.position;
                sonDusmanPozisyonu = dusmanPozisyonu;
            }
            else
            {
                // Süre dolduktan sonra, son kaydedilen pozisyonda sabit kal
                dusmanPozisyonu = sonDusmanPozisyonu;
            }
            Vector3 guncelHedefMerkez = dusmanPozisyonu + bulutOffset + Vector3.up * hedefYukseklik;
            Vector3 guncelHedef = guncelHedefMerkez + pivotOffset;

            Vector3 yatayPozisyon = new Vector3(
                Mathf.Lerp(transform.position.x, guncelHedef.x, yatayHareketHizi * Time.deltaTime),
                transform.position.y,
                Mathf.Lerp(transform.position.z, guncelHedef.z, yatayHareketHizi * Time.deltaTime)
            );

            float yeniY = Mathf.Lerp(transform.position.y, guncelHedef.y, inisHizi * Time.deltaTime);

            transform.position = new Vector3(yatayPozisyon.x, yeniY, yatayPozisyon.z);

            if (Vector3.Distance(transform.position, guncelHedef) < 0.05f)
            {
                transform.position = guncelHedef;
                inisBasladi = false;
                
                // İniş tamamlandı, yıldırım fırlatmayı başlat
                yildirimBasladi = true;
                firlatilanYildirim = 0;
                sonrakiYildirim = Time.time;
            }
        }
        
        // Yıldırım fırlatma
        if (yildirimBasladi && dusman != null && yildirimPrefab != null)
        {
            if (Time.time >= sonrakiYildirim && firlatilanYildirim < yildirimSayisi)
            {
                Firlat();
                firlatilanYildirim++;
                sonrakiYildirim = Time.time + yildirimAraligi;
                
                if (firlatilanYildirim >= yildirimSayisi)
                {
                    yildirimBasladi = false;
                    geriDonusBaslamaZamani = Time.time + geriDonusBekleme;
                }
            }
        }

        if (!inisBasladi && !yildirimBasladi && !geriDonusBasladi && geriDonusBaslamaZamani > 0f && Time.time >= geriDonusBaslamaZamani)
        {
            BaslatGeriDonus();
            geriDonusBaslamaZamani = -1f;
        }

        if (geriDonusBasladi)
        {
            transform.position = Vector3.Lerp(transform.position, geriDonusHedef, geriDonusHizi * Time.deltaTime);

            if (Vector3.Distance(transform.position, geriDonusHedef) < 0.05f)
            {
                transform.position = geriDonusHedef;
                geriDonusBasladi = false;
            }
        }
    }
    
    void BaslatGeriDonus()
    {
        if (tumRendererlar == null || tumRendererlar.Length == 0)
        {
            return;
        }

        Vector3 mevcutMerkez = transform.position - pivotOffset;
        Vector3 hedefMerkez = new Vector3(mevcutMerkez.x, geriDonusY, mevcutMerkez.z);
        geriDonusHedef = hedefMerkez + pivotOffset;
        geriDonusBasladi = true;
    }

    void Firlat()
    {
        // Bulutun merkezini hesapla
        Bounds birlesikBounds = tumRendererlar[0].bounds;
        for (int i = 1; i < tumRendererlar.Length; i++)
        {
            birlesikBounds.Encapsulate(tumRendererlar[i].bounds);
        }
        // Bulutun dünya uzayındaki merkezini al
        Vector3 bulutMerkez = birlesikBounds.center;

        // Yıldırımı bulutun tam altına, belirli bir offset ile başlat
        float bulutAltY = birlesikBounds.min.y - yildirimBulutAltOffset;
        Vector3 yildirimPozisyon = new Vector3(bulutMerkez.x, bulutAltY, bulutMerkez.z);
        
        // Yıldırımı oluştur
        GameObject yildirim = Instantiate(yildirimPrefab, yildirimPozisyon, Quaternion.identity);
        
        // Yıldırımı buluta parent yapmak isterseniz aşağıdaki satırı açın ve localPosition'ı ayarlayın
        // yildirim.transform.SetParent(transform, worldPositionStays: true);
        
        // Yıldırımın rotasyonunu Inspector'dan ayarlanan offset ile belirle
        yildirim.transform.rotation = Quaternion.Euler(yildirimRotationOffset);

        // Hasar kontrolü: yıldırımın düştüğü noktada rakip var mı?
        if (asilKarakter != null)
        {
            karakter1yumruk punch = asilKarakter.GetComponent<karakter1yumruk>();
            if (punch == null)
            {
                punch = asilKarakter.GetComponentInChildren<karakter1yumruk>();
            }

            if (punch != null)
            {
                punch.OnSpecialHitAtPosition(yildirimPozisyon);
            }
        }
        
        // Animasyon otomatik oynar, yıldırımı belirli süre sonra yok et
        Destroy(yildirim, yildirimAraligi * 0.9f);
    }
}
