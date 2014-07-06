using UnityEngine;

[RequireComponent (typeof(Thruster), typeof(Rigidbody2D))]
public class MissileController : MonoBehaviour {
	[SerializeField]
	private float _initialVelocity;

	[SerializeField]
	private float _thrustStartDelay; //seconds
	[SerializeField]
	private float _thrustEndDelay; // seconds

	private float _thrustStart, _thrustEnd; // seconds

	private PlayerController _firer;

	void Start () {
		// Setup thrust timing
		_thrustStart = Time.time + _thrustStartDelay;
		_thrustEnd = Time.time + _thrustEndDelay;

		rigidbody2D.velocity = _initialVelocity * transform.right;
	}

	//RPC?
	public void SetFirer(PlayerController firer) {
		_firer = firer;
		//Add firer's velocity
		rigidbody2D.velocity += firer.rigidbody2D.velocity;
	}

	void FixedUpdate() {
		if (Time.time >= _thrustStart && Time.time <= _thrustEnd)
			GetComponent<Thruster>().thrusting = true;
		else
			GetComponent<Thruster>().thrusting = false;
	}

	void OnDetonate() {
		_firer.MissileDetonated();
	}
}
