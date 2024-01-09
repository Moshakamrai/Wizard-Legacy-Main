using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombardoTest : MonoBehaviour
{
    [SerializeField] Transform[] bombSpawnPoints;
    [SerializeField] GameObject castPointPrefab;

    public bool spellCasted;

    void Update()
    {
        if (GameManager.Instance.outlinedObjectCount == 4 )
        {
            spellCasted = true;
            BombardoCastSpell();
            GameManager.Instance.outlinedObjectCount = 0;
        }
    }

    public void BombardoCastSpell()
    {
        for (int i = 0; i < GameManager.Instance.outlinedObjects.Count; i++)
        {
            
            Transform bombSpawnPoint = GameManager.Instance.outlinedObjects[i];
            Transform castPoint = Instantiate(castPointPrefab, bombSpawnPoint.position + new Vector3(0, 9f, -9f), Quaternion.identity).transform;

            FlyTowards(bombSpawnPoint, castPoint);
        }
        bombSpawnPoints = GameManager.Instance.outlinedObjects.ToArray();
    }

    public void FlyTowards(Transform target, Transform castPoint)
    {
        ParticleManager.Instance.PlayParticle("BombardoProjectile", castPoint.position, transform.rotation, castPoint);

        float distance = Vector3.Distance(castPoint.position, target.position);
        float flyDuration = distance / 10f; // Adjust the divisor to control the speed

        // Fly towards the target
        castPoint.DOMove(target.position, flyDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log("Reached the target!");
                ParticleManager.Instance.PlayParticle("BombardoProjectileExplosion", castPoint.position, transform.rotation, castPoint);
                spellCasted = true;
                //FocusOnObjects();
            });
    }
    
}
