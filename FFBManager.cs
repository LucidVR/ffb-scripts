/***
 * This class should be instanced once per scene.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

//Simple enum to describe some common extension lengths for force feedback
public enum FFBExtension
{
    Full = 1000,
    Half = 500,
    None = 0,
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

public class FFBManager : MonoBehaviour
{
    private ulong _bufferPtr;

    private void Awake()
    {
        OpenBuffer();
    }

    public void TriggerForceFeedback(VRFFBInput input)
    {
        //IOBuffer does some weird things with ptrs, so allocate and copy the object to unmanaged memory
        IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(input));
        try
        {
            //Get the ptr to the object
            Marshal.StructureToPtr(input, pnt, false);
            
            //Write to buffer
            EIOBufferError err = OpenVR.IOBuffer.Write(_bufferPtr, pnt, (uint) Marshal.SizeOf(typeof(VRFFBInput)));

            if (err != EIOBufferError.IOBuffer_Success)
            {
                Debug.LogError("Error writing to force feedback buffer");
            }
            else
            {
                Debug.Log("Success writing to buffer");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            //Then discard it
            Marshal.FreeHGlobal(pnt);
        }
        
    }

    private void OpenBuffer()
    {
        EIOBufferError err = OpenVR.IOBuffer.Open("/extensions/ffb/provider", EIOBufferMode.Create|EIOBufferMode.Write, (uint) Marshal.SizeOf(typeof(VRFFBInput)), 2, ref _bufferPtr);

        if (err == EIOBufferError.IOBuffer_Success)
        {
            Relax(); 
        }
        else
        {
            Debug.LogError("Error opening force feedback buffer, is it already opened?");
        }
    }

    public void Relax()
    {
        this.TriggerForceFeedback(new VRFFBInput(0, 0, 0, 0, 0, ETrackedControllerRole.LeftHand));
        this.TriggerForceFeedback(new VRFFBInput(0, 0, 0, 0, 0, ETrackedControllerRole.RightHand));
    }
    
    //This method doesn't work for some reason
    public void CloseBuffer()
    {
        EIOBufferError err = OpenVR.IOBuffer.Close(_bufferPtr);
        if (err != EIOBufferError.IOBuffer_Success)
        {
            Debug.Log("Failed to close force feedback buffer");
        }
        else
        {
            Debug.Log("Success closing force feedback buffer");
        }
    }

    public void OnDestroy()
    {
        CloseBuffer();
    }
}