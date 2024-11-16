using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceBumper")]
	[Tracked]
	public class BounceBumper : Entity {
		private const float RespawnTime = 0.6f;

		private Sprite sprite;
		private Sprite spriteEvil;
		private VertexLight light;
		private BloomPoint bloom;

		private float respawnTimer;
		private bool fireMode;
		private Wiggler hitWiggler;

		public BounceBumper(Vector2 position)
			: base(position) {
			base.Collider = new Circle(12f);
			Add(new PlayerCollider(OnPlayer));
			Add(sprite = GFX.SpriteBank.Create("bumper"));
			Add(spriteEvil = GFX.SpriteBank.Create("bumper_evil"));
			spriteEvil.Visible = false;
			Add(light = new VertexLight(Color.Teal, 1f, 16, 32));
			Add(bloom = new BloomPoint(0.5f, 16f));
			Add(hitWiggler = Wiggler.Create(1.2f, 2f, delegate {
				spriteEvil.Position = Vector2.Zero * hitWiggler.Value * 8f;
			}));
			Add(new CoreModeListener(OnChangeMode));
		}

		public BounceBumper(EntityData data, Vector2 offset)
			: this(data.Position + offset) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			fireMode = (SceneAs<Level>().CoreMode == Session.CoreModes.Hot);
			spriteEvil.Visible = fireMode;
			sprite.Visible = !fireMode;
		}

		private void OnChangeMode(Session.CoreModes coreMode) {
			fireMode = (coreMode == Session.CoreModes.Hot);
			spriteEvil.Visible = fireMode;
			sprite.Visible = !fireMode;
		}

		public override void Update() {
			base.Update();
			if (respawnTimer > 0f) {
				respawnTimer -= Engine.DeltaTime;
				if (respawnTimer <= 0f) {
					light.Visible = true;
					bloom.Visible = true;
					sprite.Play("on");
					spriteEvil.Play("on");
					if (!fireMode) {
						Audio.Play("event:/game/06_reflection/pinballbumper_reset", Position);
					}
				}
			} else if (base.Scene.OnInterval(0.05f)) {
				float num = Calc.Random.NextAngle();
				ParticleType type = fireMode ? Bumper.P_FireAmbience : Bumper.P_Ambience;
				float direction = fireMode ? (-(float)Math.PI / 2f) : num;
				float length = fireMode ? 12 : 8;
				SceneAs<Level>().Particles.Emit(type, 1, base.Center + Calc.AngleToVector(num, length), Vector2.One * 2f, direction);
			}
		}

		private void OnPlayer(Player player) {
			if (respawnTimer <= 0f) {
				if ((base.Scene as Level).Session.Area.ID == 9) {
					Audio.Play("event:/game/09_core/pinballbumper_hit", Position);
				} else {
					Audio.Play("event:/game/06_reflection/pinballbumper_hit", Position);
				}
				respawnTimer = RespawnTime;
				Vector2 vector2 = player.ExplodeLaunch(Position, snapUp: false, sidesOnly: false);
				player.Dashes = 0;
				sprite.Play("hit", restart: true);
				spriteEvil.Play("hit", restart: true);
				light.Visible = false;
				bloom.Visible = false;
				SceneAs<Level>().DirectionalShake(vector2, 0.15f);
				SceneAs<Level>().Displacement.AddBurst(base.Center, 0.3f, 8f, 32f, 0.8f);
				SceneAs<Level>().Particles.Emit(Bumper.P_Launch, 12, base.Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
			}
		}
	}
}
