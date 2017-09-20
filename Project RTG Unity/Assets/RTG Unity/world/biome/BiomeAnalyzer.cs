﻿namespace rtg.world.biome
{
    using generic.init;
    using generic.world.biome;


    using rtg.api;
    using rtg.api.util;
    using rtg.world.biome.realistic;

    using System;

    /**
     * @author Zeno410, Modified by srs_bsns 20160914
     */
    public class BiomeAnalyzer
    {
        private bool[] riverBiome;
        private bool[] oceanBiome;
        private bool[] swampBiome;
        private bool[] beachBiome;
        private bool[] landBiome;
        private int[] preferredBeach;
        private RealisticBiomeBase[] savedJittered;
        private RealisticBiomeBase scenicLakeBiome = RealisticBiomeBase.getBiome(RTGAPI.config().SCENIC_LAKE_BIOME_ID);
        private RealisticBiomeBase scenicFrozenLakeBiome = RealisticBiomeBase.getBiome(RTGAPI.config().SCENIC_FROZEN_LAKE_BIOME_ID);
        private SmoothingSearchStatus beachSearch;
        private SmoothingSearchStatus landSearch;
        private SmoothingSearchStatus oceanSearch;
        private readonly static int NO_BIOME = -1;
        private RealisticBiomePatcher biomePatcher = new RealisticBiomePatcher();

        public BiomeAnalyzer()
        {
            determineRiverBiomes();
            determineOceanBiomes();
            determineSwampBiomes();
            determineBeachBiomes();
            determineLandBiomes();
            setupBeachesForBiomes();
            prepareSearchPattern();
            setSearches();
            savedJittered = new RealisticBiomeBase[256];
        }

        public static int[] xyinverted()
        {

            int[] result = new int[256];

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    result[i * 16 + j] = j * 16 + i;
                }
            }

            for (int i = 0; i < 256; i++)
            {
                if (result[result[i]] != i) throw new Exception("" + i + " " + result[i] + " " + result[result[i]]);
            }

            return result;
        }

        /**
         *
         * @author Zeno410, Modified by srs_bsns 20160914
         */
        private class SmoothingSearchStatus
        {
            internal bool absent = false;
            internal bool notHunted;
            private int size() { return 3; }
            private int[] findings = new int[3 * 3];
            // weightings is part of a system to generate some variability in repaired chunks weighting is
            // based on how long the search went on (so quasipsuedorandom, based on direction plus distance
            private float[] weightings = new float[3 * 3];
            public int[] biomes = new int[256];
            private bool[] desired;
            private int arraySize;
            private int[] pattern;
            private readonly int upperLeftFinding = 0;
            private readonly int upperRightFinding = 3;
            private readonly int lowerLeftFinding = 1;
            private readonly int lowerRightFinding = 4;
            private readonly int[] quadrantBiome = new int[4];
            private readonly float[] quadrantBiomeWeighting = new float[4];
            private int biomeCount;
            private int[] _xyinverted = xyinverted();

            internal SmoothingSearchStatus(bool[] desired) { this.desired = desired; }

            internal void hunt(int[] biomeNeighborhood)
            {
                // 0,0 in the chunk is 9,9 int the array ; 8,8 is 10,10 and is treated as the center
                clear();
                int oldArraySize = arraySize;
                arraySize = (int)Math.Sqrt(biomeNeighborhood.Length);
                if (arraySize * arraySize != biomeNeighborhood.Length)
                    throw new Exception("non-square array");
                if (arraySize != oldArraySize)
                    pattern = new CircularSearchCreator().pattern(arraySize / 2 - 1, arraySize);
                for (int xOffset = -1; xOffset <= 1; xOffset++)
                    for (int zOffset = -1; zOffset <= 1; zOffset++)
                        search(xOffset, zOffset, biomeNeighborhood);
                // calling a routine because it gets too indented otherwise
                smoothBiomes();
            }

            private void search(int xOffset, int zOffset, int[] biomeNeighborhood)
            {
                int offset = xOffset * arraySize + zOffset;
                int location = (xOffset + 1) * size() + zOffset + 1;
                // set to failed search, which sticks if nothing is found
                findings[location] = NO_BIOME;
                weightings[location] = 2f;
                for (int i = 0; i < pattern.Length; i++)
                {
                    int biome = biomeNeighborhood[pattern[i] + offset];
                    if (desired[biome])
                    {
                        findings[location] = biome;
                        weightings[location] = (float)Math.Sqrt(pattern.Length) - (float)Math.Sqrt(i) + 2f;
                        break;
                    }
                }
            }

            private void smoothBiomes()
            {
                // more sophisticated version offsets into findings and biomes upperleft
                smoothQuadrant(biomeIndex(0, 0), upperLeftFinding);
                smoothQuadrant(biomeIndex(8, 0), upperRightFinding);
                smoothQuadrant(biomeIndex(0, 8), lowerLeftFinding);
                smoothQuadrant(biomeIndex(8, 8), lowerRightFinding);
            }

            private void smoothQuadrant(int biomesOffset, int findingsOffset)
            {
                int upperLeft = findings[upperLeftFinding + findingsOffset];
                int upperRight = findings[upperRightFinding + findingsOffset];
                int lowerLeft = findings[lowerLeftFinding + findingsOffset];
                int lowerRight = findings[lowerRightFinding + findingsOffset];
                // check for uniformity
                if ((upperLeft == upperRight) && (upperLeft == lowerLeft) && (upperLeft == lowerRight))
                {
                    // everythings the same; uniform fill;
                    for (int x = 0; x < 8; x++)
                        for (int z = 0; z < 8; z++)
                            biomes[biomeIndex(x, z) + biomesOffset] = upperLeft;
                    return;
                }
                // not all the same; we have to work;
                biomeCount = 0;
                addBiome(upperLeft);
                addBiome(upperRight);
                addBiome(lowerLeft);
                addBiome(lowerRight);
                for (int x = 0; x < 8; x++)
                {
                    for (int z = 0; z < 8; z++)
                    {
                        addBiome(lowerRight);
                        for (int i = 0; i < 4; i++) quadrantBiomeWeighting[i] = 0;
                        // weighting strategy: weights go down as you move away from the corner.
                        // they go to 0 on the far edges so only the points on the edge have effects there
                        // for continuity with the next quadrant
                        addWeight(upperLeft, weightings[upperLeftFinding + findingsOffset] * (7 - x) * (7 - z));
                        addWeight(upperRight, weightings[upperRightFinding + findingsOffset] * x * (7 - z));
                        addWeight(lowerLeft, weightings[lowerLeftFinding + findingsOffset] * (7 - x) * z);
                        addWeight(lowerRight, weightings[lowerRightFinding + findingsOffset] * x * z);
                        biomes[biomeIndex(x, z) + biomesOffset] = preferredBiome();
                    }
                }
            }

            private void addBiome(int biome)
            {
                for (int i = 0; i < biomeCount; i++)
                    if (biome == quadrantBiome[i]) return;
                // not there, add
                quadrantBiome[biomeCount++] = biome;
            }

            private void addWeight(int biome, float weight)
            {
                for (int i = 0; i < biomeCount; i++)
                {
                    if (biome == quadrantBiome[i])
                    {
                        quadrantBiomeWeighting[i] += weight;
                        return;
                    }
                }
            }

            private int preferredBiome()
            {
                float bestWeight = 0;
                int result = -2;
                for (int i = 0; i < biomeCount; i++)
                {
                    if (quadrantBiomeWeighting[i] > bestWeight)
                    {
                        bestWeight = quadrantBiomeWeighting[i];
                        result = quadrantBiome[i];
                    }
                }
                return result;
            }

            private int biomeIndex(int x, int z)
            {
                return x * 16 + z;
            }

            private void clear()
            {
                for (int i = 0; i < findings.Length; i++) findings[i] = -1;
            }

        }

        private void determineRiverBiomes()
        {
            riverBiome = new bool[256];
            for (int i = 0; i < riverBiome.Length; i++)
            {
                Biome biome = Biome.getBiome(i);
                if (biome == null) continue;
                if (biome == Biomes.RIVER) riverBiome[i] = true;
            }
        }

        private void determineOceanBiomes()
        {
            oceanBiome = new bool[256];
            for (int i = 0; i < oceanBiome.Length; i++)
            {
                Biome biome = Biome.getBiome(i);
                if (biome == null) continue;
                if (biome == Biomes.OCEAN || biome == Biomes.DEEP_OCEAN || biome == Biomes.FROZEN_OCEAN) oceanBiome[i] = true;
            }
            oceanBiome[Biomes.DEEP_OCEAN.getBiomeID()] = true;// not getting set?
        }

        private void determineSwampBiomes()
        {
            swampBiome = new bool[256];
            for (int i = 0; i < swampBiome.Length; i++)
            {
                Biome biome = Biome.getBiome(i);
                if (biome == null) continue;
                if (biome == Biomes.SWAMPLAND) swampBiome[i] = true;
                if (biome == Biomes.ICE_FLATS) swampBiome[i] = true;
                if (biome.getBiomeID() == Biomes.FROZEN_RIVER.getBiomeID()) swampBiome[i] = true;
            }
        }

        private void determineLandBiomes()
        {
            landBiome = new bool[256];
            for (int i = 0; i < landBiome.Length; i++)
            {
                if (!oceanBiome[i] && !riverBiome[i] && !beachBiome[i])
                {
                    Biome biome = Biome.getBiome(i);
                    if (biome == null) continue;
                    //if (!biome.getBiomeName().toLowerCase().equals("lake")) landBiome[i] = true;
                }
            }
        }

        private void determineBeachBiomes()
        {
            beachBiome = new bool[256];
            for (int i = 0; i < beachBiome.Length; i++)
            {
                Biome biome = Biome.getBiome(i);
                if (biome == null) continue;
                if (biome == Biomes.BEACHES ||
                        biome == Biomes.COLD_BEACH ||
                        biome == Biomes.STONE_BEACH) beachBiome[i] = true;
            }
        }

        private void setupBeachesForBiomes()
        {

            preferredBeach = new int[256];

            for (int i = 0; i < preferredBeach.Length; i++)
            {

                // We need to work with the realistic biome, so let's try to get it from the base biome, aborting if necessary.
                Biome biome = Biome.getBiome(i);
                if (biome == null) continue;
                RealisticBiomeBase realisticBiome = RealisticBiomeBase.getBiome(i);
                if (realisticBiome == null) continue;

                preferredBeach[i] = realisticBiome._beachBiome.getBiomeID();

                // If stone beaches aren't allowed in this biome, then determine the best beach to use based on the biome's temperature.
                if (realisticBiome.disallowStoneBeaches)
                {
                    if (realisticBiome._beachBiome.getBiomeID() == Biomes.STONE_BEACH.getBiomeID())
                    {
                        preferredBeach[i] = (biome.getTemperature() <= 0.05f) ? Biomes.COLD_BEACH.getBiomeID() : Biomes.BEACHES.getBiomeID();
                    }
                }

                // If beaches aren't allowed in this biome, then use this biome as the beach.
                if (realisticBiome.disallowAllBeaches)
                {
                    preferredBeach[i] = i;
                }
            }
        }

        public static Biome getPreferredBeachForBiome(Biome biome)
        {

            /*
             * Some of this code is from Climate Control, and it's still a bit crude. - Zeno
             * Some of this code is from Pink's brain, and it's also a bit crude. - Pink
             */

            float height = biome.getBaseHeight() + (biome.getHeightVariation() * 2f);
            float temp = biome.getTemperature();

            // Use a cold beach if the temperature is low enough; otherwise, just use a normal beach.
            Biome beach = (temp <= 0.05f) ? Biomes.COLD_BEACH : Biomes.BEACHES;

            // If this is a mountainous biome or a Taiga variant, then let's use a stone beach.
            if ((height > (1.0f + 0.5f) && temp > 0.05f) || isTaigaBiome(biome))
            {
                beach = Biomes.STONE_BEACH;
            }

            // Snowy biomes should always use cold beach; otherwise, the transition looks too abrupt.
            if (biome.isBiomeOfType(Biome.Type.SNOWY))
            {
                beach = Biomes.COLD_BEACH;
            }

            return beach;
        }

        private static bool isTaigaBiome(Biome biome)
        {
            return biome.isBiomeOfType(Biome.Type.COLD)
                && biome.isBiomeOfType(Biome.Type.CONIFEROUS)
                && biome.isBiomeOfType(Biome.Type.FOREST)
                && !biome.isBiomeOfType(Biome.Type.SNOWY);
        }

        /* HUNTING
         *
         */

        public void newRepair(int[] genLayerBiomes, RealisticBiomeBase[] jitteredBiomes, int[] biomeNeighborhood, int neighborhoodSize, float[] noise, float[] riverStrength)
        {

            int sampleSize = 8;
            RealisticBiomeBase realisticBiome;
            int realisticBiomeId;
            if (neighborhoodSize != sampleSize) throw new Exception("mismatch between chunk and analyzer neighborhood sizes");

            // currently just stuffs the genLayer into the jitter;
            for (int i = 0; i < 256; i++)
            {

                realisticBiome = RealisticBiomeBase.getBiome(genLayerBiomes[i]);
                // Do we need to patch the biome?
                if (realisticBiome == null)
                {
                    realisticBiome = biomePatcher.getPatchedRealisticBiome(
                        "NULL biome (" + i + ") found when performing new repair.");
                }
                realisticBiomeId = realisticBiome.baseBiome.getBiomeID();

                bool canBeRiver = riverStrength[i] > 0.7;

                // save what's there since the jitter keeps changing
                savedJittered[i] = jitteredBiomes[i];
                //if (savedJittered[i]== null) throw new RuntimeException();

                if (noise[i] > 61.5)
                {
                    // replace
                    jitteredBiomes[i] = realisticBiome;
                }
                else
                {
                    // check for river
                    if (canBeRiver && !oceanBiome[realisticBiomeId] && !swampBiome[realisticBiomeId])
                    {
                        // make river
                        int riverBiomeID = realisticBiome.riverBiome.getBiomeID();
                        jitteredBiomes[i] = RealisticBiomeBase.getBiome(riverBiomeID);
                    }
                    else
                    {
                        // replace
                        jitteredBiomes[i] = realisticBiome;
                    }
                }
            }

            // put beaches on shores
            beachSearch.notHunted = true;
            beachSearch.absent = false;
            float beachTop = 64.5f;
            for (int i = 0; i < 256; i++)
            {
                if (beachSearch.absent) break; //no point
                float beachBottom = 61.5f;
                if (noise[i] < beachBottom || noise[i] > riverAdjusted(beachTop, riverStrength[i])) continue;// this block isn't beach level
                int biomeID = jitteredBiomes[i].baseBiome.getBiomeID();
                if (swampBiome[biomeID]) continue;// swamps are acceptable at beach level
                if (beachSearch.notHunted)
                {
                    beachSearch.hunt(biomeNeighborhood);
                    landSearch.hunt(biomeNeighborhood);
                }
                int foundBiome = beachSearch.biomes[i];
                if (foundBiome != NO_BIOME)
                {
                    int nearestLandBiome = landSearch.biomes[i];
                    if (nearestLandBiome > -1)
                    {
                        foundBiome = preferredBeach[nearestLandBiome];
                    }

                    realisticBiome = RealisticBiomeBase.getBiome(foundBiome);
                    // Do we need to patch the biome?
                    if (realisticBiome == null)
                    {
                        realisticBiome = biomePatcher.getPatchedRealisticBiome(
                            "NULL biome (" + i + ") found when performing new repair.");
                    }
                    jitteredBiomes[i] = realisticBiome;
                }
            }

            // put land higher up;
            landSearch.absent = false;
            landSearch.notHunted = true;
            for (int i = 0; i < 256; i++)
            {
                if (landSearch.absent && beachSearch.absent) break; //no point
                                                                    // skip if this block isn't above beach level, adjusted for river effect to prevent abrupt beach stops
                if (noise[i] < riverAdjusted(beachTop, riverStrength[i])) continue;
                int biomeID = jitteredBiomes[i].baseBiome.getBiomeID();
                // already land
                if (landBiome[biomeID]) continue;
                // swamps are acceptable above water
                if (swampBiome[biomeID]) continue;
                if (landSearch.notHunted) landSearch.hunt(biomeNeighborhood);
                int foundBiome = landSearch.biomes[i];

                if (foundBiome == NO_BIOME)
                {
                    // no land found; try for a beach
                    if (beachSearch.notHunted)
                    {
                        beachSearch.hunt(biomeNeighborhood);
                    }
                    foundBiome = beachSearch.biomes[i];
                }

                if (foundBiome != NO_BIOME)
                {

                    realisticBiome = RealisticBiomeBase.getBiome(foundBiome);
                    // Do we need to patch the biome?
                    if (realisticBiome == null)
                    {
                        realisticBiome = biomePatcher.getPatchedRealisticBiome(
                            "NULL biome (" + i + ") found when performing new repair.");
                    }
                    jitteredBiomes[i] = realisticBiome;
                }
            }

            // put ocean below sea level
            oceanSearch.absent = false;
            oceanSearch.notHunted = true;
            for (int i = 0; i < 256; i++)
            {
                if (oceanSearch.absent) break; //no point
                float oceanTop = 61.5f;
                if (noise[i] > oceanTop) continue;// too hight
                int biomeID = jitteredBiomes[i].baseBiome.getBiomeID();
                if (oceanBiome[biomeID]) continue;// obviously ocean is OK
                if (swampBiome[biomeID]) continue;// swamps are acceptable
                if (riverBiome[biomeID]) continue;// rivers stay rivers
                if (oceanSearch.notHunted) oceanSearch.hunt(biomeNeighborhood);
                int foundBiome = oceanSearch.biomes[i];

                if (foundBiome != NO_BIOME)
                {

                    realisticBiome = RealisticBiomeBase.getBiome(foundBiome);
                    // Do we need to patch the biome?
                    if (realisticBiome == null)
                    {
                        realisticBiome = biomePatcher.getPatchedRealisticBiome(
                            "NULL biome (" + i + ") found when performing new repair.");
                    }
                    jitteredBiomes[i] = realisticBiome;
                }
            }
            // convert remainder below sea level to lake biome
            for (int i = 0; i < 256; i++)
            {
                int biomeID = jitteredBiomes[i].baseBiome.getBiomeID();
                if (noise[i] <= 61.5 && !riverBiome[biomeID])
                {
                    // check for river
                    if (!oceanBiome[biomeID] &&
                        !swampBiome[biomeID] &&
                        !beachBiome[biomeID])
                    {
                        int riverReplacement = jitteredBiomes[i].riverBiome.getBiomeID(); // make river
                        if (riverReplacement == Biomes.FROZEN_RIVER.getBiomeID())
                            jitteredBiomes[i] = scenicFrozenLakeBiome;
                        else jitteredBiomes[i] = scenicLakeBiome;
                    }
                }
            }
        }

        private void prepareSearchPattern() { /*if (searchPattern.length != 256) throw new RuntimeException();*/ }

        private void setSearches()
        {
            beachSearch = new SmoothingSearchStatus(this.beachBiome);
            landSearch = new SmoothingSearchStatus(this.landBiome);
            oceanSearch = new SmoothingSearchStatus(this.oceanBiome);
        }

        private float riverAdjusted(float top, float river)
        {
            if (river >= 1) return top;
            float erodedRiver = river / RealisticBiomeBase.actualRiverProportion;
            if (erodedRiver <= 1f) top = top * (1 - erodedRiver) + 62f * erodedRiver;
            top = top * (1 - river) + 62f * river;
            return top;
        }
    }
}