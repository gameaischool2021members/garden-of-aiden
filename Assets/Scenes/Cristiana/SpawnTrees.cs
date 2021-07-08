using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTrees : MonoBehaviour
{
    public GameObject fatTree;
    public GameObject slimTree;

    float timer = 3.0f;
    bool hasSpawned = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0.0f && !hasSpawned)
        {
            GameObject tree1 =
                GameObject.Instantiate(fatTree, transform.position, transform.rotation);

            GameObject tree2 = 
                GameObject.Instantiate(slimTree, transform.position + new Vector3(0, 0, 18f), transform.rotation);

            hasSpawned = true;
        }

    }
}
