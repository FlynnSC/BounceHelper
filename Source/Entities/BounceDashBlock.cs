using Celeste.Mod.Entities;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static Celeste.GaussianBlur;
using static Celeste.TrackSpinner;

namespace Celeste.Mod.BounceHelper.Entities
{
    [CustomEntity("BounceHelper/BounceDashBlock")]
    [Tracked(false)]
    public class BounceDashBlock : Solid
    {
        private bool permanent;

        private EntityID id;

        private char tileType;

        private float width;

        private float height;

        private bool blendIn;

        public bool canGraze;

        public Vector2 lastDashDir;

        public BounceDashBlock(Vector2 position, char tiletype, float width, float height, bool blendIn, bool permanent, bool canGraze, EntityID id)
            : base(position, width, height, safe: true)
        {
            base.Depth = -12999;
            this.id = id;
            this.permanent = permanent;
            this.width = width;
            this.height = height;
            this.blendIn = blendIn;
            this.canGraze = canGraze;
            tileType = tiletype;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];

            Add(new DashListener
            {
                OnDash = OnDash
            });
        }

        public BounceDashBlock(EntityData data, Vector2 offset, EntityID id)
            : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height, data.Bool("blendin"), data.Bool("permanent", defaultValue: true), data.Bool("canGraze", defaultValue: true), id)
        {
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            TileGrid tileGrid;
            if (!blendIn)
            {
                tileGrid = GFX.FGAutotiler.GenerateBox(tileType, (int)width / 8, (int)height / 8).TileGrid;
                Add(new LightOcclude());
            }
            else
            {
                Level level = SceneAs<Level>();
                Rectangle tileBounds = level.Session.MapData.TileBounds;
                VirtualMap<char> solidsData = level.SolidsData;
                int x = (int)(base.X / 8f) - tileBounds.Left;
                int y = (int)(base.Y / 8f) - tileBounds.Top;
                int tilesX = (int)base.Width / 8;
                int tilesY = (int)base.Height / 8;
                tileGrid = GFX.FGAutotiler.GenerateOverlay(tileType, x, y, tilesX, tilesY, solidsData).TileGrid;
                Add(new EffectCutout());
                base.Depth = -10501;
            }
            Add(tileGrid);
            Add(new TileInterceptor(tileGrid, highPriority: true));
            if (CollideCheck<Player>())
            {
                RemoveSelf();
            }
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            global::Celeste.Celeste.Freeze(0.05f);
        }

        public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true)
        {
            if (playSound)
            {
                if (tileType == '1')
                {
                    Audio.Play("event:/game/general/wall_break_dirt", Position);
                }
                else if (tileType == '3')
                {
                    Audio.Play("event:/game/general/wall_break_ice", Position);
                }
                else if (tileType == '9')
                {
                    Audio.Play("event:/game/general/wall_break_wood", Position);
                }
                else
                {
                    Audio.Play("event:/game/general/wall_break_stone", Position);
                }
            }
            for (int i = 0; (float)i < base.Width / 8f; i++)
            {
                for (int j = 0; (float)j < base.Height / 8f; j++)
                {
                    base.Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 8, 4 + j * 8), tileType, playDebrisSound).BlastFrom(from));
                }
            }
            Collidable = false;
            if (permanent)
            {
                RemoveAndFlagAsGone();
            }
            else
            {
                RemoveSelf();
            }
        }

        public void RemoveAndFlagAsGone()
        {
            RemoveSelf();
            SceneAs<Level>().Session.DoNotLoad.Add(id);
        }

        private void OnDash(Vector2 direction)
        {
            lastDashDir = direction;
        }

        public void Break(Vector2 from, Vector2 direction, bool playSound = true)
        {
            Break(from, direction, playSound, playDebrisSound: true);
        }
    }
}
