using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombardoTest : MonoBehaviour
{
    [SerializeField] Transform bombSpawnPoints;
    [SerializeField] GameObject castPointPrefab;

    [SerializeField] Transform target; // Single target instead of a list
    [SerializeField] int storedIndex;
    public bool spellCasted;

    [SerializeField] float spellSpeed;


    [SerializeField]
    private SpellData spellDatas;

    void Update()
    {
        if (GameManager.Instance.slowEffect)
        {
            BombardoCastSpell();
        }
    }

    private void Start()
    {
        storedIndex = 0;
        spellSpeed = spellDatas.spellSpeed;
    }

    public void BombardoCastSpell()
    {
        GameManager.Instance.slowEffect = false;
        target = GameManager.Instance.outlinedObject;
        FlyTowards(target);
    }

    public void FlyTowards(Transform target)
    {
        
            Transform castPoint = Instantiate(castPointPrefab, target.position + new Vector3(0, 18f, 0f), Quaternion.identity).transform;

            ParticleManager.Instance.PlayParticle("BombardoProjectile", castPoint.position, transform.rotation, castPoint);

            float distance = Vector3.Distance(castPoint.position, target.position);
            float flyDuration = distance / spellSpeed; // Adjust the divisor to control the speed

            // Fly towards the target
            castPoint.DOMove(target.position, flyDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    Debug.Log("Reached the target!");
                    ParticleManager.Instance.PlayParticle("BombardoProjectileExplosion", castPoint.position, transform.rotation, castPoint);
                    spellCasted = false;
                });
    }

}

