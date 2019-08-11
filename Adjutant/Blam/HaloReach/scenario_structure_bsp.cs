﻿using Adjutant.Blam.Common;
using Adjutant.Geometry;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.HaloReach
{
    public class scenario_structure_bsp : IRenderGeometry
    {
        private readonly CacheFile cache;
        private readonly IndexItem item;

        private bool loadedInstances;

        public scenario_structure_bsp(CacheFile cache, IndexItem item)
        {
            this.cache = cache;
            this.item = item;
        }

        [Offset(236)]
        public RealBounds XBounds { get; set; }

        [Offset(244)]
        public RealBounds YBounds { get; set; }

        [Offset(252)]
        public RealBounds ZBounds { get; set; }

        [Offset(308)]
        public BlockCollection<ClusterBlock> Clusters { get; set; }

        [Offset(320)]
        public BlockCollection<ShaderBlock> Shaders { get; set; }

        [Offset(620, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(608, MinVersion = (int)CacheType.HaloReachRetail)]
        public BlockCollection<BspGeometryInstanceBlock> GeometryInstances { get; set; }

        [Offset(796)]
        public ResourceIdentifier ResourcePointer1 { get; set; }

        [Offset(976)]
        public ResourceIdentifier ResourcePointer2 { get; set; }

        [Offset(1112, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(1100, MinVersion = (int)CacheType.HaloReachRetail)]
        public BlockCollection<SectionBlock> Sections { get; set; }

        [Offset(1124, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(1112, MinVersion = (int)CacheType.HaloReachRetail)]
        public BlockCollection<BoundingBoxBlock> BoundingBoxes { get; set; }

        [Offset(1244)]
        public ResourceIdentifier ResourcePointer3 { get; set; }

        [Offset(1296)]
        [VersionSpecific((int)CacheType.HaloReachRetail)]
        public ResourceIdentifier InstancesResourcePointer { get; set; }

        #region IRenderGeometry

        int IRenderGeometry.LodCount => 1;

        public IGeometryModel ReadGeometry(int lod)
        {
            if (lod < 0 || lod >= ((IRenderGeometry)this).LodCount)
                throw new ArgumentOutOfRangeException(nameof(lod));

            var model = new GeometryModel(item.FileName) { CoordinateSystem = CoordinateSystem.Default };

            var bspBlock = cache.Scenario.StructureBsps.First(s => s.BspReference.TagId == item.Id);
            var bspIndex = cache.Scenario.StructureBsps.IndexOf(bspBlock);

            var lightmap = cache.Scenario.ScenarioLightmapReference.Tag.ReadMetadata<scenario_lightmap>();
            var lightmapData = lightmap.LightmapRefs[bspIndex].LightmapDataReference.Tag.ReadMetadata<scenario_lightmap_bsp_data>();

            model.Bounds.AddRange(BoundingBoxes);
            model.Materials.AddRange(HaloReachCommon.GetMaterials(Shaders));

            var clusterRegion = new GeometryRegion { Name = "Clusters" };
            clusterRegion.Permutations.AddRange(
                Clusters.Select(c => new GeometryPermutation
                {
                    Name = Clusters.IndexOf(c).ToString("D3", CultureInfo.CurrentCulture),
                    MeshIndex = c.SectionIndex,
                    MeshCount = 1
                })
            );
            model.Regions.Add(clusterRegion);

            if (cache.CacheType == CacheType.HaloReachRetail && !loadedInstances)
            {
                var entry = cache.ResourceGestalt.ResourceEntries[InstancesResourcePointer.ResourceIndex];
                var address = entry.FixupOffset + entry.ResourceFixups[entry.ResourceFixups.Count - 10].Offset & 0x0FFFFFFF;

                using (var cacheReader = cache.CreateReader(cache.MetadataTranslator))
                using (var reader = cacheReader.CreateVirtualReader(cache.ResourceGestalt.FixupDataPointer.Address))
                {
                    for (int i = 0; i < GeometryInstances.Count; i++)
                    {
                        reader.Seek(address + 156 * i, SeekOrigin.Begin);
                        GeometryInstances[i].TransformScale = reader.ReadSingle();
                        GeometryInstances[i].Transform = reader.ReadObject<Matrix4x4>();
                        reader.Seek(6, SeekOrigin.Current);
                        GeometryInstances[i].SectionIndex = reader.ReadInt16();
                    }
                }

                loadedInstances = true;
            }

            foreach (var instanceGroup in GeometryInstances.GroupBy(i => i.SectionIndex))
            {
                var section = lightmapData.Sections[instanceGroup.Key];
                var sectionRegion = new GeometryRegion { Name = Utils.CurrentCulture($"Instances {instanceGroup.Key:D3}") };
                sectionRegion.Permutations.AddRange(
                    instanceGroup.Select(i => new GeometryPermutation
                    {
                        Name = i.Name,
                        Transform = i.Transform,
                        TransformScale = i.TransformScale,
                        MeshIndex = i.SectionIndex,
                        MeshCount = 1
                    })
                );
                model.Regions.Add(sectionRegion);
            }

            model.Meshes.AddRange(HaloReachCommon.GetMeshes(cache, lightmapData.ResourcePointer, lightmapData.Sections, (s) =>
            {
                var index = (short)lightmapData.Sections.IndexOf(s);
                return index >= BoundingBoxes.Count ? (short?)null : index;
            }));

            return model;
        }

        #endregion

    }

    [FixedSize(288, MaxVersion = (int)CacheType.HaloReachRetail)]
    [FixedSize(140, MinVersion = (int)CacheType.HaloReachRetail)]
    public class ClusterBlock
    {
        [Offset(0)]
        public RealBounds XBounds { get; set; }

        [Offset(8)]
        public RealBounds YBounds { get; set; }

        [Offset(16)]
        public RealBounds ZBounds { get; set; }

        [Offset(208, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(64, MinVersion = (int)CacheType.HaloReachRetail)]
        public short SectionIndex { get; set; }
    }

    [FixedSize(168, MaxVersion = (int)CacheType.HaloReachRetail)]
    [FixedSize(4, MinVersion = (int)CacheType.HaloReachRetail)]
    public class BspGeometryInstanceBlock
    {
        [Offset(0)]
        [VersionSpecific((int)CacheType.HaloReachBeta)]
        public float TransformScale { get; set; }

        [Offset(4)]
        [VersionSpecific((int)CacheType.HaloReachBeta)]
        public Matrix4x4 Transform { get; set; }

        [Offset(52)]
        [VersionSpecific((int)CacheType.HaloReachBeta)]
        public short SectionIndex { get; set; }

        [Offset(124, MaxVersion = (int)CacheType.HaloReachRetail)]
        [Offset(0, MinVersion = (int)CacheType.HaloReachRetail)]
        public StringId Name { get; set; }

        public override string ToString() => Name;
    }
}
