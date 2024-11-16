using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceRefill")]
	[Tracked]
	public class BounceRefill : Entity {
		private Sprite sprite;
		private Sprite flash;
		private Image outline;
		private Wiggler wiggler;
		private BloomPoint bloom;
		private VertexLight light;

		private Level level;
		private SineWave sine;

		private bool twoDashes;
		private bool oneUse;
		private bool jellyfishOnly;
		public int dashes;

		private ParticleType p_shatter;
		private ParticleType p_regen;
		private ParticleType p_glow;

		private float respawnTime;
		private float respawnTimer;

		public BounceRefill(Vector2 position, bool twoDashes, bool oneUse, bool jellyfishOnly, float respawnTime)
			: base(position) {
			base.Collider = new Hitbox(16f, 16f, -8f, -8f);
			if (!jellyfishOnly) {
				Add(new PlayerCollider(OnPlayer));
			}
			this.twoDashes = twoDashes;
			this.oneUse = oneUse;
			this.jellyfishOnly = jellyfishOnly;
			this.respawnTime = respawnTime;
			dashes = twoDashes ? 2 : 1;
			string str;
			if (jellyfishOnly) { 
				if (twoDashes) {
					str = "objects/BounceHelper/bounceRefillJellyfishOnly/refillTwo/";
					p_shatter = Refill.P_ShatterTwo;
					p_regen = Refill.P_RegenTwo;
					p_glow = Refill.P_GlowTwo;
				} else {
					str = "objects/BounceHelper/bounceRefillJellyfishOnly/refill/";
					p_shatter = new ParticleType(Refill.P_Shatter);
					p_shatter.Color = Calc.HexToColor("ffd4d4");
					p_shatter.Color2 = Calc.HexToColor("fc8686");
					p_regen = new ParticleType(Refill.P_Regen);
					p_regen.Color = Calc.HexToColor("FFA6CC");
					p_regen.Color2 = Calc.HexToColor("e06e6e");
					p_glow = new ParticleType(Refill.P_Glow);
					p_glow.Color = p_regen.Color;
					p_glow.Color2 = p_regen.Color2;
				}
			} else { 
				if (twoDashes) {
					str = "objects/refillTwo/";
					p_shatter = Refill.P_ShatterTwo;
					p_regen = Refill.P_RegenTwo;
					p_glow = Refill.P_GlowTwo;
				} else {
					str = "objects/refill/";
					p_shatter = Refill.P_Shatter;
					p_regen = Refill.P_Regen;
					p_glow = Refill.P_Glow;
				}
			}
			Add(outline = new Image(GFX.Game[str + "outline"]));
			outline.CenterOrigin();
			outline.Visible = false;
			Add(sprite = new Sprite(GFX.Game, str + "idle"));
			sprite.AddLoop("idle", "", 0.1f);
			sprite.Play("idle");
			sprite.CenterOrigin();
			Add(flash = new Sprite(GFX.Game, str + "flash"));
			flash.Add("flash", "", 0.05f);
			flash.OnFinish = delegate
			{
				flash.Visible = false;
			};
			flash.CenterOrigin();
			Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
			{
				sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
			}));
			Add(new MirrorReflection());
			Add(bloom = new BloomPoint(0.8f, 16f));
			Add(light = new VertexLight(Color.White, 1f, 16, 48));
			Add(sine = new SineWave(0.6f, 0f));
			sine.Randomize();
			UpdateY();
			base.Depth = -100;
		}

		public BounceRefill(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Bool("twoDash"), data.Bool("oneUse"), data.Bool("jellyfishOnly"), data.Float("respawnTime", 2.5f)) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			level = SceneAs<Level>();
		}

		public override void Update() {
			base.Update();
			if (respawnTimer > 0f) {
				respawnTimer -= Engine.DeltaTime;
				if (respawnTimer <= 0f) {
					Respawn();
				}
			} else if (base.Scene.OnInterval(0.1f)) {
				level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);
			}
			UpdateY();
			light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
			bloom.Alpha = light.Alpha * 0.8f;
			if (base.Scene.OnInterval(2f) && sprite.Visible) {
				flash.Play("flash", restart: true);
				flash.Visible = true;
			}
		}

		private void Respawn() {
			if (!Collidable) {
				Collidable = true;
				sprite.Visible = true;
				outline.Visible = false;
				base.Depth = -100;
				wiggler.Start();
				Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_return" : "event:/game/general/diamond_return", Position);
				level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
			}
		}

		private void UpdateY() {
			Sprite obj = flash;
			Sprite obj2 = sprite;
			float num2 = bloom.Y = sine.Value * 2f;
			float num5 = obj.Y = (obj2.Y = num2);
		}

		public override void Render() {
			if (sprite.Visible) {
				sprite.DrawOutline();
			}
			base.Render();
		}

		private void OnPlayer(Player player) {
			if (player.UseRefill(twoDashes)) {
				use(player.Speed.Angle());
			}
		}

		public void use(float breakAngle) {
			Audio.Play(twoDashes ? "event:/new_content/game/10_farewell/pinkdiamond_touch" : "event:/game/general/diamond_touch", Position);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
			Collidable = false;
			Add(new Coroutine(RefillRoutine(breakAngle)));
			respawnTimer = respawnTime;
		}

		private IEnumerator RefillRoutine(float breakAngle) {
			Celeste.Freeze(0.05f);
			yield return null;
			level.Shake();
			sprite.Visible = (flash.Visible = false);
			if (!oneUse) {
				outline.Visible = true;
			}
			Depth = 8999;
			yield return 0.05f;
			level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, breakAngle - (float)Math.PI / 2f);
			level.ParticlesFG.Emit(p_shatter, 5, Position, Vector2.One * 4f, breakAngle + (float)Math.PI / 2f);
			SlashFx.Burst(Position, breakAngle);
			if (oneUse) {
				RemoveSelf();
			}
		}
	}
}
