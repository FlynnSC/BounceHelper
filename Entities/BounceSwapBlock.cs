using System;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceSwapBlock")]
	[Tracked]
	class BounceSwapBlock : Solid {
		private class PathRenderer : Entity {
			private BounceSwapBlock block;
			private float timer = 0f;

			public PathRenderer(BounceSwapBlock block)
				: base(block.Position) {
				this.block = block;
				base.Depth = 8999;
				timer = Calc.Random.NextFloat();
			}

			public override void Update() {
				base.Update();
				timer += Engine.DeltaTime * 4f;
			}

			public override void Render() {
				float fade = 0.5f * (0.5f + ((float)Math.Sin(timer) + 1f) * 0.25f);
				block.DrawBlockStyle(new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, Color.White * fade);
			}
		}

		private const float ReturnTime = 0.8f;
		public Vector2 Direction;
		public bool Swapping;

		public bool moon; 

		private Vector2 start;
		private Vector2 end;
		private float lerp;
		private int target;

		private Rectangle moveRect;

		private float speed;
		private float maxForwardSpeed;
		private float maxBackwardSpeed;
		private float returnTimer;

		private bool stationary;
		private bool hasRefill = true;
		private const float refillRegenTime = 2.5f;
		private float refillRegenTimer = 0f;
		private const float afterRefillRegenWaitTime = 0.3f;


		private float redAlpha = 1f;

		private MTexture[,] nineSliceGreen;
		private MTexture[,] nineSliceRed;
		private MTexture[,] nineSliceDark;
		private MTexture[,] nineSliceTarget;

		private Sprite middleGreen;
		private Sprite middleRed;
		private MTexture middleDark;

		private EventInstance moveSfx;
		private EventInstance returnSfx;

		private DisplacementRenderer.Burst burst;
		private float particlesRemainder;

		public BounceSwapBlock(Vector2 position, float width, float height, Vector2 node, bool moon)
			: base(position, width, height, safe: false) {
			start = Position;
			end = node;
			this.moon = moon;
			float distance = Vector2.Distance(start, end);
			stationary = distance == 0;
			maxForwardSpeed = (stationary ? 0 : 360f / distance);
			maxBackwardSpeed = maxForwardSpeed * 0.4f;
			Direction.X = Math.Sign(end.X - start.X);
			Direction.Y = Math.Sign(end.Y - start.Y);
			if (!moon) {
				Add(new DashListener {
					OnDash = OnDash
				});
			}
			int num = (int)MathHelper.Min(base.X, node.X);
			int num2 = (int)MathHelper.Min(base.Y, node.Y);
			int num3 = (int)MathHelper.Max(base.X + base.Width, node.X + base.Width);
			int num4 = (int)MathHelper.Max(base.Y + base.Height, node.Y + base.Height);
			moveRect = new Rectangle(num, num2, num3 - num, num4 - num2);
			MTexture mTexture1;
			MTexture mTexture2;
			MTexture mTexture3;
			MTexture mTexture4;
			if (moon) {
				mTexture1 = GFX.Game["objects/swapblock/moon/block"];
				mTexture2 = GFX.Game["objects/swapblock/moon/blockRed"];
				mTexture3 = GFX.Game["objects/BounceHelper/bounceSwapBlock/moon/blockDark"];
				mTexture4 = GFX.Game["objects/swapblock/moon/target"];
			} else {
				mTexture1 = GFX.Game["objects/swapblock/block"];
				mTexture2 = GFX.Game["objects/swapblock/blockRed"];
				mTexture3 = GFX.Game["objects/BounceHelper/bounceSwapBlock/blockDark"];
				mTexture4 = GFX.Game["objects/swapblock/target"];
			}
			nineSliceGreen = new MTexture[3, 3];
			nineSliceRed = new MTexture[3, 3];
			nineSliceDark = new MTexture[3, 3];
			nineSliceTarget = new MTexture[3, 3];
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					nineSliceGreen[i, j] = mTexture1.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
					nineSliceRed[i, j] = mTexture2.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
					nineSliceDark[i, j] = mTexture3.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
					nineSliceTarget[i, j] = mTexture4.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
				}
			}
			
			if (moon) {
				Add(middleGreen = BounceHelperModule.spriteBank.Create("swapBlockLightMoon"));
				Add(middleRed = BounceHelperModule.spriteBank.Create("swapBlockLightRedMoon"));
				middleDark = GFX.Game["objects/BounceHelper/bounceSwapBlock/moon/midBlockDark"];
			} else {
				Add(middleGreen = GFX.SpriteBank.Create("swapBlockLight"));
				Add(middleRed = GFX.SpriteBank.Create("swapBlockLightRed"));
				middleDark = GFX.Game["objects/BounceHelper/bounceSwapBlock/midBlockDark"];
			}
			Add(new LightOcclude(0.2f));
			base.Depth = -9999;
		}

		public BounceSwapBlock(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Bool("moon", false)) {
		}

		public override void Awake(Scene scene) {
			base.Awake(scene);
			scene.Add(new PathRenderer(this));
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			Audio.Stop(moveSfx);
			Audio.Stop(returnSfx);
		}

		public override void SceneEnd(Scene scene) {
			base.SceneEnd(scene);
			Audio.Stop(moveSfx);
			Audio.Stop(returnSfx);
		}

		public void OnDash(Vector2 direction) {
			if (hasRefill) {
				Swapping = (lerp < 1f);
				target = 1;
				returnTimer = ReturnTime;
				burst = (base.Scene as Level).Displacement.AddBurst(base.Center, 0.2f, 0f, 16f);
				if (lerp >= 0.2f) {
					speed = maxForwardSpeed;
				} else {
					speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
				}
				Audio.Stop(returnSfx);
				Audio.Stop(moveSfx);
				if (!Swapping) {
					Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", base.Center);
				} else {
					moveSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_move", base.Center);
				}
			}
		}

		public bool onBounce(float angle) {
			if (hasRefill && returnTimer > 0f) {
				Audio.Play("event:/game/general/diamond_touch", Position);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Celeste.Freeze(0.05f);
				SceneAs<Level>().ParticlesFG.Emit(Refill.P_Shatter, 5, Center, Vector2.One * 4f, angle - (float)Math.PI / 2f);
				SceneAs<Level>().ParticlesFG.Emit(Refill.P_Shatter, 5, Center, Vector2.One * 4f, angle + (float)Math.PI / 2f);

				returnTimer = afterRefillRegenWaitTime;
				refillRegenTimer = refillRegenTime;
				hasRefill = false;
				return true;
			}
			return false;
		}

		public override void Update() {
			base.Update();

			if (!hasRefill) {
				refillRegenTimer -= Engine.DeltaTime;
				if (refillRegenTimer <= 0) {
					Audio.Play("event:/game/general/diamond_return", Position);
					SceneAs<Level>().ParticlesFG.Emit(Refill.P_Regen, 16, Center, Vector2.One * 2f);
					hasRefill = true;
				}
			}

			if (returnTimer > 0f && hasRefill) {
				returnTimer -= Engine.DeltaTime;
				if (returnTimer <= 0f) {
					target = 0;
					speed = 0f;
					if (!stationary) {
						returnSfx = Audio.Play("event:/game/05_mirror_temple/swapblock_return", base.Center);
					}
				}
			}
			if (burst != null) {
				burst.Position = base.Center;
			}
			redAlpha = Calc.Approach(redAlpha, (target != 1) ? 1 : 0, Engine.DeltaTime * 32f);
			if (target == 0 && lerp == 0f) {
				middleRed.SetAnimationFrame(0);
				middleGreen.SetAnimationFrame(0);
			}
			if (target == 1) {
				speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
			} else {
				speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
			}
			float num = lerp;
			lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
			if (lerp != num) {
				Vector2 liftSpeed = (end - start) * speed;
				Vector2 position = Position;
				if (target == 1) {
					liftSpeed = (end - start) * maxForwardSpeed;
				}
				if (lerp < num) {
					liftSpeed *= -1f;
				}
				if (target == 1 && base.Scene.OnInterval(0.02f)) {
					MoveParticles(end - start);
				}
				MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
				if (position != Position) {
					Audio.Position(moveSfx, base.Center);
					Audio.Position(returnSfx, base.Center);
					if (Position == start && target == 0) {
						Audio.SetParameter(returnSfx, "end", 1f);
						Audio.Play("event:/game/05_mirror_temple/swapblock_return_end", base.Center);
					} else if (Position == end && target == 1) {
						Audio.Play("event:/game/05_mirror_temple/swapblock_move_end", base.Center);
					}
				}
			}
			if (Swapping && lerp >= 1f) {
				Swapping = false;
			}
			StopPlayerRunIntoAnimation = (lerp <= 0f || lerp >= 1f);
		}

		private void MoveParticles(Vector2 normal) {
			Vector2 position;
			Vector2 positionRange;
			float direction;
			float num;
			if (normal.X > 0f) {
				position = base.CenterLeft;
				positionRange = Vector2.UnitY * (base.Height - 6f);
				direction = (float)Math.PI;
				num = Math.Max(2f, base.Height / 14f);
			} else if (normal.X < 0f) {
				position = base.CenterRight;
				positionRange = Vector2.UnitY * (base.Height - 6f);
				direction = 0f;
				num = Math.Max(2f, base.Height / 14f);
			} else if (normal.Y > 0f) {
				position = base.TopCenter;
				positionRange = Vector2.UnitX * (base.Width - 6f);
				direction = -(float)Math.PI / 2f;
				num = Math.Max(2f, base.Width / 14f);
			} else {
				position = base.BottomCenter;
				positionRange = Vector2.UnitX * (base.Width - 6f);
				direction = (float)Math.PI / 2f;
				num = Math.Max(2f, base.Width / 14f);
			}
			particlesRemainder += num;
			int num2 = (int)particlesRemainder;
			particlesRemainder -= num2;
			positionRange *= 0.5f;
			SceneAs<Level>().Particles.Emit(SwapBlock.P_Move, num2, position, positionRange, direction);
		}

		public override void Render() {
			Vector2 vector = Position + base.Shake;
			if (lerp != (float)target && speed > 0f) {
				Vector2 value = (end - start).SafeNormalize();
				if (target == 1) {
					value *= -1f;
				}
				float num = speed / maxForwardSpeed;
				float num2 = 16f * num;
				for (int i = 2; (float)i < num2; i += 2) {
					DrawBlockStyle(vector + value * i, base.Width, base.Height, nineSliceGreen, middleGreen, Color.White * (1f - (float)i / num2));
				}
			}
			if (hasRefill) {
				if (redAlpha < 1f) {
					DrawBlockStyle(vector, base.Width, base.Height, nineSliceGreen, middleGreen, Color.White);
				}
				if (redAlpha > 0f) {
					DrawBlockStyle(vector, base.Width, base.Height, nineSliceRed, middleRed, Color.White * redAlpha);
				}
			} else {
				DrawBlockStyle(vector, base.Width, base.Height, nineSliceDark, null, Color.White);
				middleDark.DrawCentered(base.Center);
			}
		}

		private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] nineSlice, Sprite middle, Color color) {
			int num = (int)(width / 8f);
			int num2 = (int)(height / 8f);
			nineSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
			nineSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
			nineSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
			nineSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
			for (int i = 1; i < num - 1; i++) {
				nineSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
				nineSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
			}
			for (int j = 1; j < num2 - 1; j++) {
				nineSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
				nineSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
			}
			for (int k = 1; k < num - 1; k++) {
				for (int l = 1; l < num2 - 1; l++) {
					nineSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
				}
			}
			if (middle != null) {
				middle.Color = color;
				middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
				middle.Render();
			}
		}
	}
}
