// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using System.Collections.Generic;
using UnityEngine;

namespace Wave.Essence.BodyTracking.Demo
{
	public class DebugRotationMaster : MonoBehaviour
	{
		public List<DebugRotation> DRs;
		void Start()
		{
			for (int i = 0; i < DRs.Count; i++)
			{
				DRs[i].Rotate();
			}
		}
	}
}
