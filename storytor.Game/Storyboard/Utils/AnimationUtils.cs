using System;
using osuTK;

namespace storytor.Game.Storyboard.Utils
{
    /// <summary>
    /// Utility class for animation interpolation and easing functions
    /// </summary>
    public static class AnimationUtils
    {
        /// <summary>
        /// Interpolates between two double values over time with easing
        /// </summary>
        public static double InterpolateDouble(double timeMs, int startTime, int endTime, double startValue, double endValue, int easing)
        {
            if (timeMs < startTime) return startValue;
            if (timeMs >= endTime) return endValue;

            var progress = (timeMs - startTime) / (endTime - startTime);
            var easedProgress = ApplyEasing(progress, easing);
            return startValue + (endValue - startValue) * easedProgress;
        }

        /// <summary>
        /// Interpolates between two Vector2 values over time with easing
        /// </summary>
        public static Vector2 InterpolateVector2(double timeMs, int startTime, int endTime, Vector2 startValue, Vector2 endValue, int easing)
        {
            if (timeMs < startTime) return startValue;
            if (timeMs >= endTime) return endValue;

            var progress = (timeMs - startTime) / (endTime - startTime);
            var easedProgress = ApplyEasing(progress, easing);
            return startValue + (endValue - startValue) * (float)easedProgress;
        }

        /// <summary>
        /// Applies easing function to progress value
        /// </summary>
        public static double ApplyEasing(double progress, int easingType)
        {
            return easingType switch
            {
                0 => progress, // Linear
                1 => easeOut(progress), // Ease Out
                2 => easeIn(progress), // Ease In
                3 => easeInOut(progress), // Ease In-Out
                _ => progress // Default to linear
            };
        }

        private static double easeOut(double t) => 1 - Math.Pow(1 - t, 2);
        private static double easeIn(double t) => t * t;
        private static double easeInOut(double t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }
    }
}