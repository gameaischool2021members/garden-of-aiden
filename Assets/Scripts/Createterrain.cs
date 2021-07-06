
using UnityEngine;

public class Createterrain : MonoBehaviour
{

    // Reference to the prefab. Drag the prefab into this field in the inspector.
    public GameObject terrainPrefab;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

            
        

    }




    private void OnMouseDown()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
            if (hit.collider != null)
            {
                //Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //Instantiate prefab to mouse position
                Instantiate(terrainPrefab, hit.point, Quaternion.identity);
            }
        
    }
}
