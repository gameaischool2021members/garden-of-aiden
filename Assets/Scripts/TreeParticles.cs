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
        GameObject startingDust = GameObject.Instantiate(dust, transform.position, dust.transform.rotation);
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

    Color GetColor()
    {
        Material[] treeMaterials = gameObject.GetComponent<Renderer>().materials;

        //for (int i = 0; i < treeMaterials.Length; i++)
        //{
        //    treeMaterials[i].color = Color.red;
        //}

        return treeMaterials[0].color;
    }

    void DestroyTree()
    {
        GameObject endLeaves = GameObject.Instantiate(leaves, transform.position + new Vector3(0, 1.5f, 0), leaves.transform.rotation);
        leavesParticles = endLeaves.GetComponentInChildren<ParticleSystem>();

        var main = leavesParticles.main;
        Color treeColor = GetColor();
        main.startColor = new Color(treeColor.r, treeColor.g, treeColor.b, treeColor.a);

        leavesParticles.Play();
    }

    /* OnDestroy gets also called on scene closing so it spawns leafs in to the editor scene that dont even show up it the scene hirachy bc unity is wierd
     * so int short it  clutters the scene
    void OnDestroy()
    {
        GameObject endLeaves = GameObject.Instantiate(leaves, transform.position + new Vector3(0, 1.5f, 0), leaves.transform.rotation);
        leavesParticles = endLeaves.GetComponentInChildren<ParticleSystem>();

        var main = leavesParticles.main;
        Color treeColor = GetColor();
        main.startColor = new Color(treeColor.r, treeColor.g, treeColor.b, treeColor.a);

        leavesParticles.Play();
    }
    */
}
