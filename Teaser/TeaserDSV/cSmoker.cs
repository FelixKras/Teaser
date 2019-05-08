using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TeaserDSV
{

    public class ParticlePool
    {
        private readonly ConcurrentBag<Particle> _objects;
        private readonly Func<Particle> _objectGenerator;

        public ParticlePool(Func<Particle> objectGenerator)
        {
            if (objectGenerator == null)
            {
                throw new ArgumentNullException("Object Generator");
            }
            _objects = new ConcurrentBag<Particle>();
            _objectGenerator = objectGenerator;
        }

        public Particle GetObject()
        {
            Particle item;
            if (!_objects.TryTake(out item))
            {
                item = _objectGenerator();
            }
            return item;
        }

        public void PutObject(Particle item)
        {
            _objects.Add(item);
        }
    }

    public class Particle
    {
        private static TimeSpan LiveTime = TimeSpan.FromSeconds(SettingsHolder.Instance.ParticleLifeTime);
        private static Stopwatch swGlobal;

        private long BirthTime = -1;

        private PointF _location;

        public PointF Location
        {
            get
            {
                return _location;
            }
        }

        private PointF _velocity;
        private readonly PointF _location1;


        static long GetTimeFromStart()
        {
            long result = -1;
            if (swGlobal == null)
            {
                swGlobal = Stopwatch.StartNew();
            }

            result = (long)(1000D * swGlobal.ElapsedTicks / Stopwatch.Frequency);
            return result;
        }

        public Particle()
        {
            if (BirthTime < 0) BirthTime = GetTimeFromStart();

            //imgSmokePSF=Image.FromFile(@"c:\psf.png");
        }

        public void Rebirth(PointF loc, PointF vel)
        {
            _location = loc;
            _velocity = vel;

            BirthTime = GetTimeFromStart();
        }

        public bool PerformFrame()
        {
            _location.X = _location.X + _velocity.X;
            _location.Y = _location.Y + _velocity.Y;

            _velocity.X = _velocity.X * SettingsHolder.Instance.ParticleDecceleration;
            _velocity.Y = _velocity.Y * SettingsHolder.Instance.ParticleDecceleration;

            //_size.Width = _size.Width * 0.99F;
            //_size.Height = _size.Height * 0.99F;

            return GetTimeFromStart() - BirthTime > LiveTime.TotalMilliseconds;
        }


        public void GetColorToLife(ref int alpha, ref int color, int direction)
        {
            double mslivetime = (GetTimeFromStart() - BirthTime);
            double lifetime = LiveTime.TotalMilliseconds;

            double percentlife = mslivetime / lifetime;

            //int Alphause = 255 - (int)(percentlife * 200);

            alpha = (int)(250 * Math.Pow(0.95, 100 * percentlife));
            //color = (int)(250-(50 * (100 * percentlife / (100 * percentlife + 10))));
            if (direction == 1)
                color = 0 + (int)(percentlife * 255);
            if (direction == -1)
                color = 255 - (int)(percentlife * 255);
            color = Math.Min(255, color);
            color = Math.Max(0, color);
        }
    }

    public class cSmoker
    {
        public List<Particle> Particles;
        public ParticlePool particlePool = new ParticlePool(() => new Particle());
        private Random rg;
        public cSmoker()
        {
            Particles = new List<Particle>();
            rg = new Random();
        }

        public Particle GetFromPool(PointF emitPoint, float Speed)
        {
            Particle particle = particlePool.GetObject();

            particle.Rebirth(emitPoint, RandomVector(Speed));
            return particle;
        }

        public void ReturnToPool(Particle p)
        {
            particlePool.PutObject(p);
            Particles.Remove(p);
        }
        private PointF RandomVector(float speed)
        {
            float rangle = (float)((rg.NextDouble() * (Math.PI * 2)));

            speed *= (float)rg.NextDouble();

            return new PointF((float)Math.Cos(rangle) * speed, (float)Math.Sin(rangle) * speed);
        }
    }
}
