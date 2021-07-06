using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeParticles : MonoBehaviour
{
    public GameObject dust;
    public GameObject leaves;

    ParticleSystem dustParticles;
    ParticleSystem leavesParticles;

    void Awake()
    {
        GameObject startingDust = GameObject.Instantiate(dust);
        dustParticles = startingDust.GetComponent<ParticleSystem>();
    }

    // Start is called before the first frame update
    void Start()
    {
        dustParticles.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        GameObject endLeaves = GameObject.Instantiate(leaves);
        leavesParticles = endLeaves.GetComponentInChildren<ParticleSystem>();
        leavesParticles.Play();
    }
}
