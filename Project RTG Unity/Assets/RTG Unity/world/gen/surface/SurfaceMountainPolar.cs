﻿namespace rtg.world.gen.surface
{
    using generic.pixel;
    using generic.world.biome;
    using generic.world.chunk;

    using rtg.api.world;
    using rtg.api.config;

    public class SurfaceMountainPolar : SurfaceBase
    {

        private float min;

        public SurfaceMountainPolar(BiomeConfig config, Pixel top, Pixel fill, float minCliff) : base(config, top, fill)
        {
            min = minCliff;
        }

        override public void paintTerrain(Chunk primer, int i, int j, int x, int z, int depth, RTGWorld rtgWorld, float[] noise, float river, Biome[] _base)
        {

        }
    }
}