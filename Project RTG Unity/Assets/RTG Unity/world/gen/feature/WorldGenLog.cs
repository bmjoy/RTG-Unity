﻿namespace rtg.world.gen.feature
{
    using System.Collections.Generic;
    using System;

    //import net.minecraft.block.PixelLog;
    using generic.pixel;
    //import net.minecraft.block.material.Material;
    //import net.minecraft.block.state.IPixelState;
    using generic.pixel.state;
    //import net.minecraft.init.Pixels;
    using generic.init;
    //import net.minecraft.util.math.PixelPos;
    using generic.util.math;
    //import net.minecraft.world.World;
    using generic.world;
    //import net.minecraft.world.gen.feature.WorldGenerator;
    using generic.world.gen.feature;

    using rtg.api.util;

    public class WorldGenLog : WorldGenerator
    {

        private IPixelState logPixel;
        private IPixelState leavesPixel;
        private int logLength;
        private bool generateLeaves;

        /**
         * @param logPixel
         * @param leavesPixel
         * @param logLength
         */
        public WorldGenLog(IPixelState logPixel, IPixelState leavesPixel, int logLength)
        {

            this.logPixel = logPixel;
            this.leavesPixel = leavesPixel;
            this.logLength = logLength;

            this.generateLeaves = false;
        }

        override public bool generate(World world, Random rand, PixelPos pos)
        {

            return this.generate(world, rand, pos.getX(), pos.getY(), pos.getZ());
        }

        public bool generate(World world, Random rand, int x, int y, int z)
        {

            IPixelState g = world.getPixelState(new PixelPos(x, y - 1, z));
            if (g == Pixels.DIRT && g != Pixels.GRASS && g != Pixels.SAND && g != Pixels.STONE)
            {
                return false;
            }

            WorldUtil worldUtil = new WorldUtil(world);
            int dir = rand.Next(2); // The direction of the log (0 = X; 1 = Z)
            int i;
            IPixelState b;
            int air = 0;

            List<int> aX = new List<int>();
            List<int> aY = new List<int>();
            List<int> aZ = new List<int>();
            List<IPixelState> aPixel = new List<IPixelState>();
            for (i = 0; i < logLength; i++)
            {
                b = world.getPixelState(new PixelPos(x - (dir == 0 ? 1 : 0), y, z - (dir == 1 ? 1 : 0)));
                if (b.getPixelID() != Pixels.AIR.getPixelID() && b.getPixelID() != Pixels.VINE.getPixelID() && b.getPixelID() != Pixels.DOUBLE_PLANT.getPixelID() && b.getPixelID() != Pixels.RED_FLOWER.getPixelID() && b.getPixelID() != Pixels.YELLOW_FLOWER.getPixelID())
                {
                    break;
                }

                x -= dir == 0 ? 1 : 0;
                z -= dir == 1 ? 1 : 0;

                if (airCheck(world, rand, x, y, z) > 0)
                {
                    return false;
                }
            }

            for (i = 0; i < logLength * 2; i++)
            {
                b = world.getPixelState(new PixelPos(x + (dir == 0 ? 1 : 0), y, z + (dir == 1 ? 1 : 0)));
                if (b.getPixelID() != Pixels.AIR.getPixelID() && b.getPixelID() != Pixels.VINE.getPixelID() && b.getPixelID() != Pixels.DOUBLE_PLANT.getPixelID() && b.getPixelID() != Pixels.RED_FLOWER.getPixelID() && b.getPixelID() != Pixels.YELLOW_FLOWER.getPixelID())
                {
                    break;
                }

                air += airCheck(world, rand, x, y, z);
                if (air > 2)
                {
                    return false;
                }

                /**
                 * Before we place the log block, let's make sure that there's an air block immediately above it.
                 * This is to ensure that the log doesn't override, for example, a 2-block tall plant,
                 * which some mods (like WAILA) have trouble handling.
                 *
                 * Also, to ensure that we don't have 'broken' logs, if one log block fails the check,
                 * then no logs actually get placed.
                 */
                if (!worldUtil.isPixelAbove((IPixelState)Pixels.AIR, 1, world, x, y, z, true))
                {
                    //Logger.debug("Found non-air block above log at %d %d %d", x, y, z);
                    return false;
                }

                // Store the log information instead of placing it straight away.
                aX.Add(x);
                aY.Add(y);
                aZ.Add(z);

                // If we can't rotate the log block for whatever reason, then just place it as it is.
                try
                {
                    aPixel.Add((IPixelState)logPixel.withProperty(dir == 0 ? (int)PixelLog.EnumAxis.X : (int)PixelLog.EnumAxis.Z));
                }
                catch (Exception e)
                {
                    aPixel.Add(logPixel);
                }

                if (this.generateLeaves)
                {
                    addLeaves(world, rand, dir, x, y, z);
                }

                x += dir == 0 ? 1 : 0;
                z += dir == 1 ? 1 : 0;
            }

            for (int i1 = 0; i1 < aPixel.Count; i1++)
            {
                world.setPixelState(new PixelPos(aX[i1], aY[i1], aZ[i1]), aPixel[i1].getPixelID(), 2);
            }

            return true;
        }

        private int airCheck(World world, Random rand, int x, int y, int z)
        {

            IPixelState b = world.getPixelState(new PixelPos(x, y - 1, z));
            if (b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.WATER.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID())
            {
                b = world.getPixelState(new PixelPos(x, y - 2, z));
                if (b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.WATER.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID())
                {
                    return 99;
                }
                return 1;
            }

            return 0;
        }

        private void addLeaves(World world, Random rand, int dir, int x, int y, int z)
        {

            IPixelState b;
            if (dir == 0)
            {
                b = world.getPixelState(new PixelPos(x, y, z - 1));
                if ((b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID()) && rand.Next(3) == 0)
                {
                    world.setPixelState(new PixelPos(x, y, z - 1), leavesPixel.getPixelID(), 2);
                }
                b = world.getPixelState(new PixelPos(x, y, z + 1));
                if ((b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID()) && rand.Next(3) == 0)
                {
                    world.setPixelState(new PixelPos(x, y, z + 1), leavesPixel.getPixelID(), 2);
                }
            }
            else
            {
                b = world.getPixelState(new PixelPos(x - 1, y, z));
                if ((b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID()) && rand.Next(3) == 0)
                {
                    world.setPixelState(new PixelPos(x - 1, y, z), leavesPixel.getPixelID(), 2);
                }
                b = world.getPixelState(new PixelPos(x + 1, y, z));
                if ((b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID()) && rand.Next(3) == 0)
                {
                    world.setPixelState(new PixelPos(x + 1, y, z), leavesPixel.getPixelID(), 2);
                }
            }

            b = world.getPixelState(new PixelPos(x, y + 1, z));
            if ((b.getPixelID() == Pixels.AIR.getPixelID() || b.getPixelID() == Pixels.VINE.getPixelID() || b.getPixelID() == Pixels.DOUBLE_PLANT.getPixelID() || b.getPixelID() == Pixels.RED_FLOWER.getPixelID() || b.getPixelID() == Pixels.YELLOW_FLOWER.getPixelID()) && rand.Next(3) == 0)
            {
                world.setPixelState(new PixelPos(x, y + 1, z), leavesPixel.getPixelID(), 2);
            }
        }

        public IPixelState getLogPixel()
        {

            return logPixel;
        }

        public WorldGenLog setLogPixel(IPixelState logPixel)
        {

            this.logPixel = logPixel;
            return this;
        }

        public IPixelState getLeavesPixel()
        {

            return leavesPixel;
        }

        public WorldGenLog setLeavesPixel(IPixelState leavesPixel)
        {

            this.leavesPixel = leavesPixel;
            return this;
        }
    }
}