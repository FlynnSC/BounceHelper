using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.BounceHelper {
	[CustomEntity("BounceHelper/BounceJellyfish")]
	[Tracked]
	public class BounceJellyfish : Actor {
		public Vector2 Speed;
		public Vector2 beforeSpeed = Vector2.Zero;

		public Holdable Hold;
		private Level level;

		private Collision onCollideH;
		private Collision onCollideV;

		private Vector2 prevLiftSpeed;
		private Vector2 startPos;

		private float noGravityTimer;

		private bool platform;
		public bool destroyed;

		private Sprite sprite;
		private Wiggler wiggler;
		private SineWave platformSine;
		private SoundSource fallingSfx;

		private Player player;

		private const float throwSpeed = 130f;
		private const float minBounceSpeed = 30f;
		private const float floorDeceleration = 100f;
		private const float maxFallSpeed = 30f;
		private const float airDecelerationX = 80f;
		private const float airDecelerationY = 150f;

		private int baseDashCount;
		private int dashes = 1;
		public bool dashing = false;
		public Vector2 dashDir;
		private bool wasRedDash = false;
		public bool soulBound = true;
		private bool ezelMode;
		public bool matchPlayerDash;

		private float dashBufferTimer = 0f;
		private const float dashBufferTime = 0.08f;
		private const float dashTime = 0.15f;
		private const float dashSpeed = 240f;
		private const float endDashSpeed = throwSpeed;
		public float dashAttackTimer = 0f;
		private const float dashAttackTime = 0.3f;

		private float dashCooldownTimer = 0f;
		private const float dashCooldownTime = 0.2f;
		private float dashRefillCooldownTimer = 0f;
		private const float dashRefillCooldownTime = 0.1f;
		
		private const float bounceSpeedMult = 1.2f;
		private bool speedRings = false;
		private float speedRingsTimer = 0f;
		private const float minSpeedRingsSpeed = 140f;

		public float boostTimer = 0f;
		private const float boostTime = 0.55f;
		public Vector2 boostDir = Vector2.Zero;

		private static Color[] dashColors = new Color[] { Player.UsedHairColor, Player.NormalHairColor, Player.TwoDashesHairColor };
		private static Color bounceColor = BounceHelperModule.bounceColor;
		private static char[] spriteSuffixes = new char[] { 'B', 'R', 'P', 'F', 'H' };
		private float flashTimer = 0f;
		private const float flashTime = 0.12f;

		private Collider refillCollider = new Hitbox(20f, 17f, -12f, -17f);

		private Vector2 effectCentre {
			get { return new Vector2(Center.X, Center.Y - 5f); }
		}

		public static ParticleType[] glideParticles = new ParticleType[3];
		public static ParticleType[] glideUpParticles = new ParticleType[3];
		public static ParticleType[] glowParticles = new ParticleType[3];
		public static ParticleType[] expandParticles = new ParticleType[3];

		static BounceJellyfish() {
			Color[] particleColors = new Color[] { new Color(79, 255, 243), new Color(255, 133, 133), new Color(255, 133, 233) };
			Color[] particleColorsLight = new Color[] { new Color(183, 243, 255), new Color(255, 199, 199), new Color(255, 199, 235) };

			for (int i = 0; i < 3; ++i) {
				glideParticles[i] = new ParticleType(Glider.P_Glide);
				glideParticles[i].Color = particleColors[i];

				glideUpParticles[i] = new ParticleType(Glider.P_GlideUp);
				glideUpParticles[i].Color = particleColors[i];

				glowParticles[i] = new ParticleType(Glider.P_Glow);
				glowParticles[i].Color = particleColorsLight[i];

				expandParticles[i] = new ParticleType(Glider.P_Expand);
				expandParticles[i].Color = particleColorsLight[i];
			}
		}

		public BounceJellyfish(Vector2 position, bool platform, bool soulBound, int baseDashCount, bool ezelMode, bool matchPlayerDash)
			: base(position) {
			this.soulBound = soulBound;
			this.baseDashCount = baseDashCount;
			dashes = baseDashCount;
			this.platform = platform;
			this.ezelMode = ezelMode;
			this.matchPlayerDash = matchPlayerDash;
			startPos = Position;
			base.Collider = new Hitbox(8f, 10f, -4f, -10f);
			onCollideH = OnCollideH;
			onCollideV = OnCollideV;
			Add(sprite = BounceHelperModule.spriteBank.Create("bounceJellyfish"));
			Add(wiggler = Wiggler.Create(0.25f, 4f));
			base.Depth = -5;
			Add(Hold = new Holdable(0.3f));
			Hold.PickupCollider = new Hitbox(20f, 22f, -10f, -16f);
			Hold.SlowFall = true;
			Hold.SlowRun = false;
			Hold.OnPickup = OnPickup;
			Hold.OnRelease = OnRelease;
			Hold.SpeedGetter = () => Speed;
			Hold.OnHitSpring = HitSpring;
			platformSine = new SineWave(0.3f, 0f);
			Add(platformSine);
			fallingSfx = new SoundSource();
			Add(fallingSfx);
			Add(new WindMover(WindMode));
		}

		public BounceJellyfish(EntityData e, Vector2 offset)
			: this(e.Position + offset, e.Bool("platform", true), e.Bool("soulBound", true), e.Int("baseDashCount", 1), 
				  e.Bool("ezelMode"), e.Bool("matchPlayerDash")) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			updateAnimationColor();
			level = SceneAs<Level>();
			foreach (BounceJellyfish entity in level.Tracker.GetEntities<BounceJellyfish>()) {
				if (entity != this && entity.Hold.IsHeld && soulBound) {
					RemoveSelf();
				}
			}
		}

		public void bufferDash() {
			//BounceHelperModule.Settings.JellyfishDash.ConsumePress();
			dashBufferTimer = dashBufferTime;
		}

		private IEnumerator dashCoroutine() {
			dashing = true;
			--dashes;
			wasRedDash = dashes == 0;
			updateAnimationColor();

			var player = getPlayer();
			if (player == null) { 
				yield break;
			}
			var playerData = new DynData<Player>(player);
			dashDir = playerData.Get<Vector2>("lastAim");
			Speed = dashSpeed * dashDir;
			boostTimer = boostTime;
			boostDir = dashDir;

			dashCooldownTimer = dashCooldownTime;
			dashRefillCooldownTimer = dashRefillCooldownTime;
			flashTimer = flashTime;
			dashAttackTimer = dashAttackTime;
			if (platform) {
				disintegratePlatform();
			}

			SlashFx.Burst(effectCentre, dashDir.Angle());
			level.Displacement.AddBurst(effectCentre, 0.4f, 8f, 64f, 0.5f, Ease.QuadOut, Ease.QuadOut);
			createTrail();

			string soundId;
			if (dashDir.Y < 0f || (dashDir.Y == 0f && dashDir.X > 0f)) {
				soundId = dashes == 1 ? "dash_pink_right" : "dash_red_right";
			} else {
				soundId = dashes == 1 ? "dash_pink_left" : "dash_red_left";
			}
			Audio.Play("event:/char/madeline/" + soundId, Position);

			foreach (BounceSwapBlock swapBlock in Scene.Tracker.GetEntities<BounceSwapBlock>()) {
				if (swapBlock.moon) {
					swapBlock.OnDash(Vector2.Zero);
                }
            }
			yield return dashTime / 2;

			if (dashing) {
				createTrail();
				yield return dashTime / 2;
			} else {
				yield return null;
            }
			

			if (dashing) {
				createTrail();
				Speed = dashDir * endDashSpeed;
				dashing = false;
			}
		}

		public override void Update() {

			#region Audio and particle stuff
            if (Scene.OnInterval(0.05f) && !destroyed) {
				level.Particles.Emit(glowParticles[dashes], 1, base.Center + Vector2.UnitY * -9f, new Vector2(10f, 4f));
			}
			float target = (!Hold.IsHeld) ? 0f : ((!Hold.Holder.OnGround()) ? Calc.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, (float)Math.PI / 3f, -(float)Math.PI / 3f) : Calc.ClampedMap(Hold.Holder.Speed.X, -300f, 300f, 0.6981317f, -0.6981317f));
			sprite.Rotation = Calc.Approach(sprite.Rotation, target, (float)Math.PI * Engine.DeltaTime);
			if (Hold.IsHeld && !Hold.Holder.OnGround() && isFalling()) {
				if (!fallingSfx.Playing) {
					Audio.Play("event:/new_content/game/10_farewell/glider_engage", Position);
					fallingSfx.Play("event:/new_content/game/10_farewell/glider_movement");
				}
				Vector2 speed = Hold.Holder.Speed;
				Vector2 vector = new Vector2(speed.X * 0.5f, (speed.Y < 0f) ? (speed.Y * 2f) : speed.Y);
				float value = Calc.Map(vector.Length(), 0f, 120f, 0f, 0.7f);
				fallingSfx.Param("glider_speed", value);
			} else {
				fallingSfx.Stop();
			}
            #endregion

            base.Update();
			if (!destroyed) {

                #region Seeker barrier stuff
                foreach (SeekerBarrier entity in base.Scene.Tracker.GetEntities<SeekerBarrier>()) {
					entity.Collidable = true;
					bool flag = CollideCheck(entity);
					entity.Collidable = false;
					if (flag) {
						destroyed = true;
						Collidable = false;
						if (Hold.IsHeld) {
							Vector2 speed2 = Hold.Holder.Speed;
							Hold.Holder.Drop();
							Speed = speed2 * 0.333f;
							Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
						}
						killPlayer();
						Add(new Coroutine(DestroyAnimationRoutine()));
						return;
					}
				}
				#endregion

				#region Dash activation + timer stuff + speed rings
				//if (BounceHelperModule.Settings.JellyfishDash.Pressed) {
				//	bufferDash();
				//}
				if (dashBufferTimer > 0f && dashes > 0 && !Hold.IsHeld && dashCooldownTimer <= 0) {
					Add(new Coroutine(dashCoroutine()));
				}

				if (flashTimer > 0) {
					flashTimer -= Engine.DeltaTime;
					if (flashTimer <= 0) {
						updateAnimationColor();
					}
				}
				if (dashCooldownTimer > 0) {
					dashCooldownTimer -= Engine.DeltaTime;
				}
				if (dashRefillCooldownTimer > 0) {
					dashRefillCooldownTimer -= Engine.DeltaTime;
				}
				if (boostTimer > 0) {
					boostTimer -= Engine.DeltaTime;
				}
				if (dashAttackTimer > 0) {
					dashAttackTimer -= Engine.DeltaTime;
				}
				if (dashBufferTimer > 0) {
					dashBufferTimer -= Engine.DeltaTime;
				}

				if (speedRings) {
					if (Speed.Length() < minSpeedRingsSpeed)
						speedRings = false;
					else {
						var was = speedRingsTimer;
						speedRingsTimer += Engine.DeltaTime;

						if (speedRingsTimer >= 0.5f) {
							speedRings = false;
							speedRingsTimer = 0f;
						} else if (Calc.OnInterval(speedRingsTimer, was, 0.15f)) {
							level.Add(Engine.Pooler.Create<SpeedRing>().Init(effectCentre, Speed.Angle(), Color.White));
						}
					}
				} else {
					speedRingsTimer = 0;
				}
				#endregion

				#region Refill checking
				Collider baseCollider = base.Collider;
				base.Collider = refillCollider;
				foreach (BounceRefill refill in CollideAll<BounceRefill>()) {
					int refillDashes = Math.Max(refill.dashes, baseDashCount);
					if (dashes < refillDashes) {
						refillDash(refillDashes);
						refill.use(Speed.Angle());
                    }
                }
				base.Collider = baseCollider;
                #endregion

                if (Hold.IsHeld) {
					prevLiftSpeed = Vector2.Zero;
				} else if (!platform) {
					if (dashing) {

						#region Dash stuff
						if (Speed != Vector2.Zero && level.OnInterval(0.02f)) {
							level.ParticlesFG.Emit(dashes == 1 ? Player.P_DashB : Player.P_DashA, Center + Calc.Random.Range(Vector2.One * -2, Vector2.One * 2), dashDir.Angle());
						}

						if (OnGround() && dashRefillCooldownTimer <= 0f) {
							refillDash();
						}
                        #endregion

                    } else if (OnGround()) {

                        #region On ground stuff
                        float target2 = (!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f));
						Speed.X = Calc.Approach(Speed.X, target2, floorDeceleration * Engine.DeltaTime);
						Vector2 liftSpeed = base.LiftSpeed;
						if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero) {
							Speed = prevLiftSpeed;
							prevLiftSpeed = Vector2.Zero;
							Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
							if (Speed.X != 0f && Speed.Y == 0f) {
								Speed.Y = -60f;
							}
							if (Speed.Y < 0f) {
								noGravityTimer = 0.15f;
							}
						} else {
							prevLiftSpeed = liftSpeed;
							if (liftSpeed.Y < 0f && Speed.Y < 0f) {
								Speed.Y = 0f;
							}
						}

						if (dashRefillCooldownTimer <= 0f) {
							refillDash();
						}

						List<Entity> solids = CollideAll<Solid>(Position + Vector2.UnitY);
						foreach (Solid solid in solids) {
							if (solid is BounceZipMover) {
								var mover = solid as BounceZipMover;
								if (mover.moon) {
									mover.activate();
								}
                            }
                        }
                        #endregion

                    } else if (Hold.ShouldHaveGravity) {

                        #region In air stuff
                        Speed.X = Calc.Approach(Speed.X, 0f, airDecelerationX * Engine.DeltaTime);
						if (noGravityTimer > 0f) {
							noGravityTimer -= Engine.DeltaTime;
						} else {
							Speed.Y = Calc.Approach(Speed.Y, level.Wind.Y < 0f ? 0f : maxFallSpeed, airDecelerationY * Engine.DeltaTime);
						}
                        #endregion
                    }

                    #region Movement and level bounds collision
                    MoveH(Speed.X * Engine.DeltaTime, onCollideH);
					MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
					CollisionData data;
					if (base.Left < (float)level.Bounds.Left) {
						base.Left = level.Bounds.Left;
						data = new CollisionData {
							Direction = -Vector2.UnitX
						};
						OnCollideH(data);
					} else if (base.Right > (float)level.Bounds.Right) {
						base.Right = level.Bounds.Right;
						data = new CollisionData {
							Direction = Vector2.UnitX
						};
						OnCollideH(data);
					}
					if (base.Top < (float)level.Bounds.Top) {
						base.Top = level.Bounds.Top;
						data = new CollisionData {
							Direction = -Vector2.UnitY
						};
						OnCollideV(data);
					} else if (base.Top > (float)(level.Bounds.Bottom + 16)) {
						killPlayer();
						RemoveSelf();
						return;
					}
					Hold.CheckAgainstColliders();
                    #endregion

                } else {
					Position = startPos + Vector2.UnitY * platformSine.Value * 1f;
				}

                #region Sprite and particle stuff
                Vector2 one = Vector2.One;
				if (!Hold.IsHeld) {
					if (level.Wind.Y < 0f) {
						PlayOpen();
					} else {
						spritePlay("idle");
					}
				} else if (Hold.Holder.Speed.Y > 20f || level.Wind.Y < 0f) {
					if (level.OnInterval(0.04f)) {
						if (level.Wind.Y < 0f) {
							level.ParticlesBG.Emit(glideUpParticles[dashes], 1, Position - Vector2.UnitY * 20f, new Vector2(6f, 4f));
						} else {
							//level.ParticlesBG.Emit(Glider.glideParticles[dashes], 1, Position - Vector2.UnitY * 10f, new Vector2(6f, 4f));
							level.ParticlesBG.Emit(glideParticles[dashes], 1, Position - Vector2.UnitY * 10f, new Vector2(6f, 4f));
						}
					}
					PlayOpen();
					if (Input.GliderMoveY.Value > 0) {
						one.X = 0.7f;
						one.Y = 1.4f;
					} else if (Input.GliderMoveY.Value < 0 && level.Wind.Y < 0f) {
						one.X = 1.2f;
						one.Y = 0.8f;
					}
					Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
				} else {
					spritePlay("held");
				}
				sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, one.Y, Engine.DeltaTime * 2f);
				sprite.Scale.X = Calc.Approach(sprite.Scale.X, (float)Math.Sign(sprite.Scale.X) * one.X, Engine.DeltaTime * 2f);
                #endregion

            } else {
				Position += Speed * Engine.DeltaTime;
			}
		}

		private void PlayOpen() {
			if (!isFalling()) {
				spritePlay("fall");
				sprite.Scale = new Vector2(1.5f, 0.6f);
				level.Particles.Emit(expandParticles[dashes], 16, base.Center + (Vector2.UnitY * -12f).Rotate(sprite.Rotation), new Vector2(8f, 3f), -(float)Math.PI / 2f + sprite.Rotation);
				if (Hold.IsHeld) {
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
				}
			}
		}

		public override void Render() {
			if (!destroyed) {
				sprite.DrawSimpleOutline();
			}
			base.Render();
			if (platform) {
				for (int i = 0; i < 24; i++) {
					Draw.Point(Position + PlatformAdd(i), PlatformColor(i));
				}
			}
		}

		private void WindMode(Vector2 wind) {
			if (!Hold.IsHeld) {
				if (wind.X != 0f) {
					MoveH(wind.X * 0.5f);
				}
				if (wind.Y != 0f) {
					MoveV(wind.Y);
				}
			}
		}

		private Vector2 PlatformAdd(int num) {
			return new Vector2(-12 + num, -5 + (int)Math.Round(Math.Sin(base.Scene.TimeActive + (float)num * 0.2f) * 1.7999999523162842));
		}

		private Color PlatformColor(int num) {
			if (num <= 1 || num >= 22) {
				return Color.White * 0.4f;
			}
			return Color.White * 0.8f;
		}

		private void bounce(Vector2 direction, Platform hit) {
			dashing = false;
			boostTimer = boostTime;
			boostDir = Vector2.Normalize(Speed);
			Speed *= bounceSpeedMult;

			if (hit is BounceSwapBlock) {
				BounceSwapBlock swapBlock = hit as BounceSwapBlock;
				if (swapBlock.moon && swapBlock.onBounce(Speed.Angle())) {
					// Allows jellies with a base dash count of 0 to still steal the essence if they have a dash
					// from a refill
					refillDash(Math.Max(baseDashCount, 1));
                }
            }

			speedRings = true;
			Vector2 slashOffset = Vector2.Normalize(Speed) * 12;
			SlashFx.Burst(effectCentre + slashOffset, Speed.Angle());
			createTrail(bounce: true);
		}

		private void OnCollideH(CollisionData data) {
			if (data.Hit is DashSwitch) {
				(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
			}
			if (Speed.X < 0f) {
				Audio.Play("event:/new_content/game/10_farewell/glider_wallbounce_left", Position);
			} else {
				Audio.Play("event:/new_content/game/10_farewell/glider_wallbounce_right", Position);
			}
			sprite.Scale = new Vector2(0.8f, 1.2f);

			if (data.Hit != null) {
				if (Math.Abs(Speed.X) > minBounceSpeed) {
					Speed.X *= -1f;
				} else {
					Speed.X = 0;
                }
				if (!(data.Hit is DashSwitch)) {
					Speed += data.Hit.LiftSpeed;
				}

				if (dashing) {
					bounce(data.Direction, data.Hit);
				}

				if (data.Hit is BounceZipMover) {
					var mover = data.Hit as BounceZipMover;
					if (mover.moon) {
						mover.activate();
					}
				}
			} else {
				Speed.X = 0;
			}
		}

		private void OnCollideV(CollisionData data) {
			if (Math.Abs(Speed.Y) > 8f) {
				sprite.Scale = new Vector2(1.2f, 0.8f);
				Audio.Play("event:/new_content/game/10_farewell/glider_land", Position);
			}
			
			if (data.Hit != null) {
				if (data.Direction.Y > 0 && Speed.Y < minBounceSpeed) {
					Speed.Y = 0;
				}
				Speed.Y *= -1f;
				Speed += data.Hit.LiftSpeed; // This might act wierd?

				if (dashing) {
					bounce(data.Direction, data.Hit);
				}

				if (data.Hit is BounceZipMover) {
					var mover = data.Hit as BounceZipMover;
					if (mover.moon) {
						mover.activate();
					}
				}
			} else {
				Speed.Y = 0;
			}
		}

		private void disintegratePlatform() {
			for (int i = 0; i < 24; i++) {
				level.Particles.Emit(Glider.P_Platform, Position + PlatformAdd(i), PlatformColor(i));
			}
			platform = false;
		}

		private void OnPickup() {
			if (platform) {
				disintegratePlatform();
			}
			AllowPushing = false;
			beforeSpeed = Speed;
			Speed = Vector2.Zero;
			AddTag(Tags.Persistent);
			wiggler.Start();
			dashing = false;
		}

		private void OnRelease(Vector2 force) {
			if (force == Vector2.Zero) {
				Audio.Play("event:/new_content/char/madeline/glider_drop", Position);
			}
			AllowPushing = true;
			wiggler.Start();
			RemoveTag(Tags.Persistent);

			Vector2 playerSpeed = getPlayer().Speed;
			Vector2 throwImpulse = force * throwSpeed;
			Speed = playerSpeed + throwImpulse;
			if (Speed.Length() > playerSpeed.Length() && Speed.Length() > throwSpeed) {
				Speed = Calc.AngleToVector(Speed.Angle(), Math.Max(playerSpeed.Length(), throwSpeed));
			}
		}

		public override void OnSquish(CollisionData data) {
			if (!TrySquishWiggle(data)) {
				die();
			}
		}

		public bool HitSpring(Spring spring) {
			bool hit = false;
			if (!Hold.IsHeld) {
				if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f) {
					Speed.X *= 0.5f;
					Speed.Y = -160f;
					noGravityTimer = 0.15f;
					hit = true;
				}
				if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f) {
					MoveTowardsY(spring.CenterY + 5f, 4f);
					Speed.X = 160f;
					Speed.Y = -80f;
					noGravityTimer = 0.1f;
					hit = true;
				}
				if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f) {
					MoveTowardsY(spring.CenterY + 5f, 4f);
					Speed.X = -160f;
					Speed.Y = -80f;
					noGravityTimer = 0.1f;
					hit = true;
				}

				if (hit) {
					wiggler.Start();
					refillDash();
					dashing = false;
				}
			}
			return hit;
		}

		private IEnumerator DestroyAnimationRoutine() {
			Audio.Play("event:/new_content/game/10_farewell/glider_emancipate", Position);
			if (dashing) {
				dashing = false;
				Speed = dashDir * endDashSpeed;
			}
			flashTimer = 0f;
			spritePlay("death");
			yield return 1f;
			RemoveSelf();
		}

		public void die(bool playSound = true) {
			destroyed = true;
			Collidable = false;
			dashing = false;
			killPlayer();
			if (playSound) {
				Audio.Play("event:/char/madeline/death", Position);
			}
			Add(new DeathEffect(dashColors[dashes], Center - Position));
			sprite.Visible = false;
			Depth = -1000000;
			Speed = Vector2.Zero;
			AllowPushing = false;

			if (platform) {
				for (int i = 0; i < 24; i++) {
					level.Particles.Emit(Glider.P_Platform, Position + PlatformAdd(i), PlatformColor(i));
				}
			}
			platform = false;
		}

		private void killPlayer() {
			player = getPlayer();
			if (player != null && !player.Dead && soulBound) {
				player.Die(-Vector2.UnitY);
			}
		}

		// Default value of -1 indicates refill dashes to base dash count
		public void refillDash(int count = -1) {
			if (count == -1) {
				count = baseDashCount;
			}

			if (count > dashes) {
				dashes = count;
				flashTimer = flashTime;
				updateAnimationColor();
			}
		}

		private bool isFalling() {
			return sprite.CurrentAnimationID.TrimEnd(spriteSuffixes) == "fall" || 
				sprite.CurrentAnimationID.TrimEnd(spriteSuffixes) == "fallLoop";
		}

		private void spritePlay(string name) {
			string suffix = flashTimer > 0 ? "F" : spriteSuffixes[dashes].ToString();
			if (ezelMode && (suffix == "R" || suffix == "P")) suffix += 'H';
			sprite.Play(name + suffix);
		}

		private void updateAnimationColor() {
			int frame = sprite.CurrentAnimationFrame;
			var spriteData = new DynData<Sprite>(sprite);
			float animationTimer = spriteData.Get<float>("animationTimer");
			spritePlay(sprite.CurrentAnimationID.TrimEnd(spriteSuffixes));
			sprite.SetAnimationFrame(frame);
			spriteData["animationTimer"] = animationTimer;
		}

		private void createTrail(bool bounce = false) {
			Vector2 scale = new Vector2(Math.Abs(sprite.Scale.X), sprite.Scale.Y);
			Color color = bounce ? bounceColor : dashColors[wasRedDash ? 0 : 1];
			TrailManager.Add(this, scale, color);
		}

		private Player getPlayer() {
			if (player == null) {
				player = level.Tracker.GetEntity<Player>();
			}
			return player;
		}
	}
}
