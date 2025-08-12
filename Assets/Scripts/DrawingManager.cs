using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab; // ������ �����
    [SerializeField] private Transform paper; // ������
    [SerializeField] private Color[] availableColors = { Color.red, Color.blue }; // ������ ������
    [SerializeField] private OVRHand rightHand; // ������ �� ���

    private int currentColorIndex;

    private List<GameObject> activeLines = new List<GameObject>(); // ������ �������� ����� ��� �������/����������
    private ObjectPool<LineRenderer> linePool; // ��� �������� ��� �������������� ������

    private bool isDrawing = false;
    private LineRenderer currentLine;
    private Vector3 lastPosition;

    public System.Action<Color> OnColorChanged; // ������� ��� UI

    // ��������� ������ ��� ������������
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
        // ������������� ����
        linePool = new ObjectPool<LineRenderer>(() => Instantiate(linePrefab).GetComponent<LineRenderer>(), 100); // ����. 100 ����� �� �����������

        rightHand = FindObjectOfType<OVRHand>(); 
        if (paper == null) paper = transform; 
    }

    void Update()
    {

      //  SimulateDrawingWithMouse(); // ��������� ��� ������������ ��� Quest

        DetectFingerDrawing(); // ������� ����

    }

    // �������� ��������������
    private void DetectFingerDrawing()
    {
        if (rightHand != null && rightHand.IsTracked)
        {
            bool isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            Ray ray = new Ray(rightHand.PointerPose.position, rightHand.PointerPose.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 2f)) // ����. ��������� 2�
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

    // ���������, ������������� ���� 
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
        if (Vector3.Distance(newPos, lastPosition) > 0.005f) // ����� ��� ��������� ������� ������� �����
        {
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, newPos);
            lastPosition = newPos;
        }
    }

    // ������������ �����
    public void ToggleColor()
    {
        currentColorIndex = (currentColorIndex + 1) % availableColors.Length;
        OnColorChanged?.Invoke(availableColors[currentColorIndex]);
    }

    // ����� ��� �������
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


    // ����� ����������
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
        Debug.Log("������� ������� �: " + path);
    }

    // ����� ��������
    public void LoadFromJson()
    {
        ClearDrawing(); // ������� ����� ���������
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
            Debug.Log("������� �������� ��: " + path);
        }
        else
        {
            Debug.Log("���� �� ������: " + path);
        }
    }
}

// ������� ����� ObjectPool 
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
            Object.Destroy(item.gameObject); //�������������� ������ ��� ���������� ���������
        }
    }
}