using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button buttonColor; // ������ ��� ������������ �����
    [SerializeField] private Button buttonSave; // ������ ����������
    [SerializeField] private Button buttonLoad; //������ ��������
    [SerializeField] private Button buttonClear; // ������ �������
    [SerializeField] private DrawingManager drawingManager; //�������� ���������

    [SerializeField] private OVRHand rightHand; // ������� ����

    void Start()
    {
        // ���������� ���������� ������ 
        buttonColor.onClick.AddListener(drawingManager.ToggleColor);
        buttonClear.onClick.AddListener(drawingManager.ClearDrawing);
        buttonSave.onClick.AddListener(SaveDrawing);
        buttonLoad.onClick.AddListener(LoadDrawing);

        drawingManager.OnColorChanged += UpdateColorButton; // �������� �� �������
        UpdateColorButton(drawingManager.GetCurrentColor()); // ����� ���������� ��������� ����

        rightHand = FindObjectOfType<OVRHand>(); // ��������� ����

        // ��������� ������� ������
        SetButtonTextSize(20);
    }

    void Update()
    {

       // SimulateUIInteractionWithMouse(); // ��������� �����

        DetectFingerUIInteraction(); // �������������� �������

    }

    // �������� ��������������
    private void DetectFingerUIInteraction()
    {
        if (rightHand != null && rightHand.IsTracked && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            Ray ray = new Ray(rightHand.PointerPose.position, rightHand.PointerPose.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 2f))
            {
                Button hitButton = hit.collider.GetComponent<Button>();
                if (hitButton != null)
                {
                    hitButton.onClick.Invoke(); // ��������� �������
                }
            }
        }
    }

    // ��������� ���� ��� �����
    private void SimulateUIInteractionWithMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Button hitButton = hit.collider.GetComponent<Button>();
                if (hitButton != null)
                {
                    hitButton.onClick.Invoke();
                }
            }
        }
    }

    private void UpdateColorButton(Color newColor)
    {
        var image = buttonColor.GetComponent<Image>();
        if (image != null)
            image.color = newColor;
    }


    private void SaveDrawing()
    {
        // ����� ���������� 
        drawingManager.SaveToJson();
    }

    private void LoadDrawing()
    {
        // ����� �������� 
        drawingManager.LoadFromJson();
    }

    // ����� ��� ��������� ��������� ������
    private void SetButtonTextSize(int size)
    {
        buttonColor.GetComponentInChildren<Text>().fontSize = size;
        buttonSave.GetComponentInChildren<Text>().fontSize = size;
        buttonLoad.GetComponentInChildren<Text>().fontSize = size;
        buttonClear.GetComponentInChildren<Text>().fontSize = size;
    }
}