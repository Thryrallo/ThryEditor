using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class DecalSceneTool
    {
        private MaterialProperty _propPosition;
        private MaterialProperty _propRotation;
        private MaterialProperty _propScale;
        private MaterialProperty _propOffset;
        private Material _material;
        private Renderer _renderer;
        private Mesh _mesh;
        private Vector2[][] _uvTriangles;
        private Vector3[][] _worldTriangles;
        private bool _isActive;
        private Mode _mode = Mode.None;
        private HandleMode _handleMode = HandleMode.Position;
        private Tool _previousTool;

        public enum Mode
        {
            None, Raycast, Handles
        }

        public enum HandleMode
        {
            Position, Rotation, Scale, Offset
        }

        private DecalSceneTool() {}

        public static DecalSceneTool Create(Renderer renderer, Material m, MaterialProperty propPosition, MaterialProperty propRotation, MaterialProperty propScale, MaterialProperty propOffset)
        {
            var tool = new DecalSceneTool();
            tool._material = m;
            tool._propPosition = propPosition;
            tool._propRotation = propRotation;
            tool._propScale = propScale;
            tool._propOffset = propOffset;
            tool._renderer = renderer;
            tool.Init();
            return tool;
        }

        public bool IsValid(Renderer renderer, Material m)
        {
            return _renderer == renderer && _material == m;
        }

        public void StartRaycastMode()
        {
            _mode = Mode.Raycast;
            this.Activate();
        }

        public void StartHandleMode()
        {
            _mode = Mode.Handles;
            this.Activate();
        }

        public void Activate()
        {
            if(_isActive) return;
            _previousTool = Tools.current;
            Tools.current = Tool.None;
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChange;
            _isActive = true;
        }

        public void Deactivate() 
        {
            if(!_isActive) return;
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChange;
            Tools.current = _previousTool;
            _isActive = false;
            _mode = Mode.None;
            
        }

        public Mode GetMode()
        {
            return _mode;
        }

        void OnSelectionChange() 
        {
            this.Deactivate();
        }

        void Init()
        {
            // EditorUtility.DisplayProgressBar("Decal Tool", "Loading Mesh...", 0.0f);
            GetMesh();
            _uvTriangles = new Vector2[_mesh.triangles.Length / 3][];
            _worldTriangles = new Vector3[_mesh.triangles.Length / 3][];
            int[] triangles = _mesh.triangles;
            Vector2[] uvs = _mesh.uv;
            Vector3[] vertices = _mesh.vertices;
            for(int i = 0; i < triangles.Length; i += 3)
            {
                // if(i%100 == 0) EditorUtility.DisplayProgressBar("Decal Tool", "Loading Mesh...", (float)i / triangles.Length);
                _uvTriangles[i / 3] = new Vector2[3];
                _worldTriangles[i / 3] = new Vector3[3];
                for(int j = 0; j < 3; j++)
                {
                    _uvTriangles[i / 3][j] = uvs[triangles[i + j]];
                    _worldTriangles[i / 3][j] = vertices[triangles[i + j]];
                }
            }
            // EditorUtility.ClearProgressBar();
        }

        private void OnSceneGUI(SceneView sceneView) 
        {
            switch(_mode)
            {
                case Mode.Raycast:
                    RaycastMode(sceneView);
                    break;
                case Mode.Handles:
                    HandlesMode(sceneView);
                    break;
            }
        }

        void RaycastMode(SceneView sceneView)
        {
            if(Tools.current != Tool.View)
            {
                Tools.current = Tool.View;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 point = _renderer.transform.InverseTransformPoint(ray.origin);
            Vector3 normal = _renderer.transform.InverseTransformDirection(ray.direction);
            ray = new Ray(point, normal);

            Vector2 uv = _propPosition.vectorValue;
            if(RaycastToClosestUV(ray, ref uv))
            {
                _propPosition.vectorValue = uv;
            }

            if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Event.current.Use();
                Deactivate();
            }
        }

        void HandlesMode(SceneView sceneView)
        {
            switch(_handleMode)
            {
                case HandleMode.Position:
                    PositionMode(sceneView);
                    break;
                case HandleMode.Rotation:
                    RotationMode(sceneView);
                    break;
                case HandleMode.Scale:
                    ScaleMode(sceneView);
                    break;
                case HandleMode.Offset:
                    OffsetMode(sceneView);
                    break;
            }

            if(Tools.current != Tool.None)
            {
                switch(Tools.current)
                {
                    case Tool.Move:
                        _handleMode = HandleMode.Position;
                        break;
                    case Tool.Rotate:
                        _handleMode = HandleMode.Rotation;
                        break;
                    case Tool.Scale:
                        _handleMode = HandleMode.Scale;
                        break;
                    case Tool.Rect:
                        _handleMode = HandleMode.Offset;
                        break;
                }
                Tools.current = Tool.None;
            }
        }

        void PositionMode(SceneView sceneView)
        {
            GetPivot();
            Vector3 pivotWorld = _renderer.transform.TransformPoint(_pivotPoint);
            Vector3 pivotNormalWorld = _renderer.transform.TransformDirection(_pivotNormal);

            if(Vector3.Dot(sceneView.camera.transform.forward, pivotNormalWorld) < 0)
            {
                pivotNormalWorld = -pivotNormalWorld;
            }
            Quaternion rotation = Quaternion.LookRotation(pivotNormalWorld, Vector3.up);

            if(Tools.pivotRotation == PivotRotation.Local)
            {
                rotation *= Quaternion.Euler(0, 0, -_propRotation.floatValue);
            }
            
            Vector3 moved = Handles.PositionHandle(pivotWorld, rotation);
            if(moved != pivotWorld)
            {
                Vector3 local = _renderer.transform.InverseTransformPoint(moved);
                Vector3 normal = _renderer.transform.InverseTransformDirection(pivotNormalWorld);
                Vector2 uv = _propPosition.vectorValue;
                Ray ray = new Ray(local - normal * 0.1f, normal);
                if(RaycastToClosestUV(ray, ref uv))
                {
                    _propPosition.vectorValue = uv;
                }
            }
        }

        void RotationMode(SceneView sceneView)
        {
            GetPivot();
            Vector3 pivotWorld = _renderer.transform.TransformPoint(_pivotPoint);
            Vector3 pivotNormalWorld = _renderer.transform.TransformDirection(_pivotNormal);

            if(Vector3.Dot(sceneView.camera.transform.forward, pivotNormalWorld) < 0)
            {
                pivotNormalWorld = -pivotNormalWorld;
            }
            Quaternion rotation = Quaternion.LookRotation(pivotNormalWorld, Vector3.up);
            rotation *= Quaternion.Euler(0, 0, -_propRotation.floatValue);

            Quaternion moved = Handles.RotationHandle(rotation, pivotWorld);
            if(moved != rotation)
            {
                Quaternion delta = Quaternion.Inverse(rotation) * moved;
                float deltaAngle = delta.eulerAngles.z;
                DecalTool.SetClampedRotation(_propRotation, _propRotation.floatValue - deltaAngle);
            }
        }

        Vector3 _initalScale;
        void ScaleMode(SceneView sceneView)
        {
            GetPivot();
            Vector3 pivotWorld = _renderer.transform.TransformPoint(_pivotPoint);
            Vector3 pivotNormalWorld = _renderer.transform.TransformDirection(_pivotNormal);

            if(Vector3.Dot(sceneView.camera.transform.forward, pivotNormalWorld) < 0)
            {
                pivotNormalWorld = -pivotNormalWorld;
            }
            Quaternion rotation = Quaternion.LookRotation(pivotNormalWorld, Vector3.up);
            rotation *= Quaternion.Euler(0, 0, -_propRotation.floatValue);

            if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _initalScale = _propScale.vectorValue;
            }

            Vector3 moved = Handles.ScaleHandle(Vector3.one, pivotWorld, rotation, HandleUtility.GetHandleSize(pivotWorld));
            if(moved != Vector3.one)
            {
                Vector4 scale = _initalScale;
                scale.x *= moved.x;
                scale.y *= moved.y;
                _propScale.vectorValue = scale;
            }
        }

        Vector4 _initalOffset;
        void OffsetMode(SceneView sceneView)
        {
            GetPivot();
            Vector3 pivotWorld = _renderer.transform.TransformPoint(_pivotPoint);
            Vector3 pivotNormalWorld = _renderer.transform.TransformDirection(_pivotNormal);

            if(Vector3.Dot(sceneView.camera.transform.forward, pivotNormalWorld) < 0)
            {
                pivotNormalWorld = -pivotNormalWorld;
            }
            Quaternion rotation = Quaternion.LookRotation(pivotNormalWorld, Vector3.up);
            rotation *= Quaternion.Euler(0, 0, -_propRotation.floatValue);

            if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                _initalOffset = _propOffset.vectorValue;
            }

            float size = HandleUtility.GetHandleSize(pivotWorld);
            float left = Handles.ScaleValueHandle(1, pivotWorld + rotation * Vector3.left * size, rotation * Quaternion.Euler(0, -90, 0), size * 5, Handles.ArrowHandleCap, 0);
            float right = Handles.ScaleValueHandle(1, pivotWorld + rotation * Vector3.right * size, rotation * Quaternion.Euler(0, 90, 0), size * 5, Handles.ArrowHandleCap, 0);
            float down = Handles.ScaleValueHandle(1, pivotWorld + rotation * Vector3.down * size, rotation * Quaternion.Euler(90, 0, 0), size * 5, Handles.ArrowHandleCap, 0);
            float up = Handles.ScaleValueHandle(1, pivotWorld + rotation * Vector3.up * size, rotation * Quaternion.Euler(-90, 0, 0), size * 5, Handles.ArrowHandleCap, 0);
            if(left != 1 || right != 1 || down != 1 || up != 1)
            {
                Vector4 offset = _initalOffset;
                offset.x -= (left - 1) * _propScale.vectorValue.x * 0.25f;
                offset.y += (right - 1) * _propScale.vectorValue.x * 0.25f;
                offset.z -= (down - 1) * _propScale.vectorValue.y * 0.25f;
                offset.w += (up - 1) * _propScale.vectorValue.y * 0.25f;
                _propOffset.vectorValue = offset;
            }
        }

        Vector3 _pivotPoint;
        Vector3 _pivotNormal;
        void GetPivot()
        {
            _pivotPoint = Vector3.zero;
            _pivotNormal = Vector3.zero;

            Vector2 uv = _propPosition.vectorValue;
            // uv position to world position using renderer mesh
            for(int i=0; i<_worldTriangles.Length;i++)
            {
                Vector2[] uvTriangle = _uvTriangles[i];
                float a = TriangleArea(uvTriangle[0], uvTriangle[1], uvTriangle[2]);
                if(a == 0) continue;
                // check if uv is inside uvTriangle
                float a1 = TriangleArea(uvTriangle[1], uvTriangle[2], uv) / a;
                if(a1 < 0) continue;
                float a2 = TriangleArea(uvTriangle[2], uvTriangle[0], uv) / a;
                if(a2 < 0) continue;
                float a3 = TriangleArea(uvTriangle[0], uvTriangle[1], uv) / a;
                if(a3 < 0) continue;
                // point inside the triangle - find mesh position by interpolation
                Vector3[] triangle = _worldTriangles[i];
                _pivotPoint = triangle[0] * a1 + triangle[1] * a2 + triangle[2] * a3;
                _pivotNormal = Vector3.Cross(triangle[1] - triangle[0], triangle[2] - triangle[0]).normalized;
                return;
            }
        }

        bool RaycastToClosestUV(Ray ray, ref Vector2 uv)
        {
            float minDistance = float.MaxValue;
            for(int i=0; i<_worldTriangles.Length;i++)
            {
                Vector3[] triangle = _worldTriangles[i];
                // raycast to triangle
                Plane plane = new Plane(triangle[0], triangle[1], triangle[2]);
                float distance;
                if(plane.Raycast(ray, out distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    // check if hitPoint is inside triangle
                    float a = TriangleArea(triangle[0], triangle[1], triangle[2]);
                    if(a == 0) continue;
                    float a1 = TriangleArea(triangle[1], triangle[2], hitPoint) / a;
                    if(a1 < 0) continue;
                    float a2 = TriangleArea(triangle[2], triangle[0], hitPoint) / a;
                    if(a2 < 0) continue;
                    float a3 = TriangleArea(triangle[0], triangle[1], hitPoint) / a;
                    if(a3 < 0) continue;
                    if(distance < minDistance)
                    {
                        minDistance = distance;
                        // point inside the triangle - find uv by interpolation
                        Vector2[] uvTriangle = _uvTriangles[i];
                        uv = uvTriangle[0] * a1 + uvTriangle[1] * a2 + uvTriangle[2] * a3;
                    }
                }
            }
            return minDistance != float.MaxValue;
        }

        float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
        {
            var v1 = a - c;
            var v2 = b - c;
            return (v1.x * v2.y - v1.y * v2.x) / 2;
        }

        void GetMesh()
        {
            if(_renderer is MeshRenderer)
            {
                _mesh = _renderer.GetComponent<MeshFilter>().sharedMesh;
            }
            else if(_renderer is SkinnedMeshRenderer)
            {
                _mesh = new Mesh();
                (_renderer as SkinnedMeshRenderer).BakeMesh(_mesh);
            }
        }
    }
}