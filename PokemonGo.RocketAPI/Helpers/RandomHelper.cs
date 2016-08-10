using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PokemonGo.RocketAPI.Helpers
{
    public class RandomHelper
    {
        private static readonly Random Random = new Random();

        public static long GetLongRandom(long min, long max)
        {
            var buf = new byte[8];
            Random.NextBytes(buf);
            var longRand = BitConverter.ToInt64(buf, 0);

            return Math.Abs(longRand % (max - min)) + min;
        }

        public static async Task RandomDelay(int delay)
        {
            var randomFactor = 0.3f;
            var randomMin = (int)(delay * (1 - randomFactor));
            var randomMax = (int)(delay * (1 + randomFactor));
            var randomizedDelay = Random.Next(randomMin, randomMax);

            await Task.Delay(randomizedDelay);
        }

        public static async Task RandomDelay(int min, int max)
        {
            await Task.Delay(Random.Next(min, max));
        }

        public static void RandomSleep(int min, int max)
        {
            Thread.Sleep(Random.Next(min, max));
        }

        public static int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}