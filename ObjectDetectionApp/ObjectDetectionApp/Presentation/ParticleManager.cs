using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
    public class ParticleManager
    {
        private class Particle : Image
        {
            public Particle(int imageNumber, double scale)
            {
                Source = "partikel" + imageNumber.ToString() + ".png";
                Scale = scale;
            }
        }

        readonly Random random = new Random();
        AbsoluteLayout absoluteLayout;
        readonly uint maxParticles = 40;
        uint numberOfParticles = 0;
        readonly object lockObj = new object();
        ConcurrentQueue<Particle> removeQueue;
        Timer timer;

        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    if (value)
                        timer.Start();
                    else
                        timer.Stop();
                }
            }
        }

        public ParticleManager(AbsoluteLayout absLayout)
        {
            removeQueue = new ConcurrentQueue<Particle>();
            timer = new Timer { Interval = 50 };
            absoluteLayout = absLayout;
            timer.Elapsed += OnTimerTick;
            timer.Start();

        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            timer.Interval = random.Next(25, 80);
            Device.BeginInvokeOnMainThread(() => CreateParticle());
        }

        private void CreateParticle()
        {
            lock (lockObj)
            {
                if (numberOfParticles < maxParticles)
                {
                    var part = new Particle(random.Next(1, 5), RandomDouble(0.2, 0.5));
                    AbsoluteLayout.SetLayoutBounds(part, new Rectangle(random.NextDouble(), 1.2, 40, 40));
                    AbsoluteLayout.SetLayoutFlags(part, AbsoluteLayoutFlags.PositionProportional);

                    absoluteLayout.Children.Add(part);
                    numberOfParticles++;

                    removeQueue.Enqueue(part);
                    part.FadeTo(0.5, 2000);
                    part.TranslateTo(0, -2000, (uint)random.Next(2000, 8000)).ContinueWith((val) => Device.BeginInvokeOnMainThread(() =>
                    {
                        lock (lockObj)
                        {
                            Particle particle;
                            while (!removeQueue.TryDequeue(out particle)) ;
                            absoluteLayout.Children.Remove(particle);
                            numberOfParticles--;
                        }
                    }));
                }
            }
        }

        private double RandomDouble(double min, double max)
        {
            return random.NextDouble() * (max - min) + min;
        }
    }
}
