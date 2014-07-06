using UnityEngine;

public class Detonateable : MonoBehaviour {
	[SerializeField]
	private string _inputDetonateAxis;

	[SerializeField]
	private float _smokeAmount;

	[SerializeField]
	private bool _autoDetonate;

	[SerializeField]
	private float _activationDelay;
	[SerializeField]
	private float _autoDetonateDelay;

	private float _activationReadyTime, _autoDetonateTime;

	void Start() {
		if (_inputDetonateAxis == "")
			_inputDetonateAxis = null;

		if (_inputDetonateAxis != null)
			_activationReadyTime = Time.time + _activationDelay;

		if (_autoDetonate)
			_autoDetonateTime = Time.time + _autoDetonateDelay;
	}

	void Update() {
		if (_inputDetonateAxis != null && Input.GetAxisRaw(_inputDetonateAxis) == 1 && Time.time >= _activationReadyTime)
			Detonate();
		else if (_autoDetonate && Time.time >= _autoDetonateTime)
			Detonate();
	}

	public void Detonate() {
		CFDController cfd = CFDController.Get();

		int cfdX, cfdY;
		cfd.WorldToGrid(transform.position, out cfdX, out cfdY);
		if (! cfd.IsInRange(cfdX) || ! cfd.IsInRange(cfdY))
			Debug.LogError("Detonating out of grid range: " + transform.position + ": " + cfdX + ", " + cfdY);

		if (Network.isClient)
			cfd.GetComponent<NetworkView>().RPC("AddDensityAt", RPCMode.Server, _smokeAmount, cfdX, cfdY);
		else
			cfd.AddDensityAt(_smokeAmount, cfdX, cfdY);

		Network.Destroy(gameObject);
		BroadcastMessage("OnDetonate", SendMessageOptions.DontRequireReceiver);
	}
}
