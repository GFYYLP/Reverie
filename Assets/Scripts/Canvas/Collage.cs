using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collage : MonoBehaviour
{
    private Photographic _photographic;
    
    [SerializeField] private Vector3 openPosition = Vector3.one;
    [SerializeField] private Vector3 closePosition = Vector3.zero;
    
    private bool isOpen = false;
    
    // Start is called before the first frame update
    void Start()
    {
        _photographic = GetComponentInParent<Photographic>();
        isOpen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            if (!isOpen)
            {
                StartCoroutine(OpenBook());
            }
            else
            {
                StartCoroutine(CloseBook());
            }
        }

        if (_photographic.inCamera && isOpen)
        {
            StartCoroutine(CloseBook());
        }
    }

    IEnumerator OpenBook()
    {
        //pull the book from below
        float t = 0f;
        isOpen = true;
        while (t < 1f) {
            float animateDuration = 0.3f;
            t += Time.deltaTime / animateDuration ;

            transform.position = Vector3.Lerp(transform.position, openPosition, t);
            
            yield return null;
        } 
    }
    
    public IEnumerator CloseBook()
    {
        //pull the book from below
        float t = 0f;
        isOpen = false;
        while (t < 1f) {
            float animateDuration = 0.3f;
            t += Time.deltaTime / animateDuration ;

            transform.position = Vector3.Lerp(transform.position, closePosition, t);
            
            yield return null;
        } 
    }
}
