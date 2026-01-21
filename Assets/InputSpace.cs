using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class InputSpace : MonoBehaviour
{
    [SerializeField]
    GameObject Camera;
    [SerializeField]
    GameObject Player;

    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.transform.position;

        //transform.rotation = quaternion.Euler(new Vector3(Camera.transform.rotation.x, Camera.transform.rotation.y, Camera.transform.rotation.z));
    }
}
