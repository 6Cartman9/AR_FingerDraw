using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable] struct V3 { public float x, y, z; public V3(Vector3 v) { x = v.x; y = v.y; z = v.z; } public Vector3 V => new(x, y, z); }
[Serializable] struct Col { public byte r, g, b, a; public Col(Color c) { var c32 = (Color32)c; r = c32.r; g = c32.g; b = c32.b; a = c32.a; } public Color C => new Color32(r, g, b, a); }
[Serializable] class StrokeDTO { public Col color; public List<V3> points = new(); }
[Serializable] class DrawingDTO { public List<StrokeDTO> strokes = new(); }

public class DrawingPlane : MonoBehaviour
{
    [Header("Refs")]
    public Camera xrCamera;
    public HandsIndexTipProvider handProvider;
    public Handedness hand = Handedness.Right;
    public LineRenderer linePrefab;
    public Transform strokesParent;

    [Header("Tuning")]
    public float touchDistance = 0.008f;     // 8 мм к касанию
    public float minPointSpacing = 0.005f;   // 5 мм между точками
    public int maxPointsPerStroke = 4000;
    public int maxActiveStrokes = 200;
    public Color currentColor = Color.red;

    readonly List<LineRenderer> _pool = new();
    readonly List<LineRenderer> _used = new();
    LineRenderer _active;
    Vector3 _lastLocal;
    Plane _plane;
    string _savePath;

    void Awake()
    {
        if (!xrCamera) xrCamera = Camera.main;
        if (!strokesParent) strokesParent = transform;
        _plane = new Plane(transform.forward, transform.position);
        _savePath = Path.Combine(Application.persistentDataPath, "drawing.json");
    }

    void Update()
    {
        if (!handProvider || !handProvider.TryGetIndexTipPose(hand, out var pose)) { EndStroke(); return; }

        float d = _plane.GetDistanceToPoint(pose.position);
        var worldOnPlane = pose.position - _plane.normal * d;
        var local = transform.InverseTransformPoint(worldOnPlane);

        bool touching = Mathf.Abs(d) < touchDistance && Inside(local);
        if (touching)
        {
            if (_active == null) BeginStroke();
            AddPoint(local);
        }
        else EndStroke();
    }

    bool Inside(Vector3 local)
    {
        // считаем Quad с размерами
        var size = new Vector2(transform.localScale.x, transform.localScale.y);
        return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f;
    }

    void BeginStroke()
    {
        _active = GetLine();
        _active.transform.SetParent(strokesParent, false);
        _active.useWorldSpace = false;
        _active.alignment = LineAlignment.View;
        _active.startColor = _active.endColor = currentColor;
        _active.positionCount = 0;
        _used.Add(_active);

        if (_used.Count > maxActiveStrokes)
        {
            ReturnLine(_used[0]);
            _used.RemoveAt(0);
        }
    }

    void AddPoint(Vector3 local)
    {
        if (!_active) return;
        if (_active.positionCount == 0 || Vector3.Distance(local, _lastLocal) >= minPointSpacing)
        {
            _active.positionCount++;
            _active.SetPosition(_active.positionCount - 1, local);
            _lastLocal = local;

            if (_active.positionCount >= maxPointsPerStroke) EndStroke();
        }
    }

    void EndStroke() { _active = null; }

    LineRenderer GetLine()
    {
        if (_pool.Count > 0)
        {
            var lr = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            lr.gameObject.SetActive(true);
            lr.positionCount = 0;
            return lr;
        }
        var created = Instantiate(linePrefab);
        created.numCornerVertices = 2;
        created.numCapVertices = 2;
        created.widthMultiplier = 0.005f;
        return created;
    }

    void ReturnLine(LineRenderer lr)
    {
        lr.gameObject.SetActive(false);
        lr.positionCount = 0;
        _pool.Add(lr);
    }

    //для UI-кнопок
    public void ToggleColor() => currentColor = (currentColor == Color.red) ? Color.blue : Color.red;

    public void ClearAll()
    {
        foreach (var lr in _used) ReturnLine(lr);
        _used.Clear();
        _active = null;
    }

    public void Save()
    {
        var dto = new DrawingDTO();
        foreach (var lr in _used)
        {
            var s = new StrokeDTO { color = new Col(lr.startColor) };
            for (int i = 0; i < lr.positionCount; i++) s.points.Add(new V3(lr.GetPosition(i)));
            dto.strokes.Add(s);
        }
        var json = JsonUtility.ToJson(dto, true);
        File.WriteAllText(_savePath, json);
        Debug.Log($"сохранение рисунка: {_savePath}");
    }

    public void LoadLast()
    {
        if (!File.Exists(_savePath)) return;
        ClearAll();
        var dto = JsonUtility.FromJson<DrawingDTO>(File.ReadAllText(_savePath));
        foreach (var s in dto.strokes)
        {
            var lr = GetLine();
            lr.transform.SetParent(strokesParent, false);
            lr.startColor = lr.endColor = s.color.C;
            lr.positionCount = s.points.Count;
            for (int i = 0; i < s.points.Count; i++) lr.SetPosition(i, s.points[i].V);
            _used.Add(lr);
        }
        Debug.Log($"загрузка рисунка: {_savePath}");
    }
}
