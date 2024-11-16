using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceMoveBlock")]
	[Tracked]
	public class BounceMoveBlock : Solid {
		public enum Directions {
			Right,
			UpRight,
			Up,
			UpLeft,
			Left,
			DownLeft,
			Down,
			DownRight,
			Unknown
		}

		private enum MovementState {
			Idling,
			Moving,
			Breaking
		}

		private class Border : Entity {
			public BounceMoveBlock Parent;

			public Border(BounceMoveBlock parent) {
				Parent = parent;
				base.Depth = 1;
			}

			public override void Update() {
				if (Parent.Scene != base.Scene) {
					RemoveSelf();
				}
				base.Update();
			}

			public override void Render() {
				Draw.Rect(Parent.X + Parent.Shake.X - 1f, Parent.Y + Parent.Shake.Y - 1f, Parent.Width + 2f, Parent.Height + 2f, Color.Black);
			}
		}

		[Pooled]
		private class Debris : Actor {
			private Image sprite;
			private bool spriteInitialised = false;

			private Vector2 home;

			private Vector2 speed;

			private bool shaking;

			private bool returning;

			private float returnEase;

			private float returnDuration;

			private SimpleCurve returnCurve;

			private bool firstHit;

			private float alpha;

			private Collision onCollideH;

			private Collision onCollideV;

			private float spin;

			public Debris()
				: base(Vector2.Zero) {
				base.Tag = Tags.TransitionUpdate;
				base.Collider = new Hitbox(4f, 4f, -2f, -2f);
				onCollideH = delegate {
					speed.X = (0f - speed.X) * 0.5f;
				};
				onCollideV = delegate {
					if (firstHit || speed.Y > 50f) {
						Audio.Play("event:/game/general/debris_stone", Position, "debris_velocity", Calc.ClampedMap(speed.Y, 0f, 600f));
					}
					if (speed.Y > 0f && speed.Y < 40f) {
						speed.Y = 0f;
					} else {
						speed.Y = (0f - speed.Y) * 0.25f;
					}
					firstHit = false;
				};
			}

			public override void OnSquish(CollisionData data) {
			}

			public Debris Init(string spritePath, Vector2 position, Vector2 center, Vector2 returnTo) {
				if (!spriteInitialised) {
					Add(sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(spritePath + "/debris"))));
					sprite.CenterOrigin();
					sprite.FlipX = Calc.Random.Chance(0.5f);
					spriteInitialised = true;
				}
				Collidable = true;
				Position = position;
				speed = (position - center).SafeNormalize(60f + Calc.Random.NextFloat(60f));
				home = returnTo;
				sprite.Position = Vector2.Zero;
				sprite.Rotation = Calc.Random.NextAngle();
				returning = false;
				shaking = false;
				sprite.Scale.X = 1f;
				sprite.Scale.Y = 1f;
				sprite.Color = Color.White;
				alpha = 1f;
				firstHit = false;
				spin = Calc.Random.Range(3.49065852f, 10.4719753f) * (float)Calc.Random.Choose(1, -1);
				return this;
			}

			public override void Update() {
				base.Update();
				if (!returning) {
					if (Collidable) {
						speed.X = Calc.Approach(speed.X, 0f, Engine.DeltaTime * 100f);
						if (!OnGround()) {
							speed.Y += 400f * Engine.DeltaTime;
						}
						MoveH(speed.X * Engine.DeltaTime, onCollideH);
						MoveV(speed.Y * Engine.DeltaTime, onCollideV);
					}
					if (shaking && base.Scene.OnInterval(0.05f)) {
						sprite.X = -1 + Calc.Random.Next(3);
						sprite.Y = -1 + Calc.Random.Next(3);
					}
				} else {
					Position = returnCurve.GetPoint(Ease.CubeOut(returnEase));
					returnEase = Calc.Approach(returnEase, 1f, Engine.DeltaTime / returnDuration);
					sprite.Scale = Vector2.One * (1f + returnEase * 0.5f);
				}
				if ((base.Scene as Level).Transitioning) {
					alpha = Calc.Approach(alpha, 0f, Engine.DeltaTime * 4f);
					sprite.Color = Color.White * alpha;
				}
				sprite.Rotation += spin * Calc.ClampedMap(Math.Abs(speed.Y), 50f, 150f) * Engine.DeltaTime;
			}

			public void StopMoving() {
				Collidable = false;
			}

			public void StartShaking() {
				shaking = true;
			}

			public void ReturnHome(float duration) {
				if (base.Scene != null) {
					Camera camera = (base.Scene as Level).Camera;
					if (base.X < camera.X) {
						base.X = camera.X - 8f;
					}
					if (base.Y < camera.Y) {
						base.Y = camera.Y - 8f;
					}
					if (base.X > camera.X + 320f) {
						base.X = camera.X + 320f + 8f;
					}
					if (base.Y > camera.Y + 180f) {
						base.Y = camera.Y + 180f + 8f;
					}
				}
				returning = true;
				returnEase = 0f;
				returnDuration = duration;
				Vector2 vector = (home - Position).SafeNormalize();
				Vector2 control = (Position + home) / 2f + new Vector2(vector.Y, 0f - vector.X) * (Calc.Random.NextFloat(16f) + 16f) * Calc.Random.Facing();
				returnCurve = new SimpleCurve(Position, home, control);
			}
		}

		private const float Accel = 300f;
		private const float SlowMoveSpeed = 45f;
		private const float NormalMoveSpeed = 60f;
		private const float FastMoveSpeed = 75f;
		private const float NoSteerTime = 0.2f;
		private const float CrashTime = 0.15f;
		private const float CrashResetTime = 0.1f;

		private Directions direction;
		private Vector2 directionVector;
		private Vector2 startPosition;
		private MovementState state = MovementState.Idling;

		private float speed;
		private float accelRate = 5;
		private float targetSpeed;
		private Player noSquish;

		private List<Image> body = new List<Image>();
		private List<MTexture> arrows = new List<MTexture>();

		private Border border;
		private Color fillColor = idleBgFill;
		private float flash;
		private SoundSource moveSfx;

		public bool triggered;
		public string activationFlag;
		private Level level;

		private static readonly Color idleBgFill = Calc.HexToColor("474070");
		private static readonly Color pressedBgFill = Calc.HexToColor("30b335");
		private static readonly Color breakingBgFill = Calc.HexToColor("cc2541");

		private float particleRemainderH;
		private float particleRemainderV;
		private const float minScrapeSpeed = 10f;
		private bool cornerClipped = false;
		private float cornerClippedTimer = 0f;
		private float cornerClippedResetTime = 0.1f;

		private bool oneUse;
		private bool beganUnknown;
		private string spritePath;
		private Vector2 targetOffset = Vector2.Zero;
		private const float bounceLerpRate = 4f;
		private const float bounceLerpCutoff = 0.5f;
		private bool bounceSoundActive = false;
		private float bounceSoundTimer = 0f;
		private const float bounceSoundDurationFactor = 0.16f; 
		private const float regenWaitTime = 1f;
		private bool moveBlockHitActivated = false;
		private float moveBlockHitActivationTime = 0.3f;

		private Vector2 moveLiftSpeed = Vector2.Zero;

		public BounceMoveBlock(Vector2 position, int width, int height, Directions direction, float speed, bool oneUse, string activationFlag, string spritePath)
			: base(position, width, height, safe: false) {
			base.Depth = -1;
			startPosition = position;
			this.direction = direction;
			beganUnknown = direction == Directions.Unknown;
			targetSpeed = speed;
			this.oneUse = oneUse;
			this.spritePath = spritePath;
			this.activationFlag = activationFlag;
			directionVector = Calc.AngleToVector(-(float)direction * (float)Math.PI / 4, 1);
			if (Math.Abs(directionVector.X) < 0.5f) {
				directionVector.X = 0;
			}
			if (Math.Abs(directionVector.Y) < 0.5f) {
				directionVector.Y = 0;
			}
			int num = width / 8;
			int num2 = height / 8;
			MTexture mTexture = GFX.Game[spritePath + "/base"];
			for (int k = 0; k < num; k++) {
				for (int l = 0; l < num2; l++) {
					int num5 = (k != 0) ? ((k < num - 1) ? 1 : 2) : 0;
					int num6 = (l != 0) ? ((l < num2 - 1) ? 1 : 2) : 0;
					AddImage(mTexture.GetSubtexture(num5 * 8, num6 * 8, 8, 8), new Vector2(k, l) * 8f, 0f, new Vector2(1f, 1f), body);
				}
			}
			arrows = GFX.Game.GetAtlasSubtextures(spritePath + "/arrow");
			Add(moveSfx = new SoundSource());
			Add(new Coroutine(Controller()));
			UpdateColors();
			Add(new LightOcclude(0.5f));
		}

		public BounceMoveBlock(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Height, data.Enum("direction", Directions.Left),
				  data.Float("speed", 60f), data.Bool("oneUse", false), 
				  data.Attr("activationFlag"), data.Attr("spritePath", "objects/BounceHelper/bounceMoveBlock")) {
		}

		public override void Awake(Scene scene) {
			base.Awake(scene);
			scene.Add(border = new Border(this));
			level = scene as Level;
		}

		public void activate() {
			triggered = true;
			moveBlockHitActivated = true;
		}

		public float bounceImpact(Vector2 bounceDir, int strength) {
			if (direction == Directions.Unknown) {
				determineDirection(bounceDir);
			}

			triggered = true;
			float playerSpeedMult = 1f;
			if (state != MovementState.Breaking) {
				if (!CollideCheck<Solid>(Position + bounceDir)) {
					targetOffset += bounceDir * strength * 8;
					moveSfx.Param("arrow_stop", 1f);
					bounceSoundTimer = bounceSoundDurationFactor * (strength + 4) / 8;
					bounceSoundActive = true;
					playerSpeedMult = 0.75f;
				}
			}
			return playerSpeedMult;
		}

		private void determineDirection(Vector2 bounceDir) {
			if (bounceDir.X == 0) {
				if (bounceDir.Y > 0) {
					direction = Directions.Down;
				} else {
					direction = Directions.Up;
				}
			} else {
				if (bounceDir.X > 0) {
					direction = Directions.Right;
				} else {
					direction = Directions.Left;
				}
			}
			directionVector = bounceDir;
			if (Math.Abs(directionVector.X) < 0.5f) {
				directionVector.X = 0;
			}
			if (Math.Abs(directionVector.Y) < 0.5f) {
				directionVector.Y = 0;
			}
		}

		private IEnumerator Controller() {
			while (true) {
                #region Idle and triggering
                triggered = false;
				state = MovementState.Idling;
				while (true) {
                    if (direction != Directions.Unknown) { 
						if (triggered || HasPlayerRider()) {
							break;
						}
						if (activationFlag != "" && level.Session.GetFlag(activationFlag)) {
							level.Session.SetFlag(activationFlag, false);
							foreach (BounceMoveBlock block in level.Tracker.GetEntities<BounceMoveBlock>()) { 
								if (block.activationFlag == activationFlag) {
									block.triggered = true;
								}
							}
							break;
						}
					}
					yield return null;
				}
				Audio.Play("event:/game/04_cliffside/arrowblock_activate", Position);
				state = MovementState.Moving;
				ActivateParticles();
				StartShaking(NoSteerTime);

				float shakeTimer = NoSteerTime;
				if (moveBlockHitActivated) {
					shakeTimer = moveBlockHitActivationTime;
					moveBlockHitActivated = false;
				}
				while (true) {
					if (shakeTimer <= 0 || targetOffset != Vector2.Zero) {
						StopShaking();
						break;
					}
					shakeTimer -= Engine.DeltaTime;
					yield return null;
				}

				moveSfx.Play("event:/game/04_cliffside/arrowblock_move");
				moveSfx.Param("arrow_stop", 0f);
				StopPlayerRunIntoAnimation = false;
				#endregion

				#region Moving
				float crashTimer = 0.15f;
				float crashResetTimer = 0.1f;
				while (true) {
					speed = Calc.Approach(speed, targetSpeed, targetSpeed * accelRate * Engine.DeltaTime);
					Vector2 move = directionVector * speed * Engine.DeltaTime;

					// Bounce movement
					Vector2 offsetMove = targetOffset * bounceLerpRate * Engine.DeltaTime;
					if (targetOffset.Length() < bounceLerpCutoff) {
						offsetMove = targetOffset;
					}
					move += offsetMove;
					targetOffset -= offsetMove;

					bool hit = false;
					if (move == Vector2.Zero) {
						hit = true;
					} else {
						if (move.X != 0) {
							List<Entity> collidedSolids = CollideAll<Solid>(Position + new Vector2(Math.Sign(move.X), 0));
							if (MoveHCheck(move.X) || collidedSolids.Count != 0) {
								hit = true;
								move.X = 0;
								targetOffset.X = 0;
								foreach (Solid solid in collidedSolids) {
									if (solid is BounceMoveBlock) {
										(solid as BounceMoveBlock).activate();
									}
								}
							}
						}
						if (move.Y != 0) {
							List<Entity> collidedSolids = CollideAll<Solid>(Position + new Vector2(0, Math.Sign(move.Y)));
							if (MoveVCheck(move.Y) || collidedSolids.Count != 0) {
								hit = directionVector.X == 0 || hit;
								move.Y = 0;
								targetOffset.Y = 0;
								foreach (Solid solid in collidedSolids) {
									if (solid is BounceMoveBlock) {
										(solid as BounceMoveBlock).activate();
									}
								}
							} else {
								hit = false;
							}

							if (directionVector.Y > 0 && Top > (float)(SceneAs<Level>().Bounds.Bottom + 32)) {
								hit = true;
							}
						}
					}

					if (move.X == 0) {
						LiftSpeed.X = 0;
					}
					if (move.Y == 0) {
						LiftSpeed.Y = 0;
					}

					if (Scene.OnInterval(0.02f)) {
						Vector2 particleDir = -directionVector;
						if (move.X == 0) {
							particleDir.X = 0;
						}
						if (move.Y == 0) {
							particleDir.Y = 0;
						}
						MoveParticles(particleDir);
					}

					if (Scene.OnInterval(0.03f)) {
						Vector2 scrapeDir = directionVector;
						if (Math.Abs(move.X) < minScrapeSpeed * Engine.DeltaTime) {
							scrapeDir.Y = 0;
						}
						if (Math.Abs(move.Y) < minScrapeSpeed * Engine.DeltaTime) {
							scrapeDir.X = 0;
						}
						ScrapeParticles(scrapeDir);
					}

					if (hit) {
						moveLiftSpeed = Vector2.Zero;
						moveSfx.Param("arrow_stop", 1f);
						if (bounceSoundActive) {
							bounceSoundTimer = 0;
							bounceSoundActive = false;
						}
						crashResetTimer = CrashResetTime;
						if (crashTimer <= 0f) {
							break;
						}
						crashTimer -= Engine.DeltaTime;
					} else {
						moveLiftSpeed = LiftSpeed;
						if (!bounceSoundActive) {
							moveSfx.Param("arrow_stop", 0f);
						}
						if (crashResetTimer > 0f) {
							crashResetTimer -= Engine.DeltaTime;
						} else {
							crashTimer = CrashTime;
						}
					}
					Level level = Scene as Level;
					if (Left < (float)level.Bounds.Left || Top < (float)level.Bounds.Top || Right > (float)level.Bounds.Right) {
						break;
					}
					yield return null;
				}
                #endregion

                #region Breaking and reforming
                Audio.Play("event:/game/04_cliffside/arrowblock_break", Position);
				moveSfx.Stop();
				state = MovementState.Breaking;
				speed = 0;
				StartShaking(0.2f);
				StopPlayerRunIntoAnimation = true;
				yield return 0.2f;
				BreakParticles();
				List<Debris> debris = new List<Debris>();
				for (int x = 0; (float)x < Width; x += 8) {
					for (int y = 0; (float)y < Height; y += 8) {
						Vector2 offset = new Vector2((float)x + 4f, (float)y + 4f);
						Debris d = Engine.Pooler.Create<Debris>().Init(spritePath, Position + offset, Center, startPosition + offset);
						debris.Add(d);
						Scene.Add(d);
					}
				}
				MoveStaticMovers(startPosition - Position);
				DisableStaticMovers();
				Position = startPosition;
				Visible = (Collidable = false);
				if (oneUse) {
					yield break;
				}

				yield return regenWaitTime;
				foreach (Debris d2 in debris) {
					d2.StopMoving();
				}
				while (CollideCheck<Actor>() || CollideCheck<Solid>()) {
					yield return null;
				}
				Collidable = true;
				EventInstance sound = Audio.Play("event:/game/04_cliffside/arrowblock_reform_begin", debris[0].Position);
				Coroutine component;
				Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(sound, debris));
				Add(component);
				foreach (Debris d4 in debris) {
					d4.StartShaking();
				}
				yield return 0.2f;
				foreach (Debris d5 in debris) {
					d5.ReturnHome(0.65f);
				}
				yield return 0.6f;
				routine.RemoveSelf();
				foreach (Debris d3 in debris) {
					d3.RemoveSelf();
				}
				Audio.Play("event:/game/04_cliffside/arrowblock_reappear", Position);
				Visible = true;
				EnableStaticMovers();
				speed = 0;
				targetOffset = Vector2.Zero;
				if (beganUnknown) {
					direction = Directions.Unknown;
				}
				noSquish = null;
				fillColor = idleBgFill;
				UpdateColors();
				flash = 1f;
                #endregion
            }
        }

		private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<Debris> debris) {
			while (true) {
				instance.getPlaybackState(out PLAYBACK_STATE state);
				if (state == PLAYBACK_STATE.STOPPED) {
					break;
				}
				Vector2 center = Vector2.Zero;
				foreach (Debris d in debris) {
					center += d.Position;
				}
				center /= (float)debris.Count;
				Audio.Position(instance, center);
				yield return null;
			}
		}

		public override void Update() {
			base.Update();
			LiftSpeed = moveLiftSpeed;
			if (moveSfx != null && moveSfx.Playing) {
				float num = (directionVector * new Vector2(-1f, 1f)).Angle();
				int num2 = (int)Math.Floor((0f - num + (float)Math.PI * 2f) % ((float)Math.PI * 2f) / ((float)Math.PI * 2f) * 8f + 0.5f);
				moveSfx.Param("arrow_influence", num2 + 1);

				if (bounceSoundActive) {
					bounceSoundTimer -= Engine.DeltaTime;
					if (bounceSoundTimer <= 0) {
						moveSfx.Param("arrow_stop", 0f);
						bounceSoundActive = false;
					}
				}

				if (cornerClipped) {
					cornerClippedTimer -= Engine.DeltaTime;
					if (cornerClippedTimer <= 0) {
						cornerClipped = false;
					}
				}
			}
			border.Visible = Visible;
			flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 5f);
			UpdateColors();
		}

		public override void OnStaticMoverTrigger(StaticMover sm) {
			triggered = true;
		}

		public override void MoveHExact(int move) {
			if (noSquish != null && ((move < 0 && noSquish.X < base.X) || (move > 0 && noSquish.X > base.X))) {
				while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitX * move)) {
					move -= Math.Sign(move);
				}
			}
			base.MoveHExact(move);
		}

		public override void MoveVExact(int move) {
			if (noSquish != null && move < 0 && noSquish.Y <= base.Y) {
				while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + Vector2.UnitY * move)) {
					move -= Math.Sign(move);
				}
			}
			base.MoveVExact(move);
		}

		private bool MoveHCheck(float move) {
			if (MoveHCollideSolids(move, thruDashBlocks: false)) {
				if (!cornerClipped) {
					for (int i = 1; i <= 3; i++) {
						for (int num = 1; num >= -1; num -= 2) {
							Vector2 value = new Vector2(Math.Sign(move), i * num);
							if (!CollideCheck<Solid>(Position + value)) {
								int offset = i * num;
								MoveVExact(offset);
								if (targetOffset.Y != 0) {
									targetOffset.Y -= offset;
								}
								MoveHExact(Math.Sign(move));
								cornerClipped = true;
								cornerClippedTimer = cornerClippedResetTime;
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		private bool MoveVCheck(float move) {
			if (MoveVCollideSolids(move, thruDashBlocks: false)) {
				if (!cornerClipped) {
					for (int j = 1; j <= 3; j++) {
						for (int num2 = 1; num2 >= -1; num2 -= 2) {
							Vector2 value2 = new Vector2(j * num2, Math.Sign(move));
							if (!CollideCheck<Solid>(Position + value2)) {
								int offset = j * num2;
								MoveHExact(offset);
								if (targetOffset.X != 0) {
									targetOffset.X -= offset;
								}
								MoveVExact(Math.Sign(move));
								cornerClipped = true;
								cornerClippedTimer = cornerClippedResetTime;
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		private void UpdateColors() {
			Color value = idleBgFill;
			if (state == MovementState.Moving) {
				value = pressedBgFill;
			} else if (state == MovementState.Breaking) {
				value = breakingBgFill;
			}
			fillColor = Color.Lerp(fillColor, value, 10f * Engine.DeltaTime);
		}

		private void AddImage(MTexture tex, Vector2 position, float rotation, Vector2 scale, List<Image> addTo) {
			Image image = new Image(tex);
			image.Position = position + new Vector2(4f, 4f);
			image.CenterOrigin();
			image.Rotation = rotation;
			image.Scale = scale;
			Add(image);
			addTo?.Add(image);
		}

		public override void Render() {
			Vector2 position = Position;
			Position += base.Shake;
			Draw.Rect(base.X + 3f, base.Y + 3f, base.Width - 6f, base.Height - 6f, fillColor);
			foreach (Image item4 in body) {
				item4.Render();
			}
			Draw.Rect(base.Center.X - 4f, base.Center.Y - 4f, 8f, 8f, fillColor);
			if (state == MovementState.Breaking) {
				GFX.Game[spritePath + "/x"].DrawCentered(base.Center);
			} else if (direction == Directions.Unknown) {
				GFX.Game[spritePath + "/unknown"].DrawCentered(base.Center);
			} else {
				MTexture mTexture = arrows[(int)direction];
				mTexture.DrawCentered(base.Center);
			}
			float num = flash * 4f;
			Draw.Rect(base.X - num, base.Y - num, base.Width + num * 2f, base.Height + num * 2f, Color.White * flash);
			Position = position;
		}

		private void ActivateParticles() {
			if (directionVector.X >= 0) {
				SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int)(Height / 2f), CenterLeft, Vector2.UnitY * (Height - 4f) * 0.5f, (float)Math.PI);
			}
			if (directionVector.X <= 0) {
				SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int)(Height / 2f), CenterRight, Vector2.UnitY * (Height - 4f) * 0.5f, 0f);
			}
			if (directionVector.Y >= 0) {
				SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int)(Width / 2f), TopCenter, Vector2.UnitX * (Width - 4f) * 0.5f, -(float)Math.PI / 2f);
			}
			if (directionVector.Y <= 0) {
				SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int)(Width / 2f), BottomCenter, Vector2.UnitX * (Width - 4f) * 0.5f, (float)Math.PI / 2f);
			}
		}

		private void BreakParticles() {
			Vector2 center = base.Center;
			for (int i = 0; (float)i < base.Width; i += 4) {
				for (int j = 0; (float)j < base.Height; j += 4) {
					Vector2 vector = Position + new Vector2(2 + i, 2 + j);
					SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 1, vector, Vector2.One * 2f, (vector - center).Angle());
				}
			}
		}

		private void MoveParticles(Vector2 dir) {
			Vector2 positionH = Vector2.Zero;
			Vector2 positionV = Vector2.Zero;
			float amountH = Width / 32f;
			float amountV = Height / 32f;
			Vector2 positionRangeH = Vector2.UnitX * ((Width - 4f) / 2);
			Vector2 positionRangeV = Vector2.UnitY * ((Height - 4f) / 2);
			if (dir.X < 0) {
				positionH = CenterLeft + Vector2.UnitX;
			} else if (dir.X > 0) {
				positionH = CenterRight;
			}
			if (dir.Y < 0) {
				positionV = TopCenter + Vector2.UnitY;
			} else if (dir.Y > 0) {
				positionV = BottomCenter;
			}
			
			if (dir.X != 0 && dir.Y != 0) {
				amountH /= (float)Math.Sqrt(2);
				amountV /= (float)Math.Sqrt(2);
			}

			if (dir.X != 0) {
				particleRemainderH += amountH;
				int amountHRounded = (int)particleRemainderH;
				particleRemainderH -= amountHRounded;
				if (amountHRounded > 0) {
					SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, amountHRounded, positionH, positionRangeV, dir.Angle());
				}
			}
			if (dir.Y != 0) {
				particleRemainderV += amountV;
				int amountVRounded = (int)particleRemainderV;
				particleRemainderV -= amountVRounded;
				if (amountVRounded > 0) {
					SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, amountVRounded, positionV, positionRangeH, dir.Angle());
				}
			}
		}

		private void ScrapeParticles(Vector2 dir) {
			bool collidable = Collidable;
			Collidable = false;
			if (dir.X != 0f) {
				float x = (!(dir.X > 0f)) ? (base.Left - 1f) : base.Right;
				for (int i = 0; (float)i < base.Height; i += 8) {
					Vector2 vector = new Vector2(x, base.Top + 4f + (float)i);
					if (base.Scene.CollideCheck<Solid>(vector)) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector);
					}
				}
			} 
			if (dir.Y != 0){
				float y = (!(dir.Y > 0f)) ? (base.Top - 1f) : base.Bottom;
				for (int j = 0; (float)j < base.Width; j += 8) {
					Vector2 vector2 = new Vector2(base.Left + 4f + (float)j, y);
					if (base.Scene.CollideCheck<Solid>(vector2)) {
						SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector2);
					}
				}
			}
			Collidable = true;
		}
	}
}

