namespace InvisibleManXRay.Core
{
    public class InvisibleManXRayCore
    {
        public static InvisibleManXRayCore Instance;

        public void Initialize()
        {
            if (Instance != null)
                return;
            
            Instance = this;
        }
    }
}