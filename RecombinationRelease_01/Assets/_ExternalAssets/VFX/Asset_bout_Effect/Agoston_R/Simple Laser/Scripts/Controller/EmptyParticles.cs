namespace Controller
{
    /// <summary>
    /// Does not do anything in case the given laser has no particles set up to play on hit.
    /// Serves as a safe default value to prevent exceptions and unnecessary nullchecks.
    /// </summary>
    public class EmptyParticles : IHitParticlesController
    {
        private static EmptyParticles _instance;
        public static EmptyParticles Instance => _instance ??= new EmptyParticles();

        private EmptyParticles() 
        {
        }

        public void Play(LaserHit hit)
        {
            // no op
        }

        public void Stop()
        {
            // no op
        }
    }
}
