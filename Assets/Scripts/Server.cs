using UnityEngine;
using System.Collections.Generic;

public enum PLAYER_COLOR {
	RED = 0,
	GREEN = 1,
	BLUE = 2
}

public class Server : SingletonMonoBehaviour<Server>, IResetable {
	public static bool connected;
	public static readonly string GAME_NAME = "HydromancySmokeDuel";
	public static string error = null;
	public static readonly int port = 25000;

	private static readonly string[] colorStrings = new string[] {"Red", "Green", "Blue"};
	public static PLAYER_COLOR playerColor = PLAYER_COLOR.RED;

	void Start() {
		ResetManager.Get().Register(this);
		connected = false;
	}

	public void Reset() {
		GameObject player = Resources.Load<GameObject>("Prefabs/Player");
		player = Network.Instantiate(player, Vector3.zero, Quaternion.identity, 0) as GameObject;

		GameObject torusCloneResource = Resources.Load<GameObject>("Prefabs/TorusClone");

		GameObject torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		NetworkViewID horizontalID = torusClone.networkView.viewID;

		torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		NetworkViewID verticalID = torusClone.networkView.viewID;

		torusClone = Network.Instantiate(torusCloneResource, Vector3.zero, Quaternion.identity, 0) as GameObject;
		NetworkViewID cornerID = torusClone.networkView.viewID;

		string playerColorName = "";
		switch (playerColor) {
			case PLAYER_COLOR.RED:
				playerColorName = "red";
				break;
			case PLAYER_COLOR.GREEN:
				playerColorName = "green";
				break;
			case PLAYER_COLOR.BLUE:
				playerColorName = "blue";
				break;
		}
		player.networkView.RPC("SetUpPlayer", RPCMode.AllBuffered, System.Environment.MachineName, horizontalID, verticalID, cornerID, playerColorName);
	}

	void OnGUI() {
		if (error != null)
			GUI.Label(new Rect(200.0f, 0.0f, 200.0f, 40.0f), error);

		if (connected)
			return;

		//Ship selection
		playerColor = (PLAYER_COLOR)GUI.SelectionGrid(new Rect(0.0f, 0.0f, 200.0f, 40.0f), (int)playerColor, colorStrings, colorStrings.Length);

		//Host game
		if (GUI.Button(new Rect(0.0f, 40.0f, 200.0f, 40.0f), "Host Game")) {
			NetworkConnectionError e = Network.InitializeServer(4, port,  Network.HavePublicAddress());
			if (e != NetworkConnectionError.NoError) {
				error = e.ToString();
				Debug.LogError(e);
			}
		}
	}

	public void OnServerInitialized() {
		OnConnect();
		MasterServer.RegisterHost(GAME_NAME, System.Environment.MachineName); 
	}

	public static void OnConnect() {
		error = "Connected!";
		connected = true;
		//TODO unregister when client connects
		//MasterServer.UnregisterHost();

		Get().Reset();
	}
}
