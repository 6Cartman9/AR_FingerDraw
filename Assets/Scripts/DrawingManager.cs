using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab; // Префаб лмнмм
    [SerializeField] private Transform paper; // бумага
    [SerializeField] private Color[] availableColors = { Color.red, Color.blue }; // массив цветов
    [SerializeField] private OVRHand rightHand; // ссылка на рку

    private int currentColorIndex;

    private List<GameObject> activeLines = new List<GameObject>(); // список активных линий для очистки/сохранения
    private ObjectPool<LineRenderer> linePool; // пул объектов для предотвращения утечек

    private bool isDrawing = false;
    private LineRenderer currentLine;
    private Vector3 lastPosition;

    public System.Action<Color> OnColorChanged; // событие для UI

    // структура данных для сериализации
    [System.Serializable]
    public class DrawingData
    {
        public List<LineData> lines = new List<LineData>();
    }

    [System.Serializable]
    public class LineData
    {
        public List<Vector3> points = new List<Vector3>();
        public Color color;
    }

    void Start()
    {
        // инициализация пула
        linePool = new ObjectPool<LineRenderer>(() => Instantiate(linePrefab).GetComponent<LineRenderer>(), 100); // макс. 100 линий до уничтожения

        rightHand = FindObjectOfType<OVRHand>(); 
        if (paper == null) paper = transform; 
    }

    void Update()
    {

      //  SimulateDrawingWithMouse(); // симуляция для тестирования без Quest

        DetectFingerDrawing(); // трекинг руки

    }

    // реальное детектирование
    private void DetectFingerDrawing()
    {
        if (rightHand != null && rightHand.IsTracked)
        {
            bool isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            Ray ray = new Ray(rightHand.PointerPose.position, rightHand.PointerPose.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 2f)) // макс. дистанция 2м
            {
                if (hit.transform == paper)
                {
                    if (isPinching && !isDrawing)
                    {
                        StartNewLine(hit.point);
                    }
                    else if (isPinching && isDrawing)
                    {
                        AddPointToLine(hit.point);
                    }
                    else if (!isPinching && isDrawing)
                    {
                        isDrawing = false; 
                    }
                }
            }
        }
    }

    // симуляция, использование мыши 
    private void SimulateDrawingWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == paper)
            {
                if (Input.GetMouseButtonDown(0) && !isDrawing)
                {
                    StartNewLine(hit.point);
                }
                else if (Input.GetMouseButton(0) && isDrawing)
                {
                    AddPointToLine(hit.point);
                }
                else if (Input.GetMouseButtonUp(0) && isDrawing)
                {
                    isDrawing = false;
                }
            }
        }
    }

    private void StartNewLine(Vector3 startPos)
    {
        currentLine = linePool.Get();
        currentLine.gameObject.SetActive(true);
        currentLine.positionCount = 1;
        currentLine.SetPosition(0, startPos);
        currentLine.startColor = currentLine.endColor = availableColors[currentColorIndex];
        activeLines.Add(currentLine.gameObject);
        lastPosition = startPos;
        isDrawing = true;
    }

    private void AddPointToLine(Vector3 newPos)
    {
        if (Vector3.Distance(newPos, lastPosition) > 0.005f) // порог для избежания слишком плотных точек
        {
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, newPos);
            lastPosition = newPos;
        }
    }

    // переключение цвета
    public void ToggleColor()
    {
        currentColorIndex = (currentColorIndex + 1) % availableColors.Length;
        OnColorChanged?.Invoke(availableColors[currentColorIndex]);
    }

    // метод для очистки
    public void ClearDrawing()
    {
        foreach (var line in activeLines)
        {
            linePool.Release(line.GetComponent<LineRenderer>());
        }
        activeLines.Clear();
    }
    public Color GetCurrentColor()
    {
        return availableColors[currentColorIndex];
    }


    // метод сохранения
    public void SaveToJson()
    {
        DrawingData data = new DrawingData();
        foreach (var lineObj in activeLines)
        {
            LineRenderer line = lineObj.GetComponent<LineRenderer>();
            LineData lineData = new LineData();
            lineData.points = new List<Vector3>();
            for (int i = 0; i < line.positionCount; i++)
            {
                lineData.points.Add(line.GetPosition(i));
            }
            lineData.color = line.startColor;
            data.lines.Add(lineData);
        }

        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/drawing.json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log("Рисунок сохранён в: " + path);
    }

    // метод загрузки
    public void LoadFromJson()
    {
        ClearDrawing(); // Очистка перед загрузкой
        string path = Application.persistentDataPath + "/drawing.json";
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            DrawingData data = JsonUtility.FromJson<DrawingData>(json);
            foreach (var lineData in data.lines)
            {
                LineRenderer line = linePool.Get();
                line.gameObject.SetActive(true);
                line.positionCount = lineData.points.Count;
                for (int i = 0; i < lineData.points.Count; i++)
                {
                    line.SetPosition(i, lineData.points[i]);
                }
                line.startColor = line.endColor = lineData.color;
                activeLines.Add(line.gameObject);
            }
            Debug.Log("Рисунок загружен из: " + path);
        }
        else
        {
            Debug.Log("Файл не найден: " + path);
        }
    }
}

// Простой класс ObjectPool 
public class ObjectPool<T> where T : Component
{
    private Queue<T> poolQueue = new Queue<T>();
    private System.Func<T> createFunction;
    private int maxPoolSize;

    public ObjectPool(System.Func<T> createFunc, int maxSize)
    {
        createFunction = createFunc;
        maxPoolSize = maxSize;
    }

    public T Get()
    {
        if (poolQueue.Count > 0)
        {
            return poolQueue.Dequeue();
        }
        return createFunction();
    }

    public void Release(T item)
    {
        if (poolQueue.Count < maxPoolSize)
        {
            item.gameObject.SetActive(false);
            poolQueue.Enqueue(item);
        }
        else
        {
            Object.Destroy(item.gameObject); //предотвращение утечек при превышении максимума
        }
    }
}