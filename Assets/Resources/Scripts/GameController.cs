using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

	// Контроллер игры
	public GameObject cell, unit;				// Ссылки на префабы клетки и юнита
	public Material quad, sign;					// Материалы без метки / с меткой
	public bool visualWay = false;				// Опция для вкл./выкл. визуального отображения навигации
	public bool [] signed, cubed;				// Здесь индекс - это номер ячейки на поле, а значения - есть ли метка и включен ли куб
	public int n;								// Количество клеток на одной стороне квадрата
	public Dictionary<int, List<int>> conn;		// Словарь, где ключи - это вершины графа (ячейка), а значения - список вершин, имеющих с ней общее ребро
	public Dictionary<Vector3, int> dict;		// Словарь, позволяющий по вектору позиции получить номер ячейки
	public Vector3[] dictRe;					// Список обратный словарю						
	public bool unitsGo = false;				// Состояние чекбокса
	public List<int> signedFree;				// Динамический список свободных ячеек с меткой
	
	void Start () {
		n = Random.Range(5, 11);						// Сторону поля определяем случайно
		dictRe = new Vector3[n * n];					// Инициализируем списки и словари
		dict = new Dictionary<Vector3, int>();
		conn = new Dictionary<int, List<int>>();
		signed = new bool[n * n];
		signedFree = new List<int>();
		cubed = new bool[n * n];
		for (int i = 0; i < n; i ++){								// В цикле создаём ячейки в нужных местах и для каждой
			for (int j = 0; j < n; j ++){							// делаем в списки и словарь записи значений по умолчанию
				Vector3 vector = new Vector3(i, 0f, j);
				Instantiate(cell, vector, Quaternion.identity);
				signed[i * n + j] = false;
				cubed[i * n + j] = false;
				dict.Add(vector, i * n + j);
				dictRe[i * n + j] = vector;
			}
		}
		transform.position = new Vector3((n - 1) / 2f, 0, (n - 1) / 2f);	// Центрируем объект контроллера относительно поля
		for (int i = 0; i < n * n; i++){
			conn[i] = new List<int>();
			for (int j = 0; j < n * n; j++){
				if (j == i || (j == i - 1 && i % n != 0) || (j == i + 1 && j % n != 0) || j == i + n || j == i - n) {
					conn[i].Add(j);									// В цикле добавляем информацию о рёбрах графа
				}
			}
		}
		int units = Random.Range(1, 6);								// Создаём случайное число юнитов
		for (int i = 0; i < units; i++){							// В разных случайных местах
			Instantiate(unit, dictRe[Random.Range(0, n * n)], Quaternion.identity);
		}
		int number = 0;
		foreach (GameObject un in GameObject.FindGameObjectsWithTag("Unit")){
			un.GetComponent<UnitBehaviour>().number = number;		// Каждому юниту присваиваем индивидуальный номер
			number ++;
		}
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetButtonDown("Fire2"))										// Правая кнопка мыши
        {																		// ставит / убирает метку с ячейки.
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);		// При этом меняется материал
        	RaycastHit hit;														// и соответствующие этой ячейке значения

        	if (Physics.Raycast(ray, out hit, 1000)){
				if (hit.collider.tag == "Cell"){
					GameObject cube = hit.transform.GetChild(1).gameObject;
					GameObject cell = hit.transform.GetChild(0).gameObject;
					Vector3 pos = hit.transform.position;
					Material material;
					if (signed[dict[pos]]){
						material = quad;
						signed[dict[pos]] = false;
						signedFree.Remove(dict[pos]);
					}
					else{
						material = sign;
						signed[dict[pos]] = true;
						signedFree.Add(dict[pos]);		// Индекс помеченной ячейки добавляется в лист свободных ячеек с меткой
						if (cubed[dict[pos]]){
							cubed[dict[pos]] = false;	// Если на ячейке включён куб - убираем его
							Connect(dict[pos]);			// и восстанавливаем рёбра
							cube.SetActive(false);
						}
					}
					cell.GetComponent<Renderer>().material = material;
				}
			}
        }
		if (Input.GetButtonDown("Fire1"))										// Левая кнопка мыши
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);		// Ставит / убирает куб
        	RaycastHit hit;

        	if (Physics.Raycast(ray, out hit, 1000)){
				if (hit.collider.tag == "Cell"){
					GameObject cube = hit.transform.GetChild(1).gameObject;
					GameObject cell = hit.transform.GetChild(0).gameObject;
					Vector3 pos = hit.transform.position;						// Если ячейка помечена,
					signed[dict[pos]] = false;									// снимаем метку и удаляем из списка свободных помеченных ячеек
					signedFree.Remove(dict[pos]);
					cubed[dict[pos]] = true;									// Обозначаем её как ячейку с кубом
					foreach (int el in conn[dict[pos]]){						// Удаляем все рёбра для этой ячейки
						if (el != dict[pos]) conn[el].Remove(dict[pos]);
					}
					conn[dict[pos]].Clear();
					cell.GetComponent<Renderer>().material = quad;
					cube.SetActive(true);
				}
				if (hit.collider.tag == "Cube"){
					Vector3 cube = hit.transform.position;
					Vector3 pos = new Vector3(cube.x, 0f, cube.z);
					cubed[dict[pos]] = false;									// Когда убираем куб, восстанавливаем все рёбра
					Connect(dict[pos]);											// для этой ячейки
					hit.transform.gameObject.SetActive(false);
				}
			}
        }
		if (Input.GetKey(KeyCode.A)){											// Клавиши "A" и "D" вращают контроллер с камерой вокруг поля
			transform.eulerAngles += new Vector3(0f, 1f, 0f);
		}
		if (Input.GetKey(KeyCode.D)){
			transform.eulerAngles -= new Vector3(0f, 1f, 0f);
		}
	}

	private void Connect(int cell){								// Функция, восстанавливающая рёбра для данной ячейки
		if (cell >= 1){											// Проверяет, существуют ли соседние ячейки и нет ли в них кубов
			if (!cubed[cell - 1] && cell % n != 0){				// Если всё так, устанавливает связь (добавляет в словарь)
				conn[cell].Add(cell - 1);						//	   n
				conn[cell - 1].Add(cell);						//	-1   1
			}													//	  -n
			if (cell >= n){
				if (!cubed[cell - n]) {
					conn[cell].Add(cell - n);
					conn[cell - n].Add(cell);
				}
			}
		}
		if (cell < n * n - 1){
			if (!cubed[cell + 1] && (cell + 1) % n != 0){
				conn[cell].Add(cell + 1);
				conn[cell + 1].Add(cell);
			}
			if (cell < n * (n - 1) - 1){
				if (!cubed[cell + n]) {
					conn[cell].Add(cell + n);
					conn[cell + n].Add(cell);
				}
			}
		}
	}

	public void UnitsGo(bool toggle){							// Метод, реагирующий на изменение состояния чекбокса,
		unitsGo = toggle;										// устанавлением соответствующего значения переменной unitsGo
		if (!unitsGo){											// В случае, если галка снимается, все ячейки с меткой
			for (int i = 0; i < n * n; i ++){					// освобождаются и добавляются в соответствующий лист
				if (signed[i] && !signedFree.Contains(i)){
					signedFree.Add(i);
				}
			}
		}
	}
}
