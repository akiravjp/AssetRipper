﻿using AssetRipper.Core.Classes.Mesh;
using AssetRipper.Core.Extensions;
using AssetRipper.Numerics;
using AssetRipper.SourceGenerated.Subclasses.CompressedMesh;
using AssetRipper.SourceGenerated.Subclasses.PackedBitVector_Single;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AssetRipper.Core.SourceGenExtensions
{
	public static class CompressedMeshExtensions
	{
		public static bool IsSet(this ICompressedMesh compressedMesh) => compressedMesh.Vertices.NumItems > 0;

		public static void DecompressCompressedMesh(this ICompressedMesh compressedMesh,
			UnityVersion version,
			out Vector3[]? vertices,
			out Vector3[]? normals,
			out Vector4[]? tangents,
			out ColorFloat[]? colors,
			out BoneWeight4[]? skin,
			out Vector2[]? uv0,
			out Vector2[]? uv1,
			out Vector2[]? uv2,
			out Vector2[]? uv3,
			out Vector2[]? uv4,
			out Vector2[]? uv5,
			out Vector2[]? uv6,
			out Vector2[]? uv7,
			out Matrix4x4[]? bindPose,
			out uint[]? processedIndexBuffer)
		{
			vertices = default;
			normals = default;
			tangents = default;
			colors = default;
			skin = default;
			bindPose = default;
			processedIndexBuffer = default;

			//Vertex
			if (compressedMesh.Vertices.NumItems > 0)
			{
				vertices = GetVertices(compressedMesh);
			}
			//UV
			GetUV(compressedMesh, out uv0, out uv1, out uv2, out uv3, out uv4, out uv5, out uv6, out uv7);
			//BindPose
			if (compressedMesh.Has_BindPoses() && compressedMesh.BindPoses.NumItems > 0)
			{
				bindPose = GetBindPoses(compressedMesh);
			}
			//Normal
			if (compressedMesh.Normals.NumItems > 0)
			{
				normals = GetNormals(compressedMesh);
			}
			//Tangent
			if (compressedMesh.Tangents.NumItems > 0)
			{
				tangents = GetTangents(compressedMesh);
			}
			//FloatColor / Color
			if ((compressedMesh.Has_FloatColors() && compressedMesh.FloatColors.NumItems > 0)
				|| (compressedMesh.Has_Colors() && compressedMesh.Colors.NumItems > 0))
			{
				colors = GetFloatColors(compressedMesh);
			}
			//Skin
			if (compressedMesh.Weights.NumItems > 0)
			{
				skin = GetWeights(compressedMesh);
			}
			//IndexBuffer
			if (compressedMesh.Triangles.NumItems > 0)
			{
				processedIndexBuffer = GetTriangles(compressedMesh);
			}
		}

		private static int GetVertexCount(ICompressedMesh compressedMesh)
		{
			return (int)compressedMesh.Vertices.NumItems / 3;//3 floats in a Vector3
		}

		public static void GetUV(this ICompressedMesh compressedMesh, out Vector2[]? uv0, out Vector2[]? uv1, out Vector2[]? uv2, out Vector2[]? uv3, out Vector2[]? uv4, out Vector2[]? uv5, out Vector2[]? uv6, out Vector2[]? uv7)
		{
			int vertexCount = GetVertexCount(compressedMesh);
			if (compressedMesh.UV.NumItems > 0)
			{
				uint m_UVInfo = compressedMesh.UVInfo;
				if (compressedMesh.Has_UVInfo() && m_UVInfo != 0)
				{
					int uvSrcOffset = 0;
					uv0 = ReadChannel(compressedMesh.UV, m_UVInfo, 0, vertexCount, ref uvSrcOffset);
					uv1 = ReadChannel(compressedMesh.UV, m_UVInfo, 1, vertexCount, ref uvSrcOffset);
					uv2 = ReadChannel(compressedMesh.UV, m_UVInfo, 2, vertexCount, ref uvSrcOffset);
					uv3 = ReadChannel(compressedMesh.UV, m_UVInfo, 3, vertexCount, ref uvSrcOffset);
					uv4 = ReadChannel(compressedMesh.UV, m_UVInfo, 4, vertexCount, ref uvSrcOffset);
					uv5 = ReadChannel(compressedMesh.UV, m_UVInfo, 5, vertexCount, ref uvSrcOffset);
					uv6 = ReadChannel(compressedMesh.UV, m_UVInfo, 6, vertexCount, ref uvSrcOffset);
					uv7 = ReadChannel(compressedMesh.UV, m_UVInfo, 7, vertexCount, ref uvSrcOffset);
				}
				else
				{
					uv0 = MeshHelper.FloatArrayToVector2(compressedMesh.UV.UnpackFloats(2, 2 * sizeof(float), 0, vertexCount));
					if (compressedMesh.UV.NumItems >= vertexCount * sizeof(float))
					{
						uv1 = MeshHelper.FloatArrayToVector2(compressedMesh.UV.UnpackFloats(2, 2 * sizeof(float), vertexCount * 2, vertexCount));
					}
					else
					{
						uv1 = default;
					}
					uv2 = default;
					uv3 = default;
					uv4 = default;
					uv5 = default;
					uv6 = default;
					uv7 = default;
				}
			}
			else
			{
				uv0 = default;
				uv1 = default;
				uv2 = default;
				uv3 = default;
				uv4 = default;
				uv5 = default;
				uv6 = default;
				uv7 = default;
			}
		}

		private static Vector2[]? ReadChannel(PackedBitVector_Single packedVector, uint uvInfo, int channelIndex, int vertexCount, ref int currentOffset)
		{
			GetChannelInfo(uvInfo, channelIndex, out bool exists, out int uvDim);
			if (exists)
			{
				Vector2[] m_UV = MeshHelper.FloatArrayToVector2(packedVector.UnpackFloats(uvDim, uvDim * sizeof(float), currentOffset, vertexCount));
				currentOffset += uvDim * vertexCount;
				return m_UV;
			}
			else
			{
				return null;
			}
		}

		private static void GetChannelInfo(uint uvInfo, int index, out bool exists, out int dimension)
		{
			const int kInfoBitsPerUV = 4;
			const int kUVDimensionMask = 3;
			const int kUVChannelExists = 4;
			const uint uvChannelMask = (1u << kInfoBitsPerUV) - 1u;
			const int kMaxTexCoordShaderChannels = 8;

			if (index < 0 || index >= kMaxTexCoordShaderChannels)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			int bitOffset = index * kInfoBitsPerUV;
			uint texCoordBits = (uvInfo >> bitOffset) & uvChannelMask;
			exists = (texCoordBits & kUVChannelExists) != 0;
			dimension = 1 + (int)(texCoordBits & kUVDimensionMask);
		}

		private static uint SetChannelInfo(uint uvInfo, int index, bool exists, int dimension)
		{
			const int kInfoBitsPerUV = 4;
			const int kUVDimensionMask = 3;
			const int kUVChannelExists = 4;
			const uint uvChannelMask = (1u << kInfoBitsPerUV) - 1u;
			const int kMaxTexCoordShaderChannels = 8;

			if (index < 0 || index >= kMaxTexCoordShaderChannels)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (dimension < 1 || dimension > 1 + kUVDimensionMask)
			{
				throw new ArgumentOutOfRangeException(nameof(dimension));
			}

			int bitOffset = index * kInfoBitsPerUV;
			uint texCoordBits = (exists ? kUVChannelExists : 0u) | (uint)(dimension - 1);
			return (uvInfo & ~(uvChannelMask << bitOffset)) | (texCoordBits << bitOffset);
		}

		public static void SetUV(this ICompressedMesh compressedMesh, Vector2[]? uv0, Vector2[]? uv1, Vector2[]? uv2, Vector2[]? uv3, Vector2[]? uv4, Vector2[]? uv5, Vector2[]? uv6, Vector2[]? uv7)
		{
			if (!compressedMesh.Has_UVInfo() || (uv2.IsNullOrEmpty() && uv3.IsNullOrEmpty() && uv4.IsNullOrEmpty() && uv5.IsNullOrEmpty() && uv6.IsNullOrEmpty() && uv7.IsNullOrEmpty()))
			{
				compressedMesh.UVInfo = 0;
				if (uv0.IsNullOrEmpty())
				{
					compressedMesh.UV.PackFloats(Array.Empty<float>());
				}
				else if (uv1.IsNullOrEmpty())
				{
					compressedMesh.UV.Pack<Vector2>(uv0);
				}
				else if (uv0.Length != uv1.Length)
				{
					throw new ArgumentException("UV arrays must be the same length.");
				}
				else
				{
					int length = uv0.Length + uv1.Length;
					Vector2[] concatenated = ArrayPool<Vector2>.Shared.Rent(length);
					Array.Copy(uv0, 0, concatenated, 0, uv0.Length);
					Array.Copy(uv1, 0, concatenated, uv0.Length, uv1.Length);
					compressedMesh.UV.Pack(new ReadOnlySpan<Vector2>(concatenated, 0, length));
					ArrayPool<Vector2>.Shared.Return(concatenated);
				}
			}
			else
			{
				int totalLength = GetLength(uv0) + GetLength(uv1) + GetLength(uv2) + GetLength(uv3) + GetLength(uv4) + GetLength(uv5) + GetLength(uv6) + GetLength(uv7);
				Vector2[] buffer = ArrayPool<Vector2>.Shared.Rent(totalLength);
				int currentOffset = 0;
				uint uvInfo = default;
				UpdateBuffer(uv0, 0, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv1, 1, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv2, 2, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv3, 3, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv4, 4, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv5, 5, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv6, 6, buffer, ref currentOffset, ref uvInfo);
				UpdateBuffer(uv7, 7, buffer, ref currentOffset, ref uvInfo);
				compressedMesh.UV.Pack(new ReadOnlySpan<Vector2>(buffer, 0, totalLength));
				compressedMesh.UVInfo = uvInfo;
				ArrayPool<Vector2>.Shared.Return(buffer);
			}

			static int GetLength(Vector2[]? array) => array?.Length ?? 0;

			static void UpdateBuffer(Vector2[]? uv, int uvIndex, Vector2[] buffer, ref int currentOffset, ref uint uvInfo)
			{
				if (!uv.IsNullOrEmpty())
				{
					uvInfo = SetChannelInfo(uvInfo, uvIndex, true, 2);
					Array.Copy(uv, 0, buffer, currentOffset, uv.Length);
					currentOffset += uv.Length;
				}
			}
		}

		public static BoneWeight4[] GetWeights(this ICompressedMesh compressedMesh)
		{
			int[] weights = compressedMesh.Weights.UnpackInts();
			int[] boneIndices = compressedMesh.BoneIndices.UnpackInts();

			BoneWeight4[] skin = new BoneWeight4[compressedMesh.Weights.NumItems];

			int bonePos = 0;
			int boneIndexPos = 0;
			int j = 0;
			int sum = 0;

			for (int i = 0; i < compressedMesh.Weights.NumItems; i++)
			{
				//read bone index and weight.
				{
					BoneWeight4 boneWeight = skin[bonePos];
					boneWeight.SetWeight(j, weights[i] / 31f);
					boneWeight.SetIndex(j, boneIndices[boneIndexPos++]);
					skin[bonePos] = boneWeight;
				}
				j++;
				sum += weights[i];

				//the weights add up to one. fill the rest for this vertex with zero, and continue with next one.
				if (sum >= 31)
				{
					for (; j < 4; j++)
					{
						BoneWeight4 boneWeight = skin[bonePos];
						boneWeight.SetWeight(j, 0);
						boneWeight.SetIndex(j, 0);
						skin[bonePos] = boneWeight;
					}
					bonePos++;
					j = 0;
					sum = 0;
				}
				//we read three weights, but they don't add up to one. calculate the fourth one, and read
				//missing bone index. continue with next vertex.
				else if (j == 3)
				{
					BoneWeight4 boneWeight = skin[bonePos];
					boneWeight.SetWeight(j, (31 - sum) / 31f);
					boneWeight.SetIndex(j, boneIndices[boneIndexPos++]);
					skin[bonePos] = boneWeight;
					bonePos++;
					j = 0;
					sum = 0;
				}
			}

			return skin;
		}

		public static void SetWeights(this ICompressedMesh compressedMesh, ReadOnlySpan<BoneWeight4> weights)
		{
			if (weights.Length > 0)
			{
				throw new NotImplementedException();
			}
			else
			{
				compressedMesh.Weights.Reset();
			}
		}

		public static Vector3[] GetNormals(this ICompressedMesh compressedMesh)
		{
			float[] normalData = compressedMesh.Normals.UnpackFloats(2, 2 * sizeof(float));
			int[] signs = compressedMesh.NormalSigns.UnpackInts();
			Vector3[] normals = new Vector3[compressedMesh.Normals.NumItems / 2];
			for (int i = 0; i < compressedMesh.Normals.NumItems / 2; ++i)
			{
				float x = normalData[(i * 2) + 0];
				float y = normalData[(i * 2) + 1];
				float zsqr = 1 - (x * x) - (y * y);
				float z;
				if (zsqr >= 0)
				{
					z = (float)System.Math.Sqrt(zsqr);
				}
				else
				{
					z = 0;
					Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));
					x = normal.X;
					y = normal.Y;
					z = normal.Z;
				}
				if (signs[i] == 0)
				{
					z = -z;
				}

				normals[i] = new Vector3(x, y, z);
			}

			return normals;
		}

		public static void SetNormals(this ICompressedMesh compressedMesh, ReadOnlySpan<Vector3> normals)
		{
			MakeFloatAndSignArrays(normals, out float[] floats, out uint[] signs);
			compressedMesh.Normals.PackFloats(floats);
			compressedMesh.NormalSigns.PackUInts(signs);
		}

		private static void MakeFloatAndSignArrays(ReadOnlySpan<Vector3> normals, out float[] floats, out uint[] signs)
		{
			floats = new float[normals.Length * 2];
			signs = new uint[normals.Length];
			for (int i = 0; i < normals.Length; i++)
			{
				//Normals should already be normalized, but it's better to be safe.
				Vector3 vector = Vector3.Normalize(normals[i]);
				floats[2 * i] = vector.X;
				floats[2 * i + 1] = vector.Y;
				signs[i] = vector.Z < 0 ? 0u : 1u;
			}
		}

		public static Vector4[] GetTangents(this ICompressedMesh compressedMesh)
		{
			float[] tangentData = compressedMesh.Tangents.UnpackFloats(2, 2 * sizeof(float));
			int[] signs = compressedMesh.TangentSigns.UnpackInts();
			Vector4[] tangents = new Vector4[compressedMesh.Tangents.NumItems / 2];
			for (int i = 0; i < compressedMesh.Tangents.NumItems / 2; ++i)
			{
				float x = tangentData[(i * 2) + 0];
				float y = tangentData[(i * 2) + 1];
				float zsqr = 1 - (x * x) - (y * y);
				float z;
				if (zsqr >= 0f)
				{
					z = (float)System.Math.Sqrt(zsqr);
				}
				else
				{
					z = 0;
					Vector3 tangent = Vector3.Normalize(new Vector3(x, y, z));
					x = tangent.X;
					y = tangent.Y;
					z = tangent.Z;
				}
				if (signs[(i * 2) + 0] == 0)
				{
					z = -z;
				}

				float w = signs[(i * 2) + 1] == 0 ? -1.0f : 1.0f;
				tangents[i] = new Vector4(x, y, z, w);
			}

			return tangents;
		}

		public static void SetTangents(this ICompressedMesh compressedMesh, ReadOnlySpan<Vector4> tangents)
		{
			MakeFloatAndSignArrays(tangents, out float[] floats, out uint[] signs);
			compressedMesh.Tangents.PackFloats(floats);
			compressedMesh.TangentSigns.PackUInts(signs);
		}

		private static void MakeFloatAndSignArrays(ReadOnlySpan<Vector4> tangents, out float[] floats, out uint[] signs)
		{
			floats = new float[tangents.Length * 2];
			signs = new uint[tangents.Length * 2];
			for (int i = 0; i < tangents.Length; i++)
			{
				//Tangents should already be normalized, but it's better to be safe.
				Vector3 vector = Vector3.Normalize(tangents[i].AsVector3());
				floats[2 * i] = vector.X;
				floats[2 * i + 1] = vector.Y;
				signs[2 * i] = vector.Z < 0 ? 0u : 1u;
				signs[2 * i + 1] = tangents[i].W < 0 ? 0u : 1u;
			}
		}

		/// <summary>
		/// Only available before Unity 5
		/// </summary>
		/// <param name="compressedMesh"></param>
		/// <returns></returns>
		public static Matrix4x4[] GetBindPoses(this ICompressedMesh compressedMesh)
		{
			if (compressedMesh.Has_BindPoses())
			{
				const int MatrixFloats = 16;
				Matrix4x4[] bindPose = new Matrix4x4[compressedMesh.BindPoses.NumItems / MatrixFloats];
				float[] m_BindPoses_Unpacked = compressedMesh.BindPoses.UnpackFloats(MatrixFloats, MatrixFloats * sizeof(float));
				MemoryMarshal.Cast<float, Matrix4x4>(m_BindPoses_Unpacked).CopyTo(bindPose);
				return bindPose;
			}
			else
			{
				return Array.Empty<Matrix4x4>(); 
			}
		}

		/// <summary>
		/// Only available before Unity 5
		/// </summary>
		/// <param name="compressedMesh"></param>
		/// <param name="bindPoses"></param>
		public static void SetBindPoses(this ICompressedMesh compressedMesh, ReadOnlySpan<Matrix4x4> bindPoses)
		{
			compressedMesh.BindPoses?.PackFloats(MemoryMarshal.Cast<Matrix4x4, float>(bindPoses));
		}

		public static Vector3[] GetVertices(this ICompressedMesh compressedMesh)
		{
			float[] verticesData = compressedMesh.Vertices.UnpackFloats(3, 3 * sizeof(float));
			return MeshHelper.FloatArrayToVector3(verticesData);
		}

		public static void SetVertices(this ICompressedMesh compressedMesh, ReadOnlySpan<Vector3> vertices)
		{
			compressedMesh.Vertices.PackFloats(MemoryMarshal.Cast<Vector3, float>(vertices));
		}

		public static ColorFloat[] GetFloatColors(this ICompressedMesh compressedMesh)
		{
			if (compressedMesh.Has_FloatColors())
			{
				return MeshHelper.FloatArrayToColorFloat(compressedMesh.FloatColors.UnpackFloats(1, 4));
			}
			else if (compressedMesh.Has_Colors())
			{
				compressedMesh.Colors.NumItems *= 4;
				compressedMesh.Colors.BitSize /= 4;
				int[] tempColors = compressedMesh.Colors.UnpackInts();
				ColorFloat[] colors = new ColorFloat[compressedMesh.Colors.NumItems / 4];
				for (int v = 0; v < compressedMesh.Colors.NumItems / 4; v++)
				{
					colors[v] = (ColorFloat)new Color32((byte)tempColors[4 * v], (byte)tempColors[(4 * v) + 1], (byte)tempColors[(4 * v) + 2], (byte)tempColors[(4 * v) + 3]);
				}
				compressedMesh.Colors.NumItems /= 4;
				compressedMesh.Colors.BitSize *= 4;
				return colors;
			}
			else
			{
				return Array.Empty<ColorFloat>();
			}
		}

		public static void SetFloatColors(this ICompressedMesh compressedMesh, ReadOnlySpan<ColorFloat> colors)
		{
			if (compressedMesh.Has_FloatColors())
			{
				compressedMesh.FloatColors.Pack(colors);
			}
			else if (compressedMesh.Has_Colors())
			{
				Color32[] buffer = ArrayPool<Color32>.Shared.Rent(colors.Length);
				for (int i = 0; i < colors.Length; i++)
				{
					buffer[i] = (Color32)colors[i];
				}
				compressedMesh.Colors.PackUInts(MemoryMarshal.Cast<Color32, uint>(new ReadOnlySpan<Color32>(buffer, 0, colors.Length)));
				ArrayPool<Color32>.Shared.Return(buffer);
			}
		}

		public static uint[] GetTriangles(this ICompressedMesh compressedMesh)
		{
			return compressedMesh.Triangles.UnpackUInts();
		}

		public static void SetTriangles(this ICompressedMesh compressedMesh, ReadOnlySpan<uint> triangles)
		{
			compressedMesh.Triangles.PackUInts(triangles);
		}
	}
}
