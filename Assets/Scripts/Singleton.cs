using UnityEngine;

//CRTP
public abstract class Singleton<T> {
	private static T _instance;

	public Singleton(T self) {
		if (_instance != null)
			Debug.LogError("Singleton already exists");

		_instance = self;
	}

	public T Get() { return _instance; }
}

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T> {
	private static T _instance;

	public virtual void Awake() {
		if (_instance != null)
			Debug.LogError("Singleton already exists");

		_instance = this as T;
	}

	public static T Get() { return _instance; }
}