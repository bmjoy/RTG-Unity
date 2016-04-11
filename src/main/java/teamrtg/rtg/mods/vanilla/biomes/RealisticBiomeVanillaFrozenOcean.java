package teamrtg.rtg.mods.vanilla.biomes;

import net.minecraft.init.Biomes;
import net.minecraft.init.Blocks;
import teamrtg.rtg.util.noise.CellNoise;
import teamrtg.rtg.util.noise.OpenSimplexNoise;
import teamrtg.rtg.world.biome.surface.SurfaceBase;
import teamrtg.rtg.mods.vanilla.surfaces.SurfaceVanillaFrozenOcean;
import teamrtg.rtg.world.biome.terrain.TerrainBase;

public class RealisticBiomeVanillaFrozenOcean extends RealisticBiomeVanillaBase {

    public RealisticBiomeVanillaFrozenOcean() {

        super(
                Biomes.FROZEN_OCEAN,
                Biomes.RIVER
        );
        this.noLakes = true;
    }

    @Override
    protected TerrainBase initTerrain() {
        return new TerrainBase() {
            @Override
            public float generateNoise(OpenSimplexNoise simplex, CellNoise cell, int x, int y, float border, float river) {
                return terrainOcean(x, y, simplex, river, 50f);
            }
        };
    }

    @Override
    protected SurfaceBase initSurface() {
        return new SurfaceVanillaFrozenOcean(this, 20f, 0.2f);
    }

    @Override
    protected void initDecos() {

    }

    @Override
    protected void initProperties() {
        config.addBlock(config.MIX_BLOCK).setDefault(Blocks.GRAVEL.getDefaultState());
        this.config.SURFACE_WATER_LAKE_CHANCE.setDefault(0);
        this.config.SURFACE_LAVA_LAKE_CHANCE.setDefault(0);
    }
}
