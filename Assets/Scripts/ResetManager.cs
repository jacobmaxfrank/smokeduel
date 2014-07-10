using UnityEngine;
using System.Collections.Generic;

public interface IPreResetable {
	void PreReset();
}

public interface IResetable {
	void Reset();
}

public class ResetManager : SingletonMonoBehaviour<ResetManager> {
	private List<IPreResetable> _preResetables = new List<IPreResetable>();
	private List<IResetable> _resetables = new List<IResetable>();
	private bool _showResetButton = false;
	private bool _serverWantsReset = false;
	private bool _clientWantsReset = false;

	public void Register(IPreResetable preResetable) {
		_preResetables.Add(preResetable);
	}

	public void Register(IResetable resetable) {
		_resetables.Add(resetable);
	}

	[RPC]
	public void Reset() {
		_preResetables.RemoveAll( (IPreResetable preResetable) => {
			//Cast to MonoBehaviour because Unity has a special null for deleted components 
			return preResetable as MonoBehaviour == null;
		});
		foreach (IPreResetable preResetable in _preResetables) {
			preResetable.PreReset();
		}

		foreach (IResetable resetable in _resetables) {
			resetable.Reset();
		}
		
		_showResetButton = false;
		_serverWantsReset = false;
		_clientWantsReset = false;
	}

	public void ShowResetButton() {
		_showResetButton = true;
	}

	[RPC]
	public void ClientWantsReset() {
		_clientWantsReset = true;

		TryReset();
	}

	void OnGUI() {
		if (_showResetButton && GUI.Button(new Rect(Screen.width - 200.0f, 0.0f, 200.0f, 40.0f), "Restart")) {
			if (Network.isServer)
				_serverWantsReset = true;
			else
				GetComponent<NetworkView>().RPC("ClientWantsReset", RPCMode.Others);

			TryReset();
		}
	}

	private void TryReset() {
		if (_serverWantsReset && _clientWantsReset)
			GetComponent<NetworkView>().RPC("Reset", RPCMode.All);
	}
}
