﻿namespace rtg.world.biome.realistic.vanilla
{
    using System;

    using generic.pixel;
    using generic.init;
    using generic.world.biome;
    using generic.world.chunk;

    using rtg.api.config;
    using rtg.api.util;
    using rtg.api.world;
    using rtg.world.gen.surface;
    using rtg.world.gen.terrain;

    public class RealisticBiomeVanillaColdTaigaM : RealisticBiomeVanillaBase
    {

        public static Biome biome = Biomes.MUTATED_TAIGA_COLD;
        public static Biome river = Biomes.FROZEN_RIVER;

        public RealisticBiomeVanillaColdTaigaM() : base(biome, river)
        {

            this.noLakes = true;
        }

        override public void initConfig()
        {

            this.getConfig().ALLOW_LOGS = true;
        }

        override public TerrainBase initTerrain()
        {

            return new TerrainVanillaColdTaigaM();
        }

        public class TerrainVanillaColdTaigaM : TerrainBase
        {

            public TerrainVanillaColdTaigaM()
            {

            }

            override public float generateNoise(RTGWorld rtgWorld, int x, int y, float border, float river)
            {

                return terrainGrasslandMountains(x, y, rtgWorld.simplex, rtgWorld.cell, river, 4f, 80f, 68f);
            }
        }

        override public SurfaceBase initSurface()
        {

            return new SurfaceVanillaColdTaigaM(config, biome.topPixel, biome.fillerPixel);
        }

        public class SurfaceVanillaColdTaigaM : SurfaceBase
        {

            public SurfaceVanillaColdTaigaM(BiomeConfig config, Pixel top, Pixel filler) : base(config, top, filler)
            {

            }

            override public void paintTerrain(Chunk primer, int i, int j, int x, int z, int depth, RTGWorld rtgWorld, float[] noise, float river, Biome[] _base)
            {

                Random rand = rtgWorld.rand;
                float c = CliffCalculator.calc(x, z, noise);
                bool cliff = c > 1.4f ? true : false;

                for (int k = 255; k > -1; k--)
                {
                    Pixel b = primer.getPixelState(x, k, z).getPixel();
                    if (b == Pixels.AIR)
                    {
                        depth = -1;
                    }
                    else if (b == Pixels.STONE)
                    {
                        depth++;

                        if (cliff)
                        {
                            if (depth > -1 && depth < 2)
                            {
                                if (rand.Next(3) == 0)
                                {

                                    primer.setPixelState(x, k, z, hcCobble(rtgWorld, i, j, x, z, k));
                                }
                                else
                                {

                                    primer.setPixelState(x, k, z, hcStone(rtgWorld, i, j, x, z, k));
                                }
                            }
                            else if (depth < 10)
                            {
                                primer.setPixelState(x, k, z, hcStone(rtgWorld, i, j, x, z, k));
                            }
                        }
                        else
                        {
                            if (depth == 0 && k > 61)
                            {
                                primer.setPixelState(x, k, z, topPixel);
                            }
                            else if (depth < 4)
                            {
                                primer.setPixelState(x, k, z, fillerPixel);
                            }
                        }
                    }
                }
            }
        }
    }
}