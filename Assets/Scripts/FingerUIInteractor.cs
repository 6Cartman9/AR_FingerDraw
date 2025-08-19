using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FingerUIInteractor : MonoBehaviour
{
    public Camera eventCamera;
    public Canvas targetCanvas;
    public HandsIndexTipProvider handProvider;
    public Handedness hand = Handedness.Right;

    [Header("Depth (meters)")]
    public float pressDepth = 0.010f;   // расстояние которое палец должен продавить плоскость Canvas
    public float hoverDistance = 0.030f; // зона ховера

    GraphicRaycaster _raycaster;
    PointerEventData _ped;
    readonly List<RaycastResult> _hits = new();
    GameObject _current;
    bool _isPressed;

    void Awake()
    {
        if (!eventCamera) eventCamera = Camera.main;
        if (targetCanvas && targetCanvas.worldCamera == null) targetCanvas.worldCamera = eventCamera;
        _raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        _ped = new PointerEventData(EventSystem.current);
    }

    void Update()
    {
        if (!handProvider || !_raycaster) { Release(); return; }
        if (!handProvider.TryGetIndexTipPose(hand, out var pose)) { Release(); return; }

        var t = targetCanvas.transform;
        var normal = t.forward;
        var signed = Vector3.Dot(pose.position - t.position, normal);

        // конвертируем позицию пальца в экранные координаты камеры
        _ped.position = eventCamera.WorldToScreenPoint(pose.position);
        _hits.Clear();
        _raycaster.Raycast(_ped, _hits);
        var top = _hits.Count > 0 ? _hits[0].gameObject : null;

        if (top != _current)
        {
            if (_current) ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerExitHandler);
            _current = top;
            if (_current && signed > -hoverDistance)
                ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerEnterHandler);
        }

        // когда палец прошёл плоскость Canvas глубже порога
        if (_current && !_isPressed && signed < -pressDepth)
        {
            ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerDownHandler);
            _isPressed = true;
        }

        // когда палец вышел вперёд
        if (_isPressed && signed > -pressDepth * 0.5f)
        {
            ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerClickHandler);
            _isPressed = false;
        }
    }

    void Release()
    {
        if (_isPressed && _current) ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerUpHandler);
        if (_current) ExecuteEvents.Execute(_current, _ped, ExecuteEvents.pointerExitHandler);
        _current = null;
        _isPressed = false;
    }
}
