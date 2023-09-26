﻿using Reclaimer.Geometry.Utilities;
using Reclaimer.IO;
using System.IO;
using System.Numerics;

namespace Reclaimer.Geometry
{
    public static class GeometryExtensions
    {
        public static Vector3 ToVector3(this IVector3 vector3) => new Vector3(vector3.X, vector3.Y, vector3.Z);
        public static Quaternion ToQuaternion(this IVector4 vector4) => new Quaternion(vector4.X, vector4.Y, vector4.Z, vector4.W);

        public static IEnumerable<int> Unstrip(this IEnumerable<int> strip)
        {
            var (position, i0, i1, i2) = (0, 0, 0, 0);

            foreach (var index in strip)
            {
                (i0, i1, i2) = (i1, i2, index);

                if (position++ < 2 || i0 == i1 || i0 == i2 || i1 == i2)
                    continue;

                yield return i0;

                if (position % 2 == 1)
                {
                    yield return i1;
                    yield return i2;
                }
                else
                {
                    yield return i2;
                    yield return i1;
                }
            }
        }

        public static IEnumerable<int> GetTriangleIndicies(this IMeshCompat mesh, ISubmeshCompat submesh)
        {
            var indices = mesh.IndexBuffer.GetSubset(submesh.IndexStart, submesh.IndexLength);

            if (mesh.IndexFormat == IndexFormat.TriangleStrip)
                indices = Unstrip(indices);

            return indices;
        }

        public static IEnumerable<Vector3> GetPositions(this IMeshCompat mesh) => GetPositions(mesh, 0, mesh.VertexCount);
        public static IEnumerable<Vector3> GetPositions(this IMeshCompat mesh, int index, int count)
        {
            return mesh.VertexBuffer.HasPositions ? mesh.VertexBuffer.PositionChannels[0].GetSubset(index, count).Select(v => new Vector3(v.X, v.Y, v.Z)) : null;
        }

        public static IEnumerable<Vector2> GetTexCoords(this IMeshCompat mesh) => GetTexCoords(mesh, 0, mesh.VertexCount);
        public static IEnumerable<Vector2> GetTexCoords(this IMeshCompat mesh, int index, int count)
        {
            return mesh.VertexBuffer.HasTextureCoordinates ? mesh.VertexBuffer.TextureCoordinateChannels[0].GetSubset(index, count).Select(v => new Vector2(v.X, v.Y)) : null;
        }

        public static IEnumerable<Vector3> GetNormals(this IMeshCompat mesh) => GetNormals(mesh, 0, mesh.VertexCount);
        public static IEnumerable<Vector3> GetNormals(this IMeshCompat mesh, int index, int count)
        {
            return mesh.VertexBuffer.HasNormals ? mesh.VertexBuffer.NormalChannels[0].GetSubset(index, count).Select(v => new Vector3(v.X, v.Y, v.Z)) : null;
        }

        public static IEnumerable<Vector4> GetBlendIndices(this IMeshCompat mesh) => GetBlendIndices(mesh, 0, mesh.VertexCount);
        public static IEnumerable<Vector4> GetBlendIndices(this IMeshCompat mesh, int index, int count)
        {
            return mesh.VertexBuffer.HasBlendIndices ? mesh.VertexBuffer.BlendIndexChannels[0].GetSubset(index, count).Select(v => new Vector4(v.X, v.Y, v.Z, v.W)) : null;
        }

        public static IEnumerable<Vector4> GetBlendWeights(this IMeshCompat mesh) => GetBlendWeights(mesh, 0, mesh.VertexCount);
        public static IEnumerable<Vector4> GetBlendWeights(this IMeshCompat mesh, int index, int count)
        {
            return mesh.VertexBuffer.HasBlendWeights ? mesh.VertexBuffer.BlendWeightChannels[0].GetSubset(index, count).Select(v => new Vector4(v.X, v.Y, v.Z, v.W)) : null;
        }

        public static void WriteRMF(this Scene scene, string fileName)
        {
            ArgumentNullException.ThrowIfNull(scene);
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            if (!fileName.EndsWith(".rmf", StringComparison.CurrentCultureIgnoreCase))
                fileName += ".rmf";

            Directory.GetParent(fileName).Create();

            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            using (var bw = new EndianWriter(fs, ByteOrder.LittleEndian))
            {
                var sceneWriter = new SceneWriter(bw);
                sceneWriter.Write(scene);
            }
        }
    }
}
