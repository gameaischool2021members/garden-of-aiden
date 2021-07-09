using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    GameObject cameraGO;
    [SerializeField]
    private float speed = 10f;

    [SerializeField]
    GameObject anker;
    [SerializeField]
    private float rotationSpeed = 10f;


    // Update is called once per frame
    void Update()
    {
        float xAxisValue = Input.GetAxis("Horizontal");
        float zAxisValue = Input.GetAxis("Vertical");
        xAxisValue *= -1;
        cameraGO.transform.Translate(new Vector3(0.0f, 0.0f, zAxisValue));

        anker.transform.Rotate(Vector3.up, xAxisValue * rotationSpeed * Time.deltaTime);
    }
}
