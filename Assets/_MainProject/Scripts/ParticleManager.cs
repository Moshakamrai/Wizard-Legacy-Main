using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    #region Singleton
    private static ParticleManager _instance;

    public static ParticleManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ParticleManager");
                _instance = go.AddComponent<ParticleManager>();
            }
            return _instance;
        }
    }
    #endregion

    [System.Serializable]
    public class ParticleType
    {
        public string name;
        public GameObject prefab;
        public int poolSize;
    }

    public List<ParticleType> particleTypes;

    private Dictionary<string, Queue<GameObject>> particlePools;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeParticlePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeParticlePools()
    {
        particlePools = new Dictionary<string, Queue<GameObject>>();

        foreach (var particleType in particleTypes)
        {
            Queue<GameObject> particlePool = new Queue<GameObject>();

            for (int i = 0; i < particleType.poolSize; i++)
            {
                GameObject particle = Instantiate(particleType.prefab);
                particle.SetActive(false);
                particlePool.Enqueue(particle);
            }

            particlePools.Add(particleType.name, particlePool);
        }
    }

    // Inside the ParticleManager class
    public void PlayParticle(string particleType, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (particlePools.ContainsKey(particleType))
        {
            GameObject particle = GetPooledParticle(particleType);

            if (particle != null)
            {
                particle.transform.position = position;
                particle.transform.rotation = rotation;

                if (parent != null)
                {
                    particle.transform.SetParent(parent); // Set the particle as a child of the specified parent
                }

                particle.SetActive(true);
                // Add additional logic to play particle effects (if any) here
            }
        }
        else
        {
            Debug.LogWarning("Particle type " + particleType + " not found!");
        }
    }


    GameObject GetPooledParticle(string particleType)
    {
        if (particlePools.ContainsKey(particleType))
        {
            if (particlePools[particleType].Count > 0)
            {
                return particlePools[particleType].Dequeue();
            }
            else
            {
                Debug.LogWarning("No available particles in the pool for type " + particleType);
                return null;
            }
        }
        else
        {
            Debug.LogWarning("Particle type " + particleType + " not found!");
            return null;
        }
    }

    public void ReturnToPool(string particleType, GameObject particle)
    {
        if (particlePools.ContainsKey(particleType))
        {
            particle.SetActive(false);
            particlePools[particleType].Enqueue(particle);
        }
        else
        {
            Debug.LogWarning("Particle type " + particleType + " not found!");
        }
    }
}
