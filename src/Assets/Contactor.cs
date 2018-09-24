using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Contactor : MonoBehaviour {

    private void Awake()
    {
        Collector = transform.GetComponentInParent<I2DContactsCollector>();
    }

    [SerializeField]
    public I2DContactsCollector Collector;

    private void OnTriggerEnter2D(Collider2D coll)
    {
        Collector.AddCollision2D(coll);
    }
}
public interface I2DContactsCollector
{
    void AddCollision2D(Collider2D coll);
}