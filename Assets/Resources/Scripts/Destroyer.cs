using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer : MonoBehaviour {

	// Скрипт для уничтожения объектов (для визуализированной навигации)
	public float time = 3f;
	void Start () {
		Destroy(gameObject, time);
	}
}
