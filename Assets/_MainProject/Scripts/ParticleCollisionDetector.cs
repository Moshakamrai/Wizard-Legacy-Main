using UnityEngine;

public class ParticleCollisionDetector : MonoBehaviour
{
    public string particleTag = "ParticleObject";
    public float pushbackForce = 20f; // Adjust the force as needed


    private void Start()
    {
        
    }

    void OnParticleCollision(GameObject other)
    {
        if (gameObject.GetComponent<Outline>())
        {
            gameObject.GetComponent<Outline>().enabled = false;
        }
        ParticleManager.Instance.PlayParticle("FirstProjectileExplosion", other.transform.position, transform.rotation, gameObject.transform);
        other.SetActive(false);
        SpellDamageControl(other);
    }

    public void SpellDamageControl(GameObject spellInfo)
    {
        int damageStat = spellInfo.GetComponent<SpellDataRetrieve>().damage;
        Debug.Log(damageStat);
    }
    
}
