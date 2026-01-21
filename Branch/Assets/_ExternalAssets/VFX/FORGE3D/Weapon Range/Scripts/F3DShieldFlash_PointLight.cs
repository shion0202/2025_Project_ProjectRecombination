using UnityEngine;
using System.Collections;

public class F3DShieldFlash_PointLight : MonoBehaviour {

    public Light ShieldFlash;
    

	// Use this for initialization
	void Start () {
        StartCoroutine("Despawn");
	}
	
    IEnumerator Despawn()
    {
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(this.gameObject);
    }


	// Update is called once per frame
	void Update () {

        ShieldFlash.intensity = Mathf.Lerp(ShieldFlash.intensity, 0f, Time.deltaTime * 7);

	}
}
