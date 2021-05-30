using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;
public class FFBManager : MonoBehaviour
{
    
    private Hand _leftHand;
    private Hand _rightHand;
    
    private GameObject _playerGameObject;

    private bool _hasPrimedLeft = false;
    private bool _hasPrimedRight = false;

    private FFBProvider _ffbProvider;

    private void Awake()
    {
        _ffbProvider = new FFBProvider();
    }

    private void Start()
    {
        Debug.Log(Marshal.SizeOf(new VRFFBInput(500,500,500,500,500, ETrackedControllerRole.LeftHand)));
        _playerGameObject = GameObject.Find("Player");
        
        Player _player = _playerGameObject.GetComponent<Player>();

        foreach(Hand hand in _player.hands)
        {
            switch (hand.handType)
            {
                case SteamVR_Input_Sources.LeftHand:
                    _leftHand = hand;
                    break;
                case SteamVR_Input_Sources.RightHand:
                    _rightHand = hand;
                    break;
            }
        }
    }

    private void Update()
    {
        SetFFBFromInteractable(_leftHand, ETrackedControllerRole.LeftHand, ref _hasPrimedLeft);
        SetFFBFromInteractable(_rightHand, ETrackedControllerRole.RightHand, ref _hasPrimedRight);
    }

    private void OnApplicationQuit()
    {
        _ffbProvider.Close();
    }

    private void SetFFBFromInteractable(Hand hand, ETrackedControllerRole handedness, ref bool hasPrimed)
    {
        if (hand.hoveringInteractable)
        {
            if (!hasPrimed)
            {
                hasPrimed = true;

                SteamVR_Skeleton_Pose_Hand skeletonPose = handedness == ETrackedControllerRole.LeftHand
                    ? hand.hoveringInteractable.skeletonPoser.skeletonMainPose.leftHand
                    : hand.hoveringInteractable.skeletonPoser.skeletonMainPose.rightHand;
                
                Debug.Log("Primed force feedback" + (handedness == ETrackedControllerRole.LeftHand ? "left hand" : "right hand"));
                _ffbProvider.SetFFB(new VRFFBInput(500, 500, 500, 500, 500, handedness)); 
            }
        }
        else
        {
            if (hasPrimed)
            {
                Debug.Log("Relaxed force feedback for " + (handedness == ETrackedControllerRole.LeftHand ? "left hand" : "right hand"));
                //If we've previously primed and the object has stopped being hovered over, relax the force feedback
                _ffbProvider.RelaxFFB(handedness);
                hasPrimed = false;
            }
            
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct VRFFBInput
{
    //Curl goes between 0-1000
    public VRFFBInput(short thumbCurl, short indexCurl, short middleCurl, short ringCurl, short pinkyCurl, ETrackedControllerRole handedness)
    {
        this.thumbCurl = thumbCurl;
        this.indexCurl = indexCurl;
        this.middleCurl = middleCurl;
        this.ringCurl = ringCurl;
        this.pinkyCurl = pinkyCurl;
        this.handedness = handedness;
    }
    public short thumbCurl;
    public short indexCurl;
    public short middleCurl;
    public short ringCurl;
    public short pinkyCurl;

    public ETrackedControllerRole handedness;
};

class FFBProvider
{
    private NamedPipesProvider _namedPipeProvider;
    public FFBProvider()
    {
        _namedPipeProvider = new NamedPipesProvider();
        
        _namedPipeProvider.Connect();
    }
   
    public void SetFFB(VRFFBInput input)
    {
        Debug.Log(Marshal.SizeOf(input));
        _namedPipeProvider.Send(input);
    }
    
    public void RelaxFFB(ETrackedControllerRole hand)
    {
        SetFFB(new VRFFBInput(0,0,0,0,0, hand));
    }

    public void Close()
    {
        _namedPipeProvider.Disconnect();
    }
}

class NamedPipesProvider
{
    public string pipeName = "application/ffb";

    private NamedPipeClientStream _pipe;

    private bool _connected;
    
    public NamedPipesProvider()
    {
        _pipe = new NamedPipeClientStream(pipeName);
        _connected = false;
    }

    public void Connect()
    {
        Debug.Log("Connecting to pipe");
        _pipe.Connect();
        Debug.Log("Successfully connected to pipe");
    }

    public void Disconnect()
    {
        _pipe.Dispose();
    }

    public void Send(VRFFBInput input)
    {
        if (_pipe.IsConnected)
        {
            int size = Marshal.SizeOf(input);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(input, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            
            _pipe.Write(arr, 0, size);

        }
    }
}
