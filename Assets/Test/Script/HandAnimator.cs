using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;
using System.Collections.Generic;

public sealed class HandAnimator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ImageSource _source = null;
    [Space]
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] bool _useAsyncReadback = true;
    [Space]
    [SerializeField] Mesh _jointMesh = null;
    [SerializeField] Mesh _boneMesh = null;
    [Space]
    [SerializeField] Material _jointMaterial = null;
    [SerializeField] Material _boneMaterial = null;
    [Space]
    [SerializeField] RawImage _monitorUI = null;

    #endregion

    #region Private members

    HandPipeline _pipeline;

    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 17), (2, 5), (5, 9), (9, 13), (13, 17)  // Palm
    };

    static readonly int[] FingertipIndices = { 4, 8, 12, 16, 20 };
    static readonly int[] LowerFingerIndices = { 3, 7, 11, 15, 19 };

    Matrix4x4 CalculateJointXform(Vector3 pos)
      => Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.07f);

    Matrix4x4 CalculateBoneXform(Vector3 p1, Vector3 p2)
    {
        var length = Vector3.Distance(p1, p2) / 2;
        var radius = 0.03f;

        var center = (p1 + p2) / 2;
        var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
        var scale = new Vector3(radius, length, radius);

        return Matrix4x4.TRS(center, rotation, scale);
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
      => _pipeline = new HandPipeline(_resources);

    void OnDestroy()
      => _pipeline.Dispose();

    void LateUpdate()
    {
        // Feed the input image to the Hand pose pipeline.
        _pipeline.UseAsyncReadback = _useAsyncReadback;
        _pipeline.ProcessImage(_source.Texture);

        var layer = gameObject.layer;

        // List to store joint coordinates
        List<Vector3> jointCoordinates = new List<Vector3>();

        // List to store fingertip coordinates
        List<Vector3> fingertipCoordinates = new List<Vector3>();

        // Joint balls
        for (var i = 0; i < HandPipeline.KeyPointCount; i++)
        {
            var position = _pipeline.GetKeyPoint(i);
            jointCoordinates.Add(position);
            var xform = CalculateJointXform(position);
            Graphics.DrawMesh(_jointMesh, xform, _jointMaterial, layer);

            // Check if the joint is a fingertip
            if (System.Array.IndexOf(FingertipIndices, i) >= 0)
            {
                fingertipCoordinates.Add(position);
            }
        }

        // Bones
        foreach (var pair in BonePairs)
        {
            var p1 = _pipeline.GetKeyPoint(pair.Item1);
            var p2 = _pipeline.GetKeyPoint(pair.Item2);
            var xform = CalculateBoneXform(p1, p2);
            Graphics.DrawMesh(_boneMesh, xform, _boneMaterial, layer);
        }

        // UI update
        _monitorUI.texture = _source.Texture;

        // Output joint coordinates
        OutputJointCoordinates(jointCoordinates);

        // Output fingertip coordinates
        OutputFingertipCoordinates(fingertipCoordinates);

        // Check if the hand is in a fist gesture
        if (IsFistGesture(jointCoordinates))
        {
            Debug.Log("Fist gesture detected!");
        }
    }

    void OutputJointCoordinates(List<Vector3> jointCoordinates)
    {
        // Example: Log the coordinates to the console
        for (int i = 0; i < jointCoordinates.Count; i++)
        {
            Debug.Log($"Joint {i}: {jointCoordinates[i]}");
        }

        // Optionally, you can store the coordinates in a file or use them elsewhere
    }

    void OutputFingertipCoordinates(List<Vector3> fingertipCoordinates)
    {
        // Example: Log the coordinates to the console
        for (int i = 0; i < fingertipCoordinates.Count; i++)
        {
            Debug.Log($"Fingertip {i}: {fingertipCoordinates[i]}");
        }

        // Optionally, you can store the coordinates in a file or use them elsewhere
    }

    bool IsFistGesture(List<Vector3> jointCoordinates)
    {
        // Check distances between fingertip joints and their corresponding lower joints
        for (int i = 0; i < FingertipIndices.Length; i++)
        {
            var fingertip = jointCoordinates[FingertipIndices[i]];
            var lowerFinger = jointCoordinates[LowerFingerIndices[i]];

            // Adjust the distance threshold as needed
            float distanceThreshold = 0.05f;

            if (Vector3.Distance(fingertip, lowerFinger) > distanceThreshold)
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}