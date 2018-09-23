using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Contactor : MonoBehaviour {

    private void Awake()
    {
        Collector = transform.parent.GetComponent<I2DContactsCollector>();
    }

    [SerializeField]
    public I2DContactsCollector Collector;

    void OnCollisionEnter2D(Collision2D coll)
    {
        Debug.Log("hit?");
        Collector.AddCollision2D(coll);
    }
}
public interface I2DContactsCollector
{
    void AddCollision2D(Collision2D coll);
}