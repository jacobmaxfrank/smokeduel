using UnityEngine;
using System.Collections.Generic;

public class Client : MonoBehaviour {

	void Start() {
		//MasterServer.RequestHostList(Server.GAME_NAME);
	}

	void OnGUI() {
		if (Server.connected)
			return;

		//Connect to self
		if (GUI.Button(new Rect(0.0f, 80.0f, 200.0f, 40.0f), "Connect to localhost")) {
			NetworkConnectionError e = Network.Connect("127.0.0.1", Server.port);
			if (e != NetworkConnectionError.NoError) {
				Server.error = e.ToString();
				Debug.LogError(e);
			}
		}

		/*
		//Ask master server
		HostData[] hostData = MasterServer.PollHostList();
		for (int i = 0; i < hostData.Length; ++i) {
			if (hostData[i].connectedPlayers >= hostData[i].playerLimit)
				continue;

			if (GUI.Button(new Rect(0.0f, 40.0f * (i+3), 200.0f, 40.0f), hostData[i].gameName)) {
				NetworkConnectionError error = Network.Connect(hostData[i]);
				if (error != NetworkConnectionError.NoError) {
					_error = error.ToString();
					Debug.LogError(error);
				}
			}
		}
		*/
	}

	public void OnConnectedToServer() {
		Server.OnConnect();
	}

	public void OnDisconnectedFromServer() {
		Server.connected = false;
	}

	public void OnFailedToConnect(NetworkConnectionError error) {
		Server.error = error.ToString();
		Debug.LogError(error);
	}
}
