using UnityEngine;

public class ParticleCollisionDetector : MonoBehaviour
{
    public string particleTag = "ParticleObject";
    public float pushbackForce = 20f; // Adjust the force as needed

    void OnParticleCollision(GameObject other)
    {
        Debug.Log("It's colliding!");
        
        ParticleManager.Instance.PlayParticle("FirstProjectileExplosion", other.transform.position, transform.rotation, gameObject.transform);
        other.gameObject.SetActive(false);
    }
    
}
