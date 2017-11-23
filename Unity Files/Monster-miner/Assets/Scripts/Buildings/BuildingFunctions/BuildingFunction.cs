//Oliver
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BuildingFunction : MonoBehaviour {

    [HideInInspector]
    public bool Built;
    
    public List<Collider> colliders = new List<Collider>();
    


    public virtual void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Building")
        {
            colliders.Add(collision.collider);
        }
    }
    public virtual void OnCollisionExit(Collision collision)
    {

        if(collision.collider.tag == "Building")
        {
            colliders.Remove(collision.collider);
        }
    }
    public virtual void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (Built)
        {
            
            Function();
        }
    }
    public abstract void Function();
    public virtual void OnBuilt()
    {

    }
}
