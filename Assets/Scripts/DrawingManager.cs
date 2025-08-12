using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab; // ������ �����
    [SerializeField] private Transform paper; // ������
    [SerializeField] private Color[] availableColors = { Color.red, Color.blue }; // ������ ������
    private int currentColorIndex = 0;

    private List<GameObject> activeLines = new List<GameObject>(); // ������ �������� ����� ��� �������/����������
    private ObjectPool<LineRenderer> linePool; // ��� �������� ��� �������������� ������

    private bool isDrawing = false;
    private LineRenderer currentLine;
    private Vector3 lastPosition;

    private OVRHand rightHand; // ������ �� ���

    void Start()
    {
        // ������������� ����
        linePool = new ObjectPool<LineRenderer>(() => Instantiate(linePrefab).GetComponent<LineRenderer>(), 100); // ����. 100 ����� �� �����������

        rightHand = FindObjectOfType<OVRHand>(); 
        if (paper == null) paper = transform; 
    }

    void Update()
    {
#if UNITY_EDITOR
        SimulateDrawingWithMouse(); // ��������� ��� ������������ ��� Quest
#else
        DetectFingerDrawing(); // �������� ������� ���� ��� Quest
#endif
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

    // TODO: �������� ����������/�������� 
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