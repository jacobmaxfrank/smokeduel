using UnityEngine;
using System.Collections;

/// <summary>
/// Missile logic
/// </summary>
public class MissileController : MonoBehaviour
{
	public float _initialVelocity;

	public float _thrustStartDelay, _thrustEndDelay, _activationDelay, _autoDetonateDelay,	// seconds
				 _thrustForce;
	private float m_thrustStart, m_thrustEnd, m_activationReady, m_autoDetonate;			// seconds

	public CFDController _CFD;
	public PlayerController _firer;

	public float _smokeAmount;

	private Sprite _normalSprite, _thrustingSprite;
	private bool _thrusting;
	public bool thrusting {
		set {
			if (_thrusting == value)
				return;

			_thrusting = value;

			Sprite sprite;
			if (_thrusting)
				sprite = _thrustingSprite;
			else
				sprite = _normalSprite;

			GetComponent<SpriteRenderer>().sprite = sprite;
			/*
			_torusHorizontal.GetComponent<SpriteRenderer>().sprite = sprite;
			_torusVertical.GetComponent<SpriteRenderer>().sprite = sprite;
			_torusCorner.GetComponent<SpriteRenderer>().sprite = sprite;
			*/
		}
		get { return _thrusting; }
	}
	public float thrustForce;


	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{
		// Setup thrust timing
		m_thrustStart = Time.time + _thrustStartDelay;
		m_thrustEnd = Time.time + _thrustEndDelay;
		m_activationReady = Time.time + _activationDelay;
		m_autoDetonate = Time.time + _autoDetonateDelay;
	}

	[RPC]
	public void SetSprite(string normalFilename, string thrustingFilename) {
		_normalSprite = Resources.Load<Sprite>(normalFilename);
		_thrustingSprite = Resources.Load<Sprite>(thrustingFilename);
		GetComponent<SpriteRenderer>().sprite = _normalSprite;

		//TODO Shouldn't do GameObject.Find during gameplay
		_CFD = GameObject.Find("CFD").GetComponent<CFDController>();
	}

	/// <summary>
	/// Update once per physics timestep
	/// </summary>
	void FixedUpdate()
	{
		if (Time.time >= m_thrustStart && Time.time <= m_thrustEnd)
		{
			thrusting = true;

			if (gameObject.networkView.isMine) {
				// Get quaternion representing heading
				Quaternion rotation = Quaternion.Euler(0f, 0f, rigidbody2D.transform.eulerAngles.z);
				
				// Get look vector from heading quat and apply thrust along it
				Vector3 forward = rotation * new Vector3 (1f, 0f, 0f);
				forward.Normalize();
				rigidbody2D.AddForce(forward * _thrustForce);
			}
		} else {
			thrusting = false;
		}

		if (Network.isServer && _thrusting) {
			int cfd_x, cfd_y;
			_CFD.WorldToGrid (transform.position, out cfd_x, out cfd_y);
			Vector2 forward = transform.right;
			_CFD.AddUForce(-thrustForce * forward.x, cfd_x, cfd_y);
			//Unity goes y: bottom to top, CFD goes V: top to bottom
			_CFD.AddVForce(-thrustForce * -forward.y, cfd_x, cfd_y);
		}
	}

	/// <summary>
	/// Update once per frame (handle user input)
	/// </summary>
	void Update ()
	{
		if ((Input.GetAxisRaw("Missile") == 1 && Time.time >= m_activationReady) || Time.time >= m_autoDetonate)
			Detonate();
	}

	private void Detonate()
	{
		_firer._firedMissile = null;
		int cfd_x, cfd_y;
		_CFD.WorldToGrid (rigidbody2D.transform.position, out cfd_x, out cfd_y);
		if (Network.isClient)
			_CFD.gameObject.GetComponent<NetworkView>().RPC("AddDensityAt", RPCMode.Server, _smokeAmount, cfd_x, cfd_y);
		else
			_CFD.AddDensityAt(_smokeAmount, cfd_x, cfd_y);

		Network.Destroy (gameObject);
	}
}
