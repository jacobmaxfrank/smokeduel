using UnityEngine;
using System.Collections;

/// <summary>
/// Mine logic
/// </summary>
public class MineController : MonoBehaviour
{
	public float _initialVelocity;

	public float _activationDelay, _autoDetonateDelay, _thrustForce;	// seconds
	private float m_activationReady, m_autoDetonate;					// seconds

	public CFDController _CFD;
	public PlayerController _firer;

	public float _smokeAmount;

	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{
		// Setup detonation timing
		m_activationReady = Time.time + _activationDelay;
		m_autoDetonate = Time.time + _autoDetonateDelay;


		// Initial thrust
		Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, transform.eulerAngles.z);
		Vector3 forward = rotation * new Vector3(1.0f, 0.0f, 0.0f);
		rigidbody2D.AddForce(forward * -_thrustForce);
	}

	/// <summary>
	/// Update once per frame (handle user input)
	/// </summary>
	void Update ()
	{
		if ((Input.GetAxisRaw("Mine") == 1 && Time.time >= m_activationReady) || Time.time >= m_autoDetonate)
			Detonate();
	}

	private void Detonate()
	{
		_firer._firedMine = null;
		int cfd_x, cfd_y;
		_CFD.WorldToGrid(transform.position, out cfd_x, out cfd_y);
		if (Network.isClient)
			_CFD.GetComponent<NetworkView>().RPC("AddDensityAt", RPCMode.Server, _smokeAmount, cfd_x, cfd_y);
		else
			_CFD.AddDensityAt(_smokeAmount, cfd_x, cfd_y);

		Network.Destroy (gameObject);
	}
}
