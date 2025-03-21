// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using Wave.Native;
using Wave.Essence.Hand;
using UnityEngine.EventSystems;

namespace Wave.Essence.InputModule
{
	/// <summary>
	/// Draws a pointer of hand to indicate to which object is pointed.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
	public sealed class HandSpotPointer : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.InputModule.HandSpotPointer";
		private void DEBUG(string msg) { Log.d(LOG_TAG, m_PointerType + ", " + msg, true); }
		private void INFO(string msg) { Log.i(LOG_TAG, m_PointerType + ", " + msg, true); }

		#region Customized Settings
		[Tooltip("Right or left pointer.")]
		[SerializeField]
		private HandManager.HandType m_PointerType = HandManager.HandType.Right;
		public HandManager.HandType PointerType { get { return m_PointerType; } set { m_PointerType = value; } }

		[Tooltip("Show or hide the pointer.")]
		[SerializeField]
		private bool m_ShowPointer = true;
		public bool ShowPointer { get { return m_ShowPointer; } set { m_ShowPointer = value; } }

		private bool m_AutoControlPointer = true;
		public bool AutoControlPointer { get { return m_AutoControlPointer; } set { m_AutoControlPointer = value; } }

		[Tooltip("The minimal diameter of pointer.")]
		[SerializeField]
		private float m_MinimalPointerDiameter = 0.01f;
		public float MinimalPointerDiameter { get { return m_MinimalPointerDiameter; } set { m_MinimalPointerDiameter = value; } }

		/// The pointer shader uses pointerOuterDiameter as _OuterDiameter to set the pointer diameter.
		/// pointerOuterDiameter is calcuated by m_MinimalPointerDiameter and pointerGrowthMultiple.
		private float pointerGrowthMultiple = 0.02f;
		private float pointerOuterDiameter = 0;

		[Tooltip("Set to use texture.")]
		[SerializeField]
		private bool m_UseTexture = true;
		public bool UseTexture { get { return m_UseTexture; } set { m_UseTexture = value; } }

		// If not use texture, the pointer color will be set to colorFactor.
		private Color colorFactor = Color.white;               // The color variable of the pointer

		[Tooltip("True for using the default texture, false for using the custom texture.")]
		[SerializeField]
		private bool m_UseDefaultTexture = true;
		public bool UseDefaultTexture { get { return m_UseDefaultTexture; } set { m_UseDefaultTexture = value; } }

		const string kTextureWhite = "Textures/HandSpotWhite01";
		const string kTextureBlue = "Textures/HandSpotBlue01";
		private Texture2D m_WhitePointerTexture = null;
		private Texture2D m_BluePointerTexture = null;

		[Tooltip("Set the custom texture used when UseDefaultTexture is not set.")]
		[SerializeField]
		private Texture2D m_CustomTexture = null;
		public Texture2D CustomTexture { get { return m_CustomTexture; } set { m_CustomTexture = value; } }

		const float kMinimalPointerDistance = 0.1f;      // Min length of Beam
		const float kMaximalPointerDistance = 100.0f;     // Max length of Beam + 0.5m

		[Tooltip("Current pointer distance in meters.")]
		private float m_PointerDistanceInMeters = 50f;
		public float PointerDistanceInMeters { get { return m_PointerDistanceInMeters; } set { m_PointerDistanceInMeters = value; } }

		const int kPointerRenderQueueMin = 1000;
		const int kPointerRenderQueueMax = 5000;
		[Tooltip("Set the Material renderQueue.")]
		[SerializeField]
		private int m_PointerRenderQueue = kPointerRenderQueueMax;
		public int PointerRenderQueue { get { return m_PointerRenderQueue; } set { m_PointerRenderQueue = value; } }
		#endregion

		/// <summary>
		/// Material resource of pointer.
		/// It contains shader **WaveVR/CtrlrPointer** and there are 5 attributes can be changed in runtime:
		/// <para>
		/// - _OuterDiameter
		/// - _DistanceInMeters
		/// - _MainTex
		/// - _Color
		/// - _useTexture
		///
		/// If _useTexture is set (default), the texture assign in _MainTex will be used.
		/// </summary>
		const string kPointerMaterial = "Materials/HandSpotPointer01";
		private Material pointerMaterial = null;
		private Material pointerMaterialInstance = null;

		const string kPointerMeshName = "WaveEssencePointer01";
		const string kUnityMeshName = "CtrlQuadPointer";
		private Mesh m_Mesh = null;
		private MeshFilter m_MeshFilter = null;
		private MeshRenderer m_MeshRenderer = null;

		/**
		 * OEM Config
		 * \"pointer\": {
		 * \"diameter\": 0.01,
		 * \"distance\": 1.3,
		 * \"use_texture\": true,
		 * \"color\": \"#FFFFFFFF\",
		 * \"border_color\": \"#777777FF\",
		 * \"focus_color\": \"#FFFFFFFF\",
		 * \"focus_border_color\": \"#777777FF\",
		 * \"texture_name\":  null,
		 * \"Blink\": false
		 * },
		 **/

		#region MonoBehaviour overrides
		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				// Load default pointer material resource and create instance.
				pointerMaterial = Resources.Load(kPointerMaterial) as Material;
				if (pointerMaterial != null)
					pointerMaterialInstance = Instantiate<Material>(pointerMaterial);
				if (pointerMaterialInstance == null)
					INFO("OnEnable() Can NOT load default material");
				else
					INFO("OnEnable() Controller pointer material: " + pointerMaterialInstance.name);

				// Load default pointer texture resource.
				m_WhitePointerTexture = (Texture2D)Resources.Load(kTextureWhite);
				if (m_WhitePointerTexture == null)
					Log.e(LOG_TAG, "OnEnable() Can NOT load the white pointer texture", true);
				m_BluePointerTexture = (Texture2D)Resources.Load(kTextureBlue);
				if (m_BluePointerTexture == null)
					Log.e(LOG_TAG, "OnEnable() Can NOT load the blue pointer texture", true);

				// Get MeshFilter instance.
				m_MeshFilter = GetComponent<MeshFilter>();

				// Get Quad mesh as default pointer mesh.
				// m_Mesh will be re-generated in CreatePointerMesh() when not using texture.
				GameObject prim_go = GameObject.CreatePrimitive(PrimitiveType.Quad);
				m_Mesh = Instantiate(prim_go.GetComponent<MeshFilter>().sharedMesh);
				m_Mesh.name = kUnityMeshName;
				prim_go.SetActive(false);
				Destroy(prim_go);

				InitializePointer();
				HandPointerProvider.Instance.SetHandPointer(m_PointerType, gameObject);
				mEnabled = true;
			}
		}

		void OnDisable()
		{
			INFO("OnDisable()");
			pointerInitialized = false;
			mEnabled = false;
		}

		/// <summary>
		/// The attributes
		/// <para>
		/// - _Color
		/// - _OuterDiameter
		/// - _DistanceInMeters
		/// can be updated directly by changing
		/// - colorFactor
		/// - pointerOuterDiameter
		/// - PointerDistanceInMeters
		/// </summary>
		void Update()
		{
			UpdateInputModule();

			ActivatePointer(m_ShowPointer && (m_HandInputModule && m_HandInputModule.enabled));

			m_PointerDistanceInMeters = Mathf.Clamp(m_PointerDistanceInMeters, kMinimalPointerDistance, kMaximalPointerDistance);

			UpdatePointerDiameter();

			if (pointerMaterialInstance != null)
			{
				pointerMaterialInstance.renderQueue = m_PointerRenderQueue;
				pointerMaterialInstance.SetFloat("_Color", 1f);
				pointerMaterialInstance.SetFloat("_useTexture", m_UseTexture ? 1.0f : 0.0f);
				pointerMaterialInstance.SetFloat("_OuterDiameter", pointerOuterDiameter);
				pointerMaterialInstance.SetFloat("_DistanceInMeters", m_PointerDistanceInMeters);
			}
			else
			{
				if (Log.gpl.Print)
					DEBUG("Update() Pointer material is null!!");
			}

			if (Log.gpl.Print)
			{
				DEBUG("Update() " + gameObject.name + " is " + (m_MeshRenderer.enabled ? "shown" : "hidden")
					+ ", show pointer? " + m_ShowPointer
					+ ", pointer color: " + colorFactor
					+ ", use texture: " + m_UseTexture
					+ ", pointer outer diameter: " + pointerOuterDiameter
					+ ", pointer distance: " + m_PointerDistanceInMeters
					+ ", render queue: " + m_PointerRenderQueue);
			}
		}
		#endregion

		private HandInputModule m_HandInputModule = null;
		private void UpdateInputModule()
		{
			if (m_HandInputModule != null)
				return;

			if (EventSystem.current != null)
				m_HandInputModule = EventSystem.current.gameObject.GetComponent<HandInputModule>();
		}

		#region Pointer Object
		const int kReticleSegments = 20;
		private void CreatePointerMesh()
		{
			int vertexCount = (kReticleSegments + 1) * 2;
			Vector3[] vertices = new Vector3[vertexCount];
			for (int vi = 0, si = 0; si <= kReticleSegments; si++)
			{
				float angle = (float)si / (float)kReticleSegments * Mathf.PI * 2.0f;
				float x = Mathf.Sin(angle);
				float y = Mathf.Cos(angle);
				vertices[vi++] = new Vector3(x, y, 0.0f);
				vertices[vi++] = new Vector3(x, y, 1.0f);
			}

			int indicesCount = (kReticleSegments + 1) * 6;
			int[] indices = new int[indicesCount];
			int vert = 0;
			for (int ti = 0, si = 0; si < kReticleSegments; si++)
			{
				indices[ti++] = vert + 1;
				indices[ti++] = vert;
				indices[ti++] = vert + 2;
				indices[ti++] = vert + 1;
				indices[ti++] = vert + 2;
				indices[ti++] = vert + 3;

				vert += 2;
			}

			DEBUG("CreatePointerMesh() Create Mesh and add MeshFilter component.");

			m_Mesh = new Mesh();
			m_Mesh.vertices = vertices;
			m_Mesh.triangles = indices;
			m_Mesh.name = kPointerMeshName;
			m_Mesh.RecalculateBounds();
		}

		private bool pointerInitialized = false;                     // true: the mesh of reticle is created, false: the mesh of reticle is not ready
		private void InitializePointer()
		{
			if (pointerInitialized)
			{
				INFO("InitializePointer() Pointer is already initialized.");
				return;
			}

			if (m_UseTexture == false)
			{
				CreatePointerMesh();
				DEBUG("InitializePointer() Create a mesh " + kPointerMeshName);
			}
			else
			{
				DEBUG("InitializePointer() Use default mesh " + kUnityMeshName);
			}

			m_MeshFilter.mesh = m_Mesh;

			if (pointerMaterialInstance != null)
			{
				if (m_UseDefaultTexture || (null == m_CustomTexture))
				{
					DEBUG("InitializePointer() Use default texture.");
					pointerMaterialInstance.mainTexture = m_WhitePointerTexture;
					pointerMaterialInstance.SetTexture("_MainTex", m_WhitePointerTexture);
				}
				else
				{
					DEBUG("InitializePointer() Use custom texture.");
					pointerMaterialInstance.mainTexture = m_CustomTexture;
					pointerMaterialInstance.SetTexture("_MainTex", m_CustomTexture);
				}
			}
			else
			{
				Log.e(LOG_TAG, "InitializePointer() Pointer material is null!!", true);
			}

			m_MeshRenderer = GetComponent<MeshRenderer>();
			m_MeshRenderer.material = pointerMaterialInstance;
			m_MeshRenderer.sortingOrder = 32767;

			pointerInitialized = true;
		}

		private void ActivatePointer(bool show)
		{
			if (!pointerInitialized)
				InitializePointer();

			if (m_MeshRenderer.enabled != show)
			{
				m_MeshRenderer.enabled = show;
				DEBUG("ActivatePointer() " + m_MeshRenderer.enabled);
			}
		}
		#endregion

		#region Pointer Data and Size
		public Vector3 GetPointerPosition()
		{
			return transform.position + transform.forward.normalized * m_PointerDistanceInMeters;
		}

		public void OnPointerEnter(GameObject target, Vector3 intersectionPosition, bool isInteractive)
		{
			if (m_AutoControlPointer)
				m_ShowPointer = true;
			if (isInteractive)
				SetPointerTarget(intersectionPosition, isInteractive);
		}

		private void SetPointerTarget(Vector3 target, bool interactive)
		{
			Vector3 targetLocalPosition = transform.InverseTransformPoint(target);
			m_PointerDistanceInMeters = Mathf.Clamp(targetLocalPosition.z, kMinimalPointerDistance, kMaximalPointerDistance);
		}

		public void OnPointerExit(GameObject target)
		{
			if (m_AutoControlPointer)
				m_ShowPointer = false;
		}

		private void UpdatePointerDiameter()
		{
			pointerOuterDiameter = m_MinimalPointerDiameter + ((m_PointerDistanceInMeters - 1) * pointerGrowthMultiple);
		}

		public void SetEffectivePointer(bool effective)
		{
			if (!effective)
			{
				pointerMaterialInstance.mainTexture = m_WhitePointerTexture;
				pointerMaterialInstance.SetTexture("_MainTex", m_WhitePointerTexture);
			}
			else
			{
				pointerMaterialInstance.mainTexture = m_BluePointerTexture;
				pointerMaterialInstance.SetTexture("_MainTex", m_BluePointerTexture);
			}
		}
		#endregion
	}
}
