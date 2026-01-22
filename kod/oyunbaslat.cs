using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
// UI elemanlarıyla (mesela Canvas) çalışmak için bunu eklemeliyiz:
using UnityEngine.UI; 
#if UNITY_EDITOR
using UnityEditor; // Editor'de Application.Quit çalışmadığı için oynatmayı durdurur
#endif

public class VideoBaslatici : MonoBehaviour
{
    [Header("Ayarlar")]
    // Videoyu oynatacak bileşen
    public VideoPlayer arkaPlanVideosu;
    // Gizleyeceğimiz Ana Menü (Canvas veya Panel)
    public GameObject anaMenuEkrani;
    [Tooltip("Video bittikten sonra açılacak sahnenin adı")]
    public string sonrakiSahneAdi = "SampleScene";

    private bool sahneYukleniyor;

    void Start()
    {
        // Başlangıçta videoyu hazırla ve ilk kareyi göster ama oynatma
        if (arkaPlanVideosu != null)
        {
            arkaPlanVideosu.playOnAwake = false;
            arkaPlanVideosu.waitForFirstFrame = true;
            arkaPlanVideosu.prepareCompleted += IlkKareyiGoster;
            arkaPlanVideosu.loopPointReached += VideodanSonraSahneAc;
            arkaPlanVideosu.Prepare();
        }
    }

    private void OnDestroy()
    {
        if (arkaPlanVideosu != null)
        {
            arkaPlanVideosu.prepareCompleted -= IlkKareyiGoster;
            arkaPlanVideosu.loopPointReached -= VideodanSonraSahneAc;
        }
    }

    // Video hazır olduğunda ilk kareyi göster
    private void IlkKareyiGoster(VideoPlayer source)
    {
        source.prepareCompleted -= IlkKareyiGoster;
        // İlk kareyi göstermek için oynat ve hemen durdur
        source.Play();
        source.Pause();
    }

    // Bu fonksiyonu "Start Fight" butonu tetikleyecek
    public void OyunuBaslat()
    {
        // 1. Videoyu oynat (menüyü gizlemeden önce!)
        if (arkaPlanVideosu != null)
        {
            // Video Player'ı etkinleştir (devre dışıysa)
            arkaPlanVideosu.enabled = true;
            arkaPlanVideosu.gameObject.SetActive(true);
            
            // Video duraklatılmış durumda, tekrar başlat
            if (arkaPlanVideosu.isPaused)
            {
                arkaPlanVideosu.Play();
            }
            else if (!arkaPlanVideosu.isPlaying)
            {
                // Eğer hiç oynatılmadıysa, baştan başlat
                arkaPlanVideosu.time = 0;
                arkaPlanVideosu.Play();
            }
        }

        // 2. Menüyü gizle
        if (anaMenuEkrani != null)
        {
            anaMenuEkrani.SetActive(false);
        }

        Debug.Log("Oyun Başladı, Video Oynuyor.");
    }

    // Çıkış butonuna bağlanır
    public void OyundanCik()
    {
        Debug.Log("Oyundan çıkılıyor.");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void VideodanSonraSahneAc(VideoPlayer source)
    {
        if (sahneYukleniyor || string.IsNullOrEmpty(sonrakiSahneAdi))
        {
            return;
        }

        sahneYukleniyor = true;
        // Video tamamlandığında belirtilen sahneyi yükle
        SceneManager.LoadScene(sonrakiSahneAdi);
    }
}