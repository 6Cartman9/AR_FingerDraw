using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab; // Префаб лмнмм
    [SerializeField] private Transform paper; // бумага
    [SerializeField] private Color[] availableColors = { Color.red, Color.blue }; // массив цветов
    private int currentColorIndex = 0;

    private List<GameObject> activeLines = new List<GameObject>(); // список активных линий для очистки/сохранения
    private ObjectPool<LineRenderer> linePool; // пул объектов для предотвращения утечек

    private bool isDrawing = false;
    private LineRenderer currentLine;
    private Vector3 lastPosition;

    private OVRHand rightHand; // ссылка на рку

    void Start()
    {
        // инициализация пула
        linePool = new ObjectPool<LineRenderer>(() => Instantiate(linePrefab).GetComponent<LineRenderer>(), 100); // макс. 100 линий до уничтожения

        rightHand = FindObjectOfType<OVRHand>(); 
        if (paper == null) paper = transform; 
    }

    void Update()
    {
#if UNITY_EDITOR
        SimulateDrawingWithMouse(); // симуляция для тестирования без Quest
#else
        DetectFingerDrawing(); // Реальный трекинг руки для Quest
#endif
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

    // TODO: Добавить сохранение/загрузку 
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