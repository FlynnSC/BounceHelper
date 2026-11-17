using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.BounceHelper;

public static class CollideCheckHelper
{
    public static Entity FindSolidOrJumpthru(this Entity entity, Vector2 direction)
    {
        Vector2 position = entity.Position + direction;
        if (entity.CollideFirst<Solid>(position) is Entity result)
        {
            return result;
        }

        if (direction == Vector2.Zero)
        {
            return null;
        }

        Direction expectedJumpThruDir = GetDir(direction);
        foreach (Type jumpThruType in jumpThruTypes)
        {
            if (jumpThruTypeDirections.ContainsKey(jumpThruType) && (jumpThruTypeDirections[jumpThruType] & expectedJumpThruDir) == Direction.Zero)
            {
                continue;
            }

            foreach (Entity jumpThru in Collide.All(entity, entity.Scene.Tracker.Entities[jumpThruType], position))
            {
                if ((GetDirection(jumpThru) & expectedJumpThruDir) != Direction.Zero && !entity.CollideCheck(jumpThru))
                {
                    return jumpThru;
                }
            }
        }
        return null;
    }

    public static void Initialize()
    {
        jumpThruTypeDirections.Clear();
        jumpThruTypes.Clear();
        jumpThruTypes.Add(typeof(JumpThru));

        if (ReflectionUtils.GetType("GravityHelper", "Celeste.Mod.GravityHelper.Entities.UpsideDownJumpThru") is { } gravity)
        {
            jumpThruTypeDirections[gravity] = Direction.Up;
        }
        if (ReflectionUtils.GetType("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru") is { } maddieupsidedown)
        {
            jumpThruTypeDirections[maddieupsidedown] = Direction.Up;
            // maddie hooks Tracker.GetEntities<JumpThru>() so you can't get UpsideDownJumpThru when searching JumpThru
            jumpThruTypes.Add(maddieupsidedown);
        }
        if (ReflectionUtils.GetType("VivHelper", "VivHelper.Entities.HoldableBarrierJumpThru") is { } viv)
        {
            jumpThruTypeDirections[viv] = Direction.Zero;
        }
        if (ReflectionUtils.GetType("MaxHelpingHand", "Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru") is { } sideways)
        {
            jumpThruTypeDirections[sideways] = Direction.LeftRight;
            jumpThruTypes.Add(sideways);
        }

        if (ReflectionUtils.GetType("SpringCollab2020", "Celeste.Mod.SpringCollab2020.Entities.SidewaysJumpThru") is { } sc2020sideways)
        {
            jumpThruTypeDirections[sc2020sideways] = Direction.LeftRight;
            jumpThruTypes.Add(sc2020sideways);
        }

        if (ReflectionUtils.GetType("SpringCollab2020", "Celeste.Mod.SpringCollab2020.Entities.UpsideDownJumpThru") is { } sc2020upsidedown)
        {
            jumpThruTypeDirections[sc2020upsidedown] = Direction.Up;
            jumpThruTypes.Add(sc2020upsidedown);
        }

        if (ReflectionUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SampleJumpThroughDown") is { } chroniadown)
        {
            jumpThruTypeDirections[chroniadown] = Direction.Up;
        }

        if (ReflectionUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SampleJumpThroughSideways") is { } chroniasideways)
        {
            jumpThruTypeDirections[chroniasideways] = Direction.LeftRight;
            jumpThruTypes.Add(chroniasideways);
        }

        if (ReflectionUtils.GetType("ChroniaHelper", "ChroniaHelper.Entities.SampleJumpThroughUp") is { } chroniaup)
        {
            jumpThruTypeDirections[chroniaup] = Direction.Down;
        }
    }

    private static Direction GetDir(Vector2 vector)
    {
        Direction result = Direction.Zero;

        if (vector.X > 0f)
        {
            result |= Direction.Right;
        }
        else if (vector.X < 0f)
        {
            result |= Direction.Left;
        }

        if (vector.Y > 0f)
        {
            result |= Direction.Down;
        }
        else if (vector.Y < 0f)
        {
            result |= Direction.Up;
        }

        return result;
    }

    private static Direction GetDirection(Entity jumpThru)
    {
        Type type = jumpThru.GetType();
        foreach (Type key in jumpThruTypeDirections.Keys)
        {
            if (type == key || type.IsSubclassOf(key))
            {
                Direction dir = jumpThruTypeDirections[key];
                if (dir == Direction.LeftRight)
                {
                    return jumpThru.GetFieldValue<bool>("AllowLeftToRight") ? Direction.Left : Direction.Right;
                }
                return dir;
            }
        }
        return Direction.Down;
    }

    private static Dictionary<Type, Direction> jumpThruTypeDirections = new();

    private static List<Type> jumpThruTypes = new();

    [Flags]
    private enum Direction
    {
        Zero = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        LeftRight = Left | Right
    }
}

public static class ReflectionUtils
{
    public static Type GetType(string modName, string name)
    {
        return Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == modName)?.GetType().Assembly?.GetType(name);
    }

    public static T GetFieldValue<T>(this object obj, string name)
    {
        object result = obj.GetType().GetField(name)?.GetValue(obj);
        if (result == null)
        {
            return default;
        }
        return (T)result;
    }
}