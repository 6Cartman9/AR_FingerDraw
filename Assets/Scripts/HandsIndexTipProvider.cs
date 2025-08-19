using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

public enum Handedness { Left, Right }


public class HandsIndexTipProvider : MonoBehaviour
{
    XRHandSubsystem _subsystem;

    void OnEnable() => EnsureSubsystem();

    void EnsureSubsystem()
    {
        if (_subsystem != null) return;
        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0) _subsystem = list[0];
    }

    public bool TryGetIndexTipPose(Handedness hand, out Pose pose)
    {
        pose = default;
        EnsureSubsystem();
        if (_subsystem == null) return false;

        var xrHand = hand == Handedness.Left ? _subsystem.leftHand : _subsystem.rightHand;
        if (!xrHand.isTracked) return false;

        var joint = xrHand.GetJoint(XRHandJointID.IndexTip);
        if (joint.TryGetPose(out var p)) { pose = p; return true; }
        return false;
    }

    public bool IsTracked(Handedness hand)
    {
        EnsureSubsystem();
        if (_subsystem == null) return false;
        var xrHand = hand == Handedness.Left ? _subsystem.leftHand : _subsystem.rightHand;
        return xrHand.isTracked;
    }
}
