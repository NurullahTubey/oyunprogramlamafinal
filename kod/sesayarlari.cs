using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SesAyarlari : MonoBehaviour
{
    private const string SesPrefKey = "SesSeviyesiNormalized";
    private const float SesKapatmaEsigi = 0.0001f;

    private static float gecerliSes = 1f;

    [SerializeField] private GameObject ayarMenusu;
    [SerializeField] private Slider sesSlider;

    private void Awake()
    {
        UygulaKayitliSes();
        SliderDinleyicisiniEkle();
        SenkronizeSlider();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += SahneYuklendi;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SahneYuklendi;
    }

    private void OnDestroy()
    {
        SliderDinleyicisiniKaldir();
    }

    private void Start()
    {
        if (ayarMenusu != null)
        {
            ayarMenusu.SetActive(false);
        }
    }

    private void SahneYuklendi(Scene scene, LoadSceneMode mode)
    {
        ApplyNormalizedVolume(gecerliSes, false);
    }

    private void UygulaKayitliSes()
    {
        float kayitliDeger = PlayerPrefs.GetFloat(SesPrefKey, 1f);
        ApplyNormalizedVolume(kayitliDeger, false);
    }

    private void SenkronizeSlider()
    {
        if (sesSlider == null)
        {
            return;
        }

        float kayitliNormalized = PlayerPrefs.GetFloat(SesPrefKey, 1f);
        float hedefDeger = kayitliNormalized * Mathf.Max(0.0001f, sesSlider.maxValue);
        sesSlider.SetValueWithoutNotify(hedefDeger);
    }

    private void SliderDinleyicisiniEkle()
    {
        if (sesSlider == null)
        {
            return;
        }

        sesSlider.onValueChanged.RemoveListener(SesiDegistir);
        sesSlider.onValueChanged.AddListener(SesiDegistir);
    }

    private void SliderDinleyicisiniKaldir()
    {
        if (sesSlider == null)
        {
            return;
        }

        sesSlider.onValueChanged.RemoveListener(SesiDegistir);
    }

    public void AyarMenusuGoster()
    {
        if (ayarMenusu != null)
        {
            ayarMenusu.SetActive(true);
        }
    }

    public void AyarMenusuKapat()
    {
        if (ayarMenusu != null)
        {
            ayarMenusu.SetActive(false);
        }
    }

    public void SesiDegistir(float gelenDeger)
    {
        float normalizeDeger = NormalizedSliderDegeri(gelenDeger);
        ApplyNormalizedVolume(normalizeDeger, true);
    }

    private float NormalizedSliderDegeri(float sliderDegeri)
    {
        float maxDeger = sesSlider != null ? Mathf.Max(0.0001f, sesSlider.maxValue) : 10f;
        return Mathf.Clamp01(sliderDegeri / maxDeger);
    }

    private void ApplyNormalizedVolume(float normalizedDeger, bool kaydet)
    {
        normalizedDeger = Mathf.Clamp01(normalizedDeger);
        gecerliSes = normalizedDeger;

        bool mute = normalizedDeger <= SesKapatmaEsigi;

        AudioListener.volume = normalizedDeger;
        AudioListener.pause = mute;
        TumSesBilesenleriniGuncelle(normalizedDeger, mute);

        if (kaydet)
        {
            PlayerPrefs.SetFloat(SesPrefKey, normalizedDeger);
            PlayerPrefs.Save();
        }
    }

    private void TumSesBilesenleriniGuncelle(float normalizedDeger, bool mute)
    {
        var tumSesKaynaklari = FindObjectsOfType<AudioSource>(true);
        foreach (var kaynak in tumSesKaynaklari)
        {
            if (kaynak == null)
            {
                continue;
            }

            kaynak.ignoreListenerPause = false;
            kaynak.ignoreListenerVolume = false;
            kaynak.mute = mute;
        }

        var tumVideoOynaticilar = FindObjectsOfType<VideoPlayer>(true);
        foreach (var video in tumVideoOynaticilar)
        {
            if (video == null)
            {
                continue;
            }

            if (video.audioOutputMode == VideoAudioOutputMode.Direct || video.audioOutputMode == VideoAudioOutputMode.APIOnly)
            {
                ushort trackCount = video.audioTrackCount;
                for (ushort trackIndex = 0; trackIndex < trackCount; trackIndex++)
                {
                    video.SetDirectAudioMute(trackIndex, mute);
                    video.SetDirectAudioVolume(trackIndex, normalizedDeger);
                }
            }
        }
    }
}