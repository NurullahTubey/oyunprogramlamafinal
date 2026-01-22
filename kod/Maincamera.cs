using UnityEngine;

// İki karakter referansını takip edip kamerayı aralarında konumlandırır.
// Dövüş oyunlarına benzer şekilde, kamera genişliği ve bakışını dinamik ayarlar.
public class Maincamera : MonoBehaviour
{
    [Header("Karakter Referansları")]
    [SerializeField] private Transform characterA;
    [SerializeField] private Transform characterB;
    // SerializeField sayesinde referanslar Inspector'dan atanabilir.

    [Header("Karakter Mesafesi (Dünya Birimi)")]
    [SerializeField] private float minCharactersDistance = 2f;
    [SerializeField] private float maxCharactersDistance = 20f;

    [Header("Kamera Offset Mesafesi")]
    [SerializeField] private float minCameraDistance = 5f;
    [SerializeField] private float maxCameraDistance = 18f;
    [SerializeField, Range(0.01f, 1f)] private float positionSmoothTime = 0.2f;

    [Header("Bakış Ayarları")]
    [SerializeField] private bool lookAtTargets = true;
    [SerializeField, Range(0.01f, 1f)] private float rotationSmoothFactor = 0.25f;

    private Vector3 offsetDirection;
    private Vector3 positionVelocity;
    // SmoothDamp için hız vektörünü saklıyoruz; Unity bu değeri her kare güncelliyor.

    private void Start()
    {
        if (characterA == null || characterB == null)
        {
            // Eksik referans varsa kamera davranışını durdurmak en güvenli yaklaşım.
            return;
        }

        // Kameranın başlangıçta merkezden güvenli bir mesafede olmasını sağlar.
        Vector3 center = GetCharactersCenter();
        Vector3 initialOffset = transform.position - center;
        if (initialOffset.sqrMagnitude < 0.001f)
        {
            initialOffset = new Vector3(0f, 5f, -10f);
        }

        offsetDirection = initialOffset.normalized;
    }

    private void LateUpdate()
    {
        if (characterA == null || characterB == null)
        {
            return;
        }

        // Karakterler uzaklaştıkça kamerayı geri çekerek ikisini de kadrajda tutar.
        float charactersDistance = Vector3.Distance(characterA.position, characterB.position);
        float normalizedDistance = Mathf.InverseLerp(minCharactersDistance, maxCharactersDistance, charactersDistance);
        float targetCameraDistance = Mathf.Lerp(minCameraDistance, maxCameraDistance, normalizedDistance);

        Vector3 center = GetCharactersCenter();
        Vector3 desiredPosition = center + offsetDirection * targetCameraDistance;
        // SmoothDamp ani zıplamaları engelleyerek oyuncuya konforlu bir kamera hissi verir.
        float smoothTime = Mathf.Max(0.0001f, positionSmoothTime);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, smoothTime);

        if (lookAtTargets)
        {
            // Geçerli bir bakış vektörü varsa döndürerek hatalı rotasyonları engeller.
            Vector3 lookDirection = center - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                // Slerp kullanarak yumuşak bir dönüş elde ediyoruz; ani çarpışmaları engeller.
                float rotationFactor = Mathf.Clamp01(rotationSmoothFactor);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationFactor);
            }
        }
    }

    private Vector3 GetCharactersCenter()
    {
        // Karakterler arasındaki orta noktayı bularak kadrajı ortalar.
        return (characterA.position + characterB.position) * 0.5f;
    }
}
