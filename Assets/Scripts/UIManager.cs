using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button buttonColor; // кнопка дл€ переключени€ цвета
    [SerializeField] private Button buttonSave; // кнопка сохранени€
    [SerializeField] private Button buttonLoad; //кнопка загрузки
    [SerializeField] private Button buttonClear; // кнопка очистки
    [SerializeField] private DrawingManager drawingManager; //менеджер рисовани€

    [SerializeField] private OVRHand rightHand; // правуа€ рука

    void Start()
    {
        // назначение слушателей кнопок 
        buttonColor.onClick.AddListener(drawingManager.ToggleColor);
        buttonClear.onClick.AddListener(drawingManager.ClearDrawing);
        buttonSave.onClick.AddListener(SaveDrawing);
        buttonLoad.onClick.AddListener(LoadDrawing);

        drawingManager.OnColorChanged += UpdateColorButton; // подписка на событие
        UpdateColorButton(drawingManager.GetCurrentColor()); // сразу установить начальный цвет

        rightHand = FindObjectOfType<OVRHand>(); // получение руки

        // установка размера текста
        SetButtonTextSize(20);
    }

    void Update()
    {

       // SimulateUIInteractionWithMouse(); // симул€ци€ мышью

        DetectFingerUIInteraction(); // взаимодействие пальцем

    }

    // реальное детектирование
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
                    hitButton.onClick.Invoke(); // симул€ци€ нажати€
                }
            }
        }
    }

    // симул€ци€ мыши при клике
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
        // вызов сохранени€ 
        drawingManager.SaveToJson();
    }

    private void LoadDrawing()
    {
        // вызов загрузки 
        drawingManager.LoadFromJson();
    }

    // метод дл€ установки разремера текста
    private void SetButtonTextSize(int size)
    {
        buttonColor.GetComponentInChildren<Text>().fontSize = size;
        buttonSave.GetComponentInChildren<Text>().fontSize = size;
        buttonLoad.GetComponentInChildren<Text>().fontSize = size;
        buttonClear.GetComponentInChildren<Text>().fontSize = size;
    }
}