using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace TooSimpleFramework.UI
{
	/// <summary>
	/// UGUI描边
	/// </summary>
	public class OutlineExJobs : BaseMeshEffect
	{
		public Color OutlineColor = new Color(0, 0, 0, 0.5f);

		[Range(1, 6)]
		public int OutlineWidth = 1;

		[SerializeField]
		private Material _m;

		private Material M
		{
			set
			{
				_m = value;
			}
			get
			{
				if (_m == null)
				{
					var shader = Shader.Find("TSF Shaders/UI/OutlineEx");
					_m = new Material(shader);
				}
				return _m;
			}
		}

		//會拿hen多次 為了效能
		static List<UIVertex> m_VetexList = new List<UIVertex>();
		static readonly Vector2 vRight = Vector2.right;
		static readonly Vector2 vUp = Vector2.up;

		protected override void Start()
		{
			base.Start();

			//var shader = Shader.Find("TSF Shaders/UI/OutlineEx");
			base.graphic.material = M;// new Material(shader);

			var v1 = base.graphic.canvas.additionalShaderChannels;
			var v2 = AdditionalCanvasShaderChannels.TexCoord1;
			if ((v1 & v2) != v2)
			{
				base.graphic.canvas.additionalShaderChannels |= v2;
			}
			v2 = AdditionalCanvasShaderChannels.TexCoord2;
			if ((v1 & v2) != v2)
			{
				base.graphic.canvas.additionalShaderChannels |= v2;
			}

			this._Refresh();
		}


#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			if (base.graphic.material != null)
			{
				this._Refresh();
			}
		}
#endif


		private void _Refresh()
		{
			base.graphic.material.SetColor("_OutlineColor", this.OutlineColor);
			base.graphic.material.SetInt("_OutlineWidth", this.OutlineWidth);
			base.graphic.SetVerticesDirty();
		}

		public override void ModifyMesh(VertexHelper vh)
		{
			if (!IsActive())
				return;

			vh.GetUIVertexStream(m_VetexList);

			this._ProccessVertices_Job();

			vh.Clear();
			vh.AddUIVertexTriangleStream(m_VetexList);
		}

		public struct MyJob : IJobParallelFor
		{
			public int OutlineWidth;

			public NativeArray<UIVertex> v1Array;
			public NativeArray<UIVertex> v2Array;
			public NativeArray<UIVertex> v3Array;

			public void Execute(int i)
			{
				var v1 = v1Array[i];
				var v2 = v2Array[i];
				var v3 = v3Array[i];

				// 计算原顶点坐标中心点
				//
				var minX = _Min(v1.position.x, v2.position.x, v3.position.x);
				var minY = _Min(v1.position.y, v2.position.y, v3.position.y);
				var maxX = _Max(v1.position.x, v2.position.x, v3.position.x);
				var maxY = _Max(v1.position.y, v2.position.y, v3.position.y);
				var posCenter = new Vector2(minX + maxX, minY + maxY) * 0.5f;
				// 计算原始顶点坐标和UV的方向
				//
				Vector2 triX, triY, uvX, uvY;
				Vector2 pos1 = v1.position;
				Vector2 pos2 = v2.position;
				Vector2 pos3 = v3.position;
				if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
					> Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
				{
					triX = pos2 - pos1;
					triY = pos3 - pos2;
					uvX = v2.uv0 - v1.uv0;
					uvY = v3.uv0 - v2.uv0;
				}
				else
				{
					triX = pos3 - pos2;
					triY = pos2 - pos1;
					uvX = v3.uv0 - v2.uv0;
					uvY = v2.uv0 - v1.uv0;
				}
				// 计算原始UV框
				//
				var uvMin = _Min(v1.uv0, v2.uv0, v3.uv0);
				var uvMax = _Max(v1.uv0, v2.uv0, v3.uv0);
				var uvOrigin = new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y);
				// 为每个顶点设置新的Position和UV，并传入原始UV框
				//
				v1 = _SetNewPosAndUV(v1, OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
				v2 = _SetNewPosAndUV(v2, OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
				v3 = _SetNewPosAndUV(v3, OutlineWidth, posCenter, triX, triY, uvX, uvY, uvOrigin);
				// 应用设置后的UIVertex
				//
				v1Array[i] = v1;
				v2Array[i] = v2;
				v3Array[i] = v3;
			}
		}

		private void _ProccessVertices_Job()
		{
			int triangleCount = m_VetexList.Count / 3;
			NativeArray<UIVertex> v1Array = new NativeArray<UIVertex>(triangleCount, Allocator.TempJob);
			NativeArray<UIVertex> v2Array = new NativeArray<UIVertex>(triangleCount, Allocator.TempJob);
			NativeArray<UIVertex> v3Array = new NativeArray<UIVertex>(triangleCount, Allocator.TempJob);

			for (int i = 0, vIndex = 0; i <= m_VetexList.Count - 3; i += 3)
			{
				v1Array[vIndex] = m_VetexList[i];
				v2Array[vIndex] = m_VetexList[i + 1];
				v3Array[vIndex] = m_VetexList[i + 2];
				vIndex++;
			}

			MyJob jobData = new MyJob();
			jobData.v1Array = v1Array;
			jobData.v2Array = v2Array;
			jobData.v3Array = v3Array;
			jobData.OutlineWidth = OutlineWidth;

			JobHandle handle = jobData.Schedule(triangleCount, 32);
			handle.Complete();

			for (int i = 0, vIndex = 0; i <= m_VetexList.Count - 3; i += 3)
			{
				m_VetexList[i] = v1Array[vIndex];
				m_VetexList[i + 1] = v2Array[vIndex];
				m_VetexList[i + 2] = v3Array[vIndex];
				vIndex++;
			}

			v1Array.Dispose();
			v2Array.Dispose();
			v3Array.Dispose();
		}

		private static UIVertex _SetNewPosAndUV(
			UIVertex pVertex,
			int pOutLineWidth,
			Vector2 pPosCenter,
			Vector2 pTriangleX, Vector2 pTriangleY,
			Vector2 pUVX, Vector2 pUVY,
			Vector4 pUVOrigin)
		{
			// Position
			var pos = pVertex.position;
			var posXOffset = pos.x > pPosCenter.x ? pOutLineWidth : -pOutLineWidth;
			var posYOffset = pos.y > pPosCenter.y ? pOutLineWidth : -pOutLineWidth;
			pos.x += posXOffset;
			pos.y += posYOffset;
			pVertex.position = pos;
			// UV (縮小回原來大小)
			var uv = pVertex.uv0;
			uv += pUVX / pTriangleX.magnitude * posXOffset * (Vector2.Dot(pTriangleX, vRight) > 0 ? 1 : -1);
			uv += pUVY / pTriangleY.magnitude * posYOffset * (Vector2.Dot(pTriangleY, vUp) > 0 ? 1 : -1);
			pVertex.uv0 = uv;
			// 原始UV框
			pVertex.uv1.x = pUVOrigin.x;
			pVertex.uv1.y = pUVOrigin.y;
			pVertex.uv2.x = pUVOrigin.z;
			pVertex.uv2.y = pUVOrigin.w;

			return pVertex;
		}


		private static float _Min(float pA, float pB, float pC)
		{
			//會做hen多次 為了效能
			if (pA <= pB && pA <= pC)
			{
				return pA;
			}

			if (pB <= pA && pB <= pC)
			{
				return pB;
			}

			return pC;

			//return Mathf.Min(Mathf.Min(pA, pB), pC);
		}


		private static float _Max(float pA, float pB, float pC)
		{
			//會做hen多次 為了效能
			if (pA >= pB && pA >= pC)
			{
				return pA;
			}

			if (pB >= pA && pB >= pC)
			{
				return pB;
			}

			return pC;

			//return Mathf.Max(Mathf.Max(pA, pB), pC);
		}


		private static Vector2 _Min(Vector2 pA, Vector2 pB, Vector2 pC)
		{
			return new Vector2(_Min(pA.x, pB.x, pC.x), _Min(pA.y, pB.y, pC.y));
		}


		private static Vector2 _Max(Vector2 pA, Vector2 pB, Vector2 pC)
		{
			return new Vector2(_Max(pA.x, pB.x, pC.x), _Max(pA.y, pB.y, pC.y));
		}
	}
}