using UnityEngine;

public class KarakterSesleri : MonoBehaviour
{
    // Inspector'dan sürükleyip bırakacağın alanlar
    public AudioSource audioSource;
    public AudioClip yumruksesi;
    public AudioClip tekmesesi;
    public AudioClip atestopusesi;
    public AudioClip ktususesi;

    // Bu fonksiyonu animasyondan çağıracağız
    public void YumrukSesiCal()
    {
        // Eğer ses dosyası atanmışsa ve audio source varsa
        if (yumruksesi != null && audioSource != null)
        {
            // PlayOneShot kullanırız ki sesler üst üste binebilsin
            audioSource.PlayOneShot(yumruksesi);
            Debug.Log("Yumruk sesi çalındı: PlayOneShot kullanıldı", this);
            return;
        }

        // Fallback ve hata bildirimleri
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource atanmamış: `audioSource` alanını Inspector'da atayın veya GameObject'e bir AudioSource ekleyin.", this);
        }
        else if (yumruksesi == null)
        {
            // Eğer clip yoksa eğer audioSource.clip boşsa onu atayıp Play() deneyelim
            if (audioSource.clip != null)
            {
                audioSource.Play();
                Debug.Log("Yumruk sesi: AudioSource.clip üzerinden Play() çağrıldı", this);
            }
            else
            {
                Debug.LogWarning("Yumruk ses klibi atanmamış: `yumruksesi` alanına bir AudioClip atayın.", this);
            }
        }
    }

    // Bazı animator event veya diğer çağrılar farklı isimle yapılmış olabilir.
    // Faydalı olması için kısa alias ekleyelim.
    public void YumrukSesi()
    {
        YumrukSesiCal();
    }

    // Tekme sesi için benzer fonksiyon
    public void TekmeSesiCal()
    {
        if (tekmesesi != null && audioSource != null)
        {
            audioSource.PlayOneShot(tekmesesi);
            Debug.Log("Tekme sesi çalındı: PlayOneShot kullanıldı", this);
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource atanmamış: `audioSource` alanını Inspector'da atayın veya GameObject'e bir AudioSource ekleyin.", this);
        }
        else if (tekmesesi == null)
        {
            if (audioSource.clip != null)
            {
                audioSource.Play();
                Debug.Log("Tekme sesi: AudioSource.clip üzerinden Play() çağrıldı", this);
            }
            else
            {
                Debug.LogWarning("Tekme ses klibi atanmamış: `tekmesesi` alanına bir AudioClip atayın.", this);
            }
        }
    }

    public void TekmeSesi()
    {
        TekmeSesiCal();
    }

    // Ateş topu sesi için fonksiyon
    public void AtesTopuSesiCal()
    {
        if (atestopusesi != null && audioSource != null)
        {
            audioSource.PlayOneShot(atestopusesi);
            Debug.Log("Ateş topu sesi çalındı: PlayOneShot kullanıldı", this);
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource atanmamış: `audioSource` alanını Inspector'da atayın veya GameObject'e bir AudioSource ekleyin.", this);
        }
        else if (atestopusesi == null)
        {
            Debug.LogWarning("Ateş topu ses klibi atanmamış: `atestopusesi` alanına bir AudioClip atayın.", this);
        }
    }

    public void AtesTopuSesi()
    {
        AtesTopuSesiCal();
    }

    // K tuşu sesi için fonksiyon
    public void KTusuSesiCal()
    {
        if (ktususesi != null && audioSource != null)
        {
            audioSource.PlayOneShot(ktususesi);
            Debug.Log("K tuşu sesi çalındı: PlayOneShot kullanıldı", this);
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource atanmamış: `audioSource` alanını Inspector'da atayın veya GameObject'e bir AudioSource ekleyin.", this);
        }
        else if (ktususesi == null)
        {
            Debug.LogWarning("K tuşu ses klibi atanmamış: `ktususesi` alanına bir AudioClip atayın.", this);
        }
    }

    public void KTusuSesi()
    {
        KTusuSesiCal();
    }

    // (İsteğe bağlı: Eğer AudioSource'u script içinden bulmak istersen)
    void Start()
    {
        // audioSource'u Inspector'dan sürüklemezsen bu satır onu bulur
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        // Eğer halen atanmamışsa, child veya aynı GameObject üzerinde arama yapalım
        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }
}