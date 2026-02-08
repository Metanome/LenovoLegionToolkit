using System;
using System.Collections.Generic;
using Windows.Devices.Lights;
using Windows.UI;

namespace LenovoLegionToolkit.Lib.Utils.LampEffects;

public class MeteorEffect : BaseLampEffect
{
    public override string Name => "Meteor";
    private readonly Random _random = new();

    public MeteorEffect(Color color, int meteorCount = 3, double speed = 1.0)
    {
        Parameters["Color"] = color;
        Parameters["MeteorCount"] = meteorCount;
        Parameters["Speed"] = speed;
        Parameters["Meteors"] = new List<Meteor>();
    }

    public override void Reset()
    {
        Parameters["Meteors"] = new List<Meteor>();
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var color = (Color)Parameters["Color"];
        var meteorCount = (int)Parameters["MeteorCount"];
        var speed = (double)Parameters["Speed"];
        var meteors = (List<Meteor>)Parameters["Meteors"];

        while (meteors.Count < meteorCount)
        {
            meteors.Add(new Meteor
            {
                StartTime = time + _random.NextDouble() * 3,
                StartY = _random.NextDouble() * 800,
                Speed = speed * (0.7 + _random.NextDouble() * 0.6)
            });
        }

        double maxIntensity = 0;
        foreach (var meteor in meteors)
        {
            if (time < meteor.StartTime) continue;

            var elapsed = time - meteor.StartTime;
            var meteorX = elapsed * meteor.Speed * 300;

            if (meteorX > 2500)
            {
                meteor.StartTime = time + _random.NextDouble() * 0.5;
                meteor.StartY = _random.NextDouble() * 800;
                continue;
            }

            var dx = lampInfo.Position.X - meteorX;
            var dy = lampInfo.Position.Y - meteor.StartY;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            if (dx > -200 && dx < 150 && Math.Abs(dy) < 80)
            {
                var intensity = 0.0;
                
                if (dx < 0)
                {
                    intensity = 1.0 - Math.Abs(dx) / 150.0;
                    intensity *= 1.0 - Math.Abs(dy) / 80.0;
                }
                else
                {
                    intensity = 1.0 - dx / 200.0;
                    intensity *= 1.0 - Math.Abs(dy) / 80.0;
                    intensity *= 0.7;
                }
                
                intensity = Math.Pow(intensity, 0.8);
                maxIntensity = Math.Max(maxIntensity, intensity);
            }
        }

        return Color.FromArgb(255,
            (byte)(color.R * maxIntensity),
            (byte)(color.G * maxIntensity),
            (byte)(color.B * maxIntensity));
    }

    private class Meteor
    {
        public double StartTime { get; set; }
        public double StartY { get; set; }
        public double Speed { get; set; }
    }
}

public class RippleEffect : BaseLampEffect
{
    public override string Name => "Ripple";
    private readonly Random _random = new();

    public RippleEffect(Color color, double period = 2.0, int rippleCount = 2)
    {
        Parameters["Color"] = color;
        Parameters["Period"] = period;
        Parameters["RippleCount"] = rippleCount;
        Parameters["Ripples"] = new List<Ripple>();
    }

    public override void Reset()
    {
        Parameters["Ripples"] = new List<Ripple>();
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var color = (Color)Parameters["Color"];
        var period = (double)Parameters["Period"];
        var rippleCount = (int)Parameters["RippleCount"];
        var ripples = (List<Ripple>)Parameters["Ripples"];

        if (ripples.Count == 0 || time - ripples[^1].StartTime > period)
        {
            if (ripples.Count >= rippleCount)
                ripples.RemoveAt(0);

            ripples.Add(new Ripple
            {
                StartTime = time,
                CenterX = _random.NextDouble() * 2000,
                CenterY = _random.NextDouble() * 800
            });
        }

        double maxIntensity = 0;
        foreach (var ripple in ripples)
        {
            var elapsed = time - ripple.StartTime;
            var radius = elapsed * 300;

            var dx = lampInfo.Position.X - ripple.CenterX;
            var dy = lampInfo.Position.Y - ripple.CenterY;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            var diff = Math.Abs(distance - radius);
            if (diff < 50)
            {
                var intensity = 1.0 - diff / 50.0;
                intensity *= 1.0 - Math.Min(elapsed / 3.0, 1.0);
                maxIntensity = Math.Max(maxIntensity, intensity);
            }
        }

        return Color.FromArgb(255,
            (byte)(color.R * maxIntensity),
            (byte)(color.G * maxIntensity),
            (byte)(color.B * maxIntensity));
    }

    private class Ripple
    {
        public double StartTime { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
    }
}

public class SparkleEffect : BaseLampEffect
{
    public override string Name => "Sparkle";
    private readonly Random _random = new();

    public SparkleEffect(Color color, double density = 0.1)
    {
        Parameters["Color"] = color;
        Parameters["Density"] = density;
        Parameters["Sparkles"] = new Dictionary<int, double>();
    }

    public override void Reset()
    {
        Parameters["Sparkles"] = new Dictionary<int, double>();
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var color = (Color)Parameters["Color"];
        var density = (double)Parameters["Density"];
        var sparkles = (Dictionary<int, double>)Parameters["Sparkles"];

        if (!sparkles.ContainsKey(lampIndex) && _random.NextDouble() < density * 0.01)
        {
            sparkles[lampIndex] = time;
        }

        if (sparkles.TryGetValue(lampIndex, out var startTime))
        {
            var elapsed = time - startTime;
            if (elapsed > 0.5)
            {
                sparkles.Remove(lampIndex);
                return Color.FromArgb(255, 0, 0, 0);
            }

            var intensity = 1.0 - elapsed / 0.5;
            intensity = EaseOut(intensity);

            return Color.FromArgb(255,
                (byte)(color.R * intensity),
                (byte)(color.G * intensity),
                (byte)(color.B * intensity));
        }

        return Color.FromArgb(255, 0, 0, 0);
    }
}

public class GradientEffect : BaseLampEffect
{
    public override string Name => "Gradient";

    public GradientEffect(Color[] colors, GradientDirection direction = GradientDirection.LeftToRight, bool animated = false, double period = 5.0)
    {
        Parameters["Colors"] = colors;
        Parameters["Direction"] = direction;
        Parameters["Animated"] = animated;
        Parameters["Period"] = period;
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var colors = (Color[])Parameters["Colors"];
        var direction = (GradientDirection)Parameters["Direction"];
        var animated = (bool)Parameters["Animated"];
        var period = (double)Parameters["Period"];

        if (colors.Length == 0) return Color.FromArgb(255, 0, 0, 0);
        if (colors.Length == 1) return colors[0];

        double position = direction switch
        {
            GradientDirection.LeftToRight => lampInfo.Position.X / 2200.0,
            GradientDirection.RightToLeft => 1.0 - lampInfo.Position.X / 2200.0,
            GradientDirection.TopToBottom => lampInfo.Position.Y / 800.0,
            GradientDirection.BottomToTop => 1.0 - lampInfo.Position.Y / 800.0,
            _ => lampInfo.Position.X / 2200.0
        };

        if (animated)
        {
            position = (position + time / period) % 1.0;
        }

        var segmentSize = 1.0 / (colors.Length - 1);
        var segmentIndex = (int)(position / segmentSize);
        segmentIndex = Math.Clamp(segmentIndex, 0, colors.Length - 2);

        var segmentPosition = (position - segmentIndex * segmentSize) / segmentSize;
        return LerpColor(colors[segmentIndex], colors[segmentIndex + 1], segmentPosition);
    }
}

public enum GradientDirection
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop
}
