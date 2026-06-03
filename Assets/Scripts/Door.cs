using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    
    private Material material;

    public event Action playerEntered ;
    
    void Awake()
    {
        material = GetComponent<Renderer>().material;
        
        //pitch black mesh
        material.SetColor("_BaseColor", Color.black);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerEntered?.Invoke();
            Debug.Log("Player entered");
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
