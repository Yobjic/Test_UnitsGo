using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitBehaviour : MonoBehaviour {

	// Скрипт, определяющий поведение юнита
	private GameController gc;								// Для доступа к контроллеру игры
	public GameObject sph, sph2;							// Для визуализации навигации
	private float delay;									// Задержка между передвижением
	private int index;										// Переменная, хранящая номер ячейки
	private bool moving = false, unitsGo, find = false;		// Логические переменные для переключения между состояниями
	public int number = 0;									// Индивидуальный номер юнита

	void Start () {
		gc = GameObject.Find("GameController").GetComponent<GameController>();
		delay = Random.Range(0.014f, 0.02f);				// Устанавливаем различную задержку для каждого юнита, чтобы это выглядело живее
		index = Random.Range(0, gc.n * gc.n);				// Определяем первую ячейку назначения
		while (gc.cubed[index]){							// Проверяем, нет ли там случайно куба
			index = Random.Range(0, gc.n * gc.n);			// Если есть, пробуем другую ячейку
		}
		unitsGo = gc.unitsGo;								// Инициализируем переменную соответственно значению в контроллере игры
		StartCoroutine(Move(index));						// Включаем корутину для движения к заданной ячейке
	}

	void Update()
	{
		if (unitsGo != gc.unitsGo){		// Проверяем, изменилось ли состояние чекбокса, сравнивая со значением переменной unitsGo в этом скрипте
			moving = false;				// Если да - возвращаем все значения по умолчанию
			find = false;
			unitsGo = gc.unitsGo;		// Изменяем переменную в соответствии со значением чекбокса
			StopAllCoroutines();		// Останавливаем здесь все корутины
		}
		if (!moving){					// Когда корутина не выполняется
			if (unitsGo){				// Чекбокс включен
				if (find){				// Юнит нашёл и занял ячейку
					if (!gc.signed[FindPosition(transform.position)]){		// Проверим, не снята ли ещё с неё метка
						find = false;										// Если снята - надо искать новую
					}
				}
				else{							// Если юнит не занял или не нашёл ячейку
					StartCoroutine(Find());		// Запускаем корутину поиска ближайшей ячейки
				}
			}
			else {										// Чекбокс выключен
				index = Random.Range(0, gc.n * gc.n);	// Ищем новую ячейку, куда пойдёт юнит
				if (!gc.cubed[index]){					// Если в ней нет куба
					StartCoroutine(Move(index));		// Запускаем корутину движения к ячейке назначения
				}
			}
		}
	}

	IEnumerator Move(int point)													// Корутина движения к ячейке назначения
	{
		moving = true;															// Сразу сообщаем в перменную, что идёт движение
		while (gc.dictRe[point] != transform.position && moving){				// Выполнять пока не достигли нужной ячейки или не поменялось значение чекбокса
			int move = PathFinder(point);										// Функцией поиска пути определяем следующую ячейку кратчайшего маршрута
			if (move == -1){													// Если такой точки не найдено, выходим из цикла
				break;
			}
			if (gc.visualWay) {													// Если включена визуализация
				Instantiate(sph, gc.dictRe[move], Quaternion.identity);			// Создать объект (куб) в ячейке назначения и в следующей ячейке
				Instantiate(sph2, gc.dictRe[point], Quaternion.identity);
			}
			Vector3 delta = (gc.dictRe[move] - transform.position) / 10;		// Для плавного движения будем проходить по 1/10 пути
			while ((gc.dictRe[move] - transform.position).magnitude > 0.09f){	// Пока не окажемся достаточно близко
				transform.position += delta;
				yield return new WaitForSeconds(delay);							// После каждого шага делаем паузу
			}
			transform.position = gc.dictRe[move];								// Приравниваем позицию юнита к позиции следующей ячейки
		}
		moving = false;															// Возвращаем переменную в исходное состояние (дошли до конечной ячейки)
		yield break;
	}

	IEnumerator Find(){												// Корутина поиска ячейки с меткой
		find = true;												// Указываем, что идёт поиск
		moving = true;												// Указываем, что идёт движение (чтобы не запускать корутину ещё раз каждый кадр)
		yield return new WaitForSeconds(number * 0.01f);			// Делаем небольшую задержку, чтобы два юнита не пошли занимать одную ячейку
		int minWay = 100;											// Переменная с минимальной длинной пути (максимальное значение по умолчанию)
		int way = -1;												// Переменная с номером целевой ячейки
		foreach (int point in gc.signedFree){						// Для каждого элемента в списке свободных ячеек с меткой
			int n = PathFinder(point, true);						// Находим длинну кратчайшего пути
			if (n < minWay && n != -1){								// Если она не равна -1 (путь существует) и меньше переременной минимума
				minWay = n;											// Следующие элементы будем сравнивать с ней
				way = point;
			}
		}
		if (way != -1){												// Если нашли ближайшую ячейку с меткой
			gc.signedFree.Remove(way);								// Удалим её из списка свободных ячеек с меткой
			StartCoroutine(Move(way));								// Направим туда юнита
		}
		else {														// Если подходящих ячеек нет,
			find = false;											// Возвращаем этим переменным значения по умолчанию
			moving = false;											// (проходим путь заново в следующем кадре)
		}
		yield break;
	}

	private int PathFinder(int point, bool way=false){				// Метод поиска пути
		int unit = FindPosition(transform.position);				// Определяем ячейку, в которой находится юнит
		int n = 0;													// Номер итерации
		List<int> res = new List<int>();							// Исследованные ячейки
		List<int> vis = new List<int>();							// Исследуемые ячейки
		List<int> newbies = new List<int>();						// Новые найденные ячейки, которые будут исследованны
		vis.Add(point);												// Добавляем ячейку назначения в исследуемые
		while (vis.Count > 0){										// Выполняем цикл, пока не охватим все ячейки
			n ++;													// Итерация началась
			foreach (int num in vis){								// Для каждой ячейки в исследуемых
				foreach (int i in gc.conn[num])						// Проверим каждую ячейку, с которой у неё есть ребро
				{													// Если нет ни в одном из списков
					if (!vis.Contains(i) && !res.Contains(i) && !newbies.Contains(i)){
						newbies.Add(i);								// Добавим в список новичков
						if (i == unit){								// Если это ячейка, где стоит юнит
							if (way) return n;						// Если запросили путь - вернём длинну пути
							return num;								// Возвращаем номер ячейки, в соседях которой нашли юнита
						}
					}
				}
			}
			res.AddRange(vis);										// Все исследуемые переносим в исследованные
			vis.Clear();
			vis.AddRange(newbies);									// В чистый список исследуемых переносим всех новичков
			newbies.Clear();
		}
		return -1;
	}

	int FindPosition(Vector3 vector){								// Метод для определения ближайшей ячейки к полученной точке
		float min = 1;												// (В отличие от словаря dict можно использовать при промежуточных векторах)
		int k = 0;
		for (int i = 0; i < gc.n * gc.n; i ++){
			float diff = (gc.dictRe[i] - vector).magnitude;
			if (diff < min){
				min = diff;
				k = i;
			}
		}
		return k;
	}
}
