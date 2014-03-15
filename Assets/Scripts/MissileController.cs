using UnityEngine;
using System.Collections;

/// <summary>
/// Missile logic
/// </summary>
public class MissileController : MonoBehaviour
{
	public float _initialVelocity;

	public float _thrustStartDelay, _thrustDuration,	// seconds
				 _thrustForce;
	private float m_thrustStart, m_thrustEnd;			// seconds

	/// <summary>
	/// Initialization
	/// </summary>
	void Start ()
	{
		// Setup thrust timing
		m_thrustStart = Time.time + _thrustStartDelay;
		m_thrustEnd = m_thrustStart + _thrustDuration;
	}

	/// <summary>
	/// Update once per physics timestep
	/// </summary>
	void FixedUpdate()
	{
		if (Time.time >= m_thrustStart && Time.time <= m_thrustEnd)
		{
			// Get quaternion representing heading
			Quaternion rotation = Quaternion.Euler(0f, 0f, rigidbody2D.transform.eulerAngles.z);
			
			// Get look vector from heading quat and apply thrust along it
			Vector3 forward = rotation * new Vector3 (1f, 0f, 0f);
			forward.Normalize();
			rigidbody2D.AddForce(forward * _thrustForce);
		}
	}

	/// <summary>
	/// Update once per frame (handle user input)
	/// </summary>
	void Update ()
	{
		if (Input.GetAxisRaw("Missile") == 1)
		{
			// TODO: detonate in-flight missile here if applicable
		}
	}
}
