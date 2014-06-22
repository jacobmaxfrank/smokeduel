using UnityEngine;
using System.Collections.Generic;

public class Server : MonoBehaviour {
	public static bool connected;
	//public static readonly string GAME_NAME = "HydromancySmokeDuel";
	public static string error = null;
	public static readonly int port = 25000;

	void Start() {
		connected = false;
	}

	void OnGUI() {
		if (error != null)
			GUI.Label(new Rect(200.0f, 0.0f, 200.0f, 40.0f), error);

		if (connected)
			return;

		//Host game
		if (GUI.Button(new Rect(0.0f, 0.0f, 200.0f, 40.0f), "Host Game")) {
			NetworkConnectionError e = Network.InitializeServer(4, port,  Network.HavePublicAddress());
			if (e != NetworkConnectionError.NoError) {
				error = e.ToString();
				Debug.LogError(e);
			}
		}
	}

	public void OnServerInitialized() {
		OnConnect();
		//MasterServer.RegisterHost(GAME_NAME, System.Environment.MachineName); 
	}

	public static void OnConnect() {
		error = "Connected!";
		connected = true;

		GameObject player = Resources.Load<GameObject>("Prefabs/Player");
		player = Network.Instantiate(player, Vector3.zero, Quaternion.identity, 0) as GameObject;
		player.name = System.Environment.MachineName;

		GameObject torusCloneResource = Resources.Load<GameObject>("Prefabs/TorusClone");
		PlayerController controller = player.GetComponent<PlayerController>();
		controller._isLocalPlayer = true;

		GameObject torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		torusClone.name = player.name + " Clone";
		controller._torusHorizontal = torusClone;

		torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		torusClone.name = player.name + " Clone";
		controller._torusVertical = torusClone;

		torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		torusClone.name = player.name + " Clone";
		controller._torusCorner = torusClone;

		CFDController cfd = GameObject.Find("CFD").GetComponent<CFDController>();
		controller._CFD = cfd;
	}
}
