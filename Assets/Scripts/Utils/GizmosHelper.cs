using UnityEngine;
using Gamelogic.Extensions;
using System.Collections.Generic;

namespace UnityPrototype
{
    public static class GizmosHelper
    {
        public static void DrawPoint(Vector2 pos, float scale = 1.0f)
        {
            Gizmos.DrawLine(pos + new Vector2(-0.5f * scale, -0.5f * scale), pos + new Vector2(+0.5f * scale, +0.5f * scale));
            Gizmos.DrawLine(pos + new Vector2(+0.5f * scale, -0.5f * scale), pos + new Vector2(-0.5f * scale, +0.5f * scale));
        }

        public static void DrawVector(Vector2 pos, Vector2 dir, float scale = 1.0f)
        {
            Gizmos.DrawLine(pos, pos + dir * scale);
        }

        public static void DrawCircle(Vector2 origin, float radius, int segments = 16)
        {
            DrawEllipse(origin, Vector2.one * radius, segments);
        }

        public static void DrawEllipse(Vector2 origin, Vector2 axesLength, int segments = 16)
        {
            segments = Mathf.Max(segments, 2);

            var prevDir = Vector2.up;
            var prevPoint = prevDir * axesLength;
            var deltaAngle = 360.0f / segments;

            for (var i = 0; i < segments; i++)
            {
                var dir = prevDir.Rotate(deltaAngle);
                var pos = dir * axesLength;

                Gizmos.DrawLine(origin + prevPoint, origin + pos);

                prevPoint = pos;
                prevDir = dir;
            }
        }

        public static void DrawCurve(IEnumerable<Vector2> points, float pointSize = -1.0f, bool wireframePoint = false, bool loop = false)
        {
            Vector2? firstPoint = null;
            Vector2? prevPoint = null;

            foreach (var point in points)
            {
                if (!firstPoint.HasValue)
                    firstPoint = point;

                if (prevPoint.HasValue)
                    Gizmos.DrawLine(prevPoint.Value, point);

                if (pointSize > 0.0f)
                {
                    if (wireframePoint)
                        GizmosHelper.DrawCircle(point, pointSize);
                    else
                        Gizmos.DrawSphere(point, pointSize);
                }

                prevPoint = point;
            }

            if (loop && prevPoint.HasValue && firstPoint.HasValue)
                Gizmos.DrawLine(prevPoint.Value, firstPoint.Value);
        }

        public static float ScreenSizeToWorldSize(float size)
        {
            var camera = Camera.current;
            var center = camera.ScreenToWorldPoint(new Vector3(0, 0, 0));
            var offset = camera.ScreenToWorldPoint(new Vector3(size, 0, 0));

            return (center - offset).magnitude;
        }

        public static void DrawOutline(Vector2 position, string text)
        {
            DrawOutline(position, text, Color.white, Color.black);
        }

        public static void DrawOutline(Vector2 position, string text, Color color, Color outlineColor)
        {
            var style = new GUIStyle();
            var outlineStyle = new GUIStyle();
            style.normal.textColor = color;
            outlineStyle.normal.textColor = outlineColor;

            DrawOutline(position, text, style, outlineStyle);
        }

        public static void DrawOutline(Vector2 position, string text, GUIStyle style, GUIStyle outlineStyle)
        {
#if UNITY_EDITOR
            var offset = ScreenSizeToWorldSize(1.0f);
            UnityEditor.Handles.Label(position + new Vector2(-offset, 0), text, outlineStyle);
            UnityEditor.Handles.Label(position + new Vector2(+offset, 0), text, outlineStyle);
            UnityEditor.Handles.Label(position + new Vector2(0, -offset), text, outlineStyle);
            UnityEditor.Handles.Label(position + new Vector2(0, +offset), text, outlineStyle);

            UnityEditor.Handles.Label(position, text, style);
#endif
        }

        public static void DrawRectangle(Vector2 center, Vector2 size)
        {
            Gizmos.DrawCube(center, size);
        }

        public static void DrawFunction(Vector2 position, System.Func<float, float> func, float from, float to, float step, float tScale = 1.0f, float valScale = 1.0f)
        {
            var points = new List<Vector2>();

            Gizmos.DrawLine(position - Vector2.right * from, position + Vector2.right * to);

            for (var t = from; t <= to; t += step)
                points.Add(GetFunctionPoint(position, func, t, tScale, valScale));

            DrawCurve(points);
        }

        public static void DrawHistogram(Vector2 position, IEnumerable<float> values, float columnWidth = 1.0f, float valScale = 1.0f)
        {
            var offset = Vector2.zero;
            foreach (var value in values)
            {
                var height = value * valScale;
                var size = new Vector2(columnWidth, height);
                var center = position + offset + 0.5f * size;
                DrawRectangle(center, size);
                offset.x += columnWidth;
            }
        }

        public static void DrawFunctionValue(Vector2 position, System.Func<float, float> func, float t, float tScale = 1.0f, float valScale = 1.0f, float pointSize = 0.1f)
        {
            Gizmos.DrawSphere(GetFunctionPoint(position, func, t, tScale, valScale), pointSize);
        }

        private static Vector2 GetFunctionPoint(Vector2 position, System.Func<float, float> func, float t, float tScale = 1.0f, float valScale = 1.0f)
        {
            var value = func(t);
            return new Vector2(t * tScale, value * valScale) + position;
        }
    }
}
