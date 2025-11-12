using UnityEngine;
using System.Collections;

public class F3DDespawn : MonoBehaviour {

    // Use this for initialization
    void Start()
    {
        StartCoroutine("Despawn");
    }

    IEnumerator Despawn()
    {
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(this.gameObject);
    }

    // Update is called once per frame
    void Update () {
	
	}
}
