using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;

public class HandTracker : MonoBehaviour
{
    [Header("Hand Tracking")]
    [SerializeField] CustomImageSource _source = null;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] Shader _keyPointShader = null;
    [SerializeField] Shader _handRegionShader = null;
    [SerializeField] Text _text = null;

    HandPipeline _pipeline;
    (Material keys, Material region) _material;

    public enum Gesture
    {
        None,
        Fist,
        Palm
    }
    
    Gesture _currentGesture = Gesture.Palm;

    [Header("Puzzle")]
    [SerializeField] GameObject[] _pieces;
    [SerializeField] GameObject[] _slots;
    int _heldPiece = -1;
    bool[] _isCorrect = {false, false, false, false};

    // Start is called before the first frame update
    void Start()
    {
        _pipeline = new HandPipeline(_resources);
        _material = (new Material(_keyPointShader),
                     new Material(_handRegionShader));

        // Material initial setup
        _material.keys.SetBuffer("_KeyPoints", _pipeline.KeyPointBuffer);
        _material.region.SetBuffer("_Image", _pipeline.HandRegionCropBuffer);
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_material.keys);
        Destroy(_material.region);
    }

    void LateUpdate() 
    {
        // Feed the input image to the Hand pose pipeline.
        _pipeline.ProcessImage(_source.Texture);

        Logic();
    }

    void OnRenderObject()
    {
        // Key point circles
        _material.keys.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 96, 21);

        // Skeleton lines
        _material.keys.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, 2, 21);
    }

    void Logic()
    {
        // detect gesture
        Gesture newGesture = DetectGesture();

        if(_currentGesture != newGesture)
        {
            _currentGesture = newGesture;
            HandleNewGesture();
        }

        //Debug.Log(GetHandPosition());

        if(_heldPiece != -1)
        {
            _pieces[_heldPiece].transform.position = GetHandPosition();
        }
    }

    Gesture DetectGesture()
    {
        if(IsFist()) return Gesture.Fist;
        if(IsPalm()) return Gesture.Palm;
        return Gesture.None;
    }

    void HandleNewGesture()
    {
        if(_currentGesture == Gesture.Fist)
        {
            _text.text = "Grabbing";
            TryGrabPiece();
        }
        else if(_currentGesture == Gesture.Palm)
        {
            _text.text = "Release";
            ReleasePiece();
        }
    }

    bool IsFist()
    {
        return 
            (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Index3), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Thumb3)))       // Thumb 3

            && (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle3), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Thumb4)))       // Thumb 4

            && (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Index1), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Index4)))       // Index

            && (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle1), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle4)))      // Middle

            && (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Ring1), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Ring4)))        // Ring

            && (0.1F > Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Pinky1), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Pinky4)));      // Pinky
    }

    bool IsPalm()
    {
        return 
            (0.2F <= Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Thumb4)))       // Thumb

            && (0.3F <= Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Index4)))       // Index

            && (0.3F <= Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle4)))      // Middle

            && (0.3F <= Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Ring4)))        // Ring

            && (0.25F <= Vector3.Distance(
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist), 
                _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Pinky4)));      // Pinky
    }

    void TryGrabPiece()
    {
        if(_heldPiece != -1)
        {
            ReleasePiece();
        }

        // links .1 rechts 1
        // obn 0.5 unten -0.5
        var handPosition = GetHandPosition();
        for(var i = 0; i < 4; i++)
        {
            if(_isCorrect[i]) continue;
            Vector2 piecePosition = _pieces[i].transform.position;

            if(0.2F >= Vector2.Distance(piecePosition, handPosition))
            {
                _heldPiece = i;
            }
        }
    }

    void ReleasePiece()
    {
        if(_heldPiece == -1) return;

        Vector2 piecePosition = _pieces[_heldPiece].transform.position;
        Vector2 slotPosition = _slots[_heldPiece].transform.position;
        if(0.05F >= Vector2.Distance(piecePosition, slotPosition))
        {
            _pieces[_heldPiece].transform.position = slotPosition;

            _isCorrect[_heldPiece] = true;
        }

        _heldPiece = -1;
    }

    Vector2 GetHandPosition()
    {
        var x = _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist).x 
            + _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle1).x;

        var y = _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Wrist).y 
            + _pipeline.GetKeyPoint(HandPipeline.KeyPoint.Middle1).y;

        return new Vector2(x / 2, y / 2);
    }
}
