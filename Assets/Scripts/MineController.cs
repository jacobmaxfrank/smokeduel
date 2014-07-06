using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MineController : MonoBehaviour {
	public float _initialVelocity;

	private PlayerController _firer;

	void Start () {
		rigidbody2D.velocity = -_initialVelocity * transform.right;
	}

	//RPC?
	public void SetFirer(PlayerController firer) {
		_firer = firer;
		//Add firer's velocity
		rigidbody2D.velocity += firer.rigidbody2D.velocity;
	}

	[RPC]
	public void SetSprite(string filename) {
		GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(filename);
	}

	void OnDetonate() {
		_firer.MineDetonated();
	}
}
