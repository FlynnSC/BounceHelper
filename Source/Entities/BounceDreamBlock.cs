using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceDreamBlock")]
	[TrackedAs(typeof(DreamBlock))]
	class BounceDreamBlock : DreamBlock {

		#region Vanilla stuff
		private struct DreamParticle {
			public Vector2 Position;
			public int Layer;
			public Color Color;
			public float TimeOffset;
		}

		private static readonly Color activeBackColor = Color.Black;
		private static readonly Color activeLineColor = Color.White;

		private float wobbleFrom = Calc.Random.NextFloat((float)Math.PI * 2f);
		private float wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);
		private float wobbleEase = 0f;

		private MTexture[] particleTextures;
		private DreamParticle[] particles;

		private float animTimer;
		#endregion

		private Vector2? nodeRef;
		private Vector2 dreamLiftSpeed = Vector2.Zero;

		private Vector2 internalAccel;
		private Vector2 particleAccelOffset = Vector2.Zero;
		private const float particleAccelOffsetMult = 0.35f;
		private float oscillationDuration;

		private float canvasWidth = 128f;
		private float canvasHeight = 128f;

		public BounceDreamBlock(Vector2 position, float width, float height, Vector2? node, float internalAccelX, float internalAccelY, float oscillationDuration)
			: base(position, width, height, node, true, false) {
			nodeRef = node;
			internalAccel = new Vector2(internalAccelX, internalAccelY);
			this.oscillationDuration = oscillationDuration;
			canvasWidth = Math.Max(canvasWidth, width);
			canvasHeight = Math.Max(canvasHeight, height);

			particleTextures = new MTexture[4]
			{
				GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
				GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
				GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
				GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7)
			};
		}

		public BounceDreamBlock(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Height, data.FirstNodeNullable(offset), data.Float("internalAccelX"), data.Float("internalAccelY"), data.Float("oscillationDuration")) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);

			if (nodeRef.HasValue) {
				// Removes old tween
				Tween tween = null;
				foreach (Component component in Components) {
					if (component is Tween) {
						tween = component as Tween;
					}
				}
				Remove(tween);

				// New tween with customizable oscillation period
				Vector2 start = Position;
				Vector2 end = nodeRef.Value;
				tween = Tween.Create(Tween.TweenMode.YoyoLooping, Ease.SineInOut, oscillationDuration / 2, start: true);
				tween.OnUpdate = delegate (Tween t) {
					if (Collidable) {
						MoveTo(Vector2.Lerp(start, end, t.Eased));
					} else {
						MoveToNaive(Vector2.Lerp(start, end, t.Eased));
					}
					dreamLiftSpeed = LiftSpeed;
				};
				Add(tween);
			}
			Setup();
		}

		public new void Setup() {
			particles = new DreamParticle[(int)(canvasWidth / 8f * (canvasHeight / 8f) * 0.7f)];
			for (int i = 0; i < particles.Length; i++) {
				particles[i].Position = new Vector2(Calc.Random.NextFloat(canvasWidth) + X + Width / 2, Calc.Random.NextFloat(canvasHeight) + Y + Height / 2);
				particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
				particles[i].TimeOffset = Calc.Random.NextFloat();
				switch (particles[i].Layer) {
					case 0:
						particles[i].Color = Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"));
						break;
					case 1:
						particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"));
						break;
					case 2:
						particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"));
						break;
				}
			}
		}

		public override void Update() {
			base.Update();
			animTimer += 6f * Engine.DeltaTime;
			wobbleEase += Engine.DeltaTime * 2f;
			if (wobbleEase > 1f) {
				wobbleEase = 0f;
				wobbleFrom = wobbleTo;
				wobbleTo = Calc.Random.NextFloat((float)Math.PI * 2f);
			}

			LiftSpeed = dreamLiftSpeed;

			particleAccelOffset += internalAccel * particleAccelOffsetMult * Engine.DeltaTime;
			//particleAccelOffset = PutInside(particleAccelOffset);

			Player player = CollideFirst<Player>();
			if (player != null) {
				player.Speed += internalAccel * Engine.DeltaTime;
			}
		}

		public override void Render() {
			Camera camera = SceneAs<Level>().Camera;
			if (base.Right < camera.Left || base.Left > camera.Right || base.Bottom < camera.Top || base.Top > camera.Bottom) {
				return;
			}
			Draw.Rect(base.X, base.Y, base.Width, base.Height, activeBackColor);
			Vector2 position = SceneAs<Level>().Camera.Position;
			for (int i = 0; i < particles.Length; i++) {
				int layer = particles[i].Layer;
				Vector2 position2 = particles[i].Position;
				position2 += position * (0.3f + 0.25f * (float)layer) + particleAccelOffset * (0.3f + 0.25f * (1 - (float)layer));
				position2 = PutInside(position2);
				Color color = particles[i].Color;
				MTexture mTexture;
				switch (layer) {
					case 0: {
							int num2 = (int)((particles[i].TimeOffset * 4f + animTimer) % 4f);
							mTexture = particleTextures[3 - num2];
							break;
						}
					case 1: {
							int num = (int)((particles[i].TimeOffset * 2f + animTimer) % 2f);
							mTexture = particleTextures[1 + num];
							break;
						}
					default:
						mTexture = particleTextures[2];
						break;
				}
				if (position2.X >= base.X + 2f && position2.Y >= base.Y + 2f && position2.X < base.Right - 2f && position2.Y < base.Bottom - 2f) {
					mTexture.DrawCentered(position2, color);
				}
			}
			WobbleLine(new Vector2(base.X, base.Y), new Vector2(base.X + base.Width, base.Y), 0f);
			WobbleLine(new Vector2(base.X + base.Width, base.Y), new Vector2(base.X + base.Width, base.Y + base.Height), 0.7f);
			WobbleLine(new Vector2(base.X + base.Width, base.Y + base.Height), new Vector2(base.X, base.Y + base.Height), 1.5f);
			WobbleLine(new Vector2(base.X, base.Y + base.Height), new Vector2(base.X, base.Y), 2.5f);
			Draw.Rect(new Vector2(base.X, base.Y), 2f, 2f, activeLineColor);
			Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y), 2f, 2f, activeLineColor);
			Draw.Rect(new Vector2(base.X, base.Y + base.Height - 2f), 2f, 2f, activeLineColor);
			Draw.Rect(new Vector2(base.X + base.Width - 2f, base.Y + base.Height - 2f), 2f, 2f, activeLineColor);
		}

		private Vector2 PutInside(Vector2 pos) {
			while (pos.X < base.X) {
				pos.X += canvasWidth;
			}
			while (pos.X > base.X + canvasWidth) {
				pos.X -= canvasWidth;
			}
			while (pos.Y < base.Y) {
				pos.Y += canvasHeight;
			}
			while (pos.Y > base.Y + canvasHeight) {
				pos.Y -= canvasHeight;
			}
			return pos;
		}

		private void WobbleLine(Vector2 from, Vector2 to, float offset) {
			float num = (to - from).Length();
			Vector2 value = Vector2.Normalize(to - from);
			Vector2 vector = new Vector2(value.Y, 0f - value.X);
			Color color = activeLineColor;
			Color color2 = activeBackColor;
			float scaleFactor = 0f;
			int num2 = 16;
			for (int i = 2; (float)i < num - 2f; i += num2) {
				float num3 = Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i), wobbleEase);
				if ((float)(i + num2) >= num) {
					num3 = 0f;
				}
				float num4 = Math.Min(num2, num - 2f - (float)i);
				Vector2 vector2 = from + value * i + vector * scaleFactor;
				Vector2 vector3 = from + value * ((float)i + num4) + vector * num3;
				Draw.Line(vector2 - vector, vector3 - vector, color2);
				Draw.Line(vector2 - vector * 2f, vector3 - vector * 2f, color2);
				Draw.Line(vector2, vector3, color);
				scaleFactor = num3;
			}
		}

		private float LineAmplitude(float seed, float index) {
			return (float)(Math.Sin((double)(seed + index / 16f) + Math.Sin(seed * 2f + index / 32f) * 6.2831854820251465) + 1.0) * 1.5f;
		}

		private float Lerp(float a, float b, float percent) {
			return a + (b - a) * percent;
		}
	}
}
