using UnityEngine;

public class DamageTest : MonoBehaviour
{
    public Health targetHealth;
    public float healAmount = 5f;

    void Update()
    {
        // D tuşu hasar verme kaldırıldı - artık yumruklar hasar veriyor
        
        // Test heal with a key press
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (targetHealth != null)
            {
                targetHealth.Heal(healAmount);
                Debug.Log("Applied " + healAmount + " healing. Current health: " + targetHealth.currentHealth);
            }
        }
    }
}
