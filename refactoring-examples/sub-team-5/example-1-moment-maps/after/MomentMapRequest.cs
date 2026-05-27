namespace DataFeatures
{
    public sealed class MomentMapRequest
    {
        public float[] Moment0Pixels { get; }
        public float[] Moment1Pixels { get; }
        
        public float Moment0Min { get; }
        public float Moment0Max { get; }
        
        public float Moment1Min { get; }
        public float Moment1Max { get; }
        
        public MomentMapResults(
            float[] moment0Pixels,
            float[] moment1Pixels,
            float moment0Min,
            float moment0Max,
            float moment1Min,
            float moment1Max,)
            {
            Moment0Pixels = moment0Pixels;
            Moment1Pixels = moment1Pixels;
            Moment0Min = moment0Min;
            Moment0Max = moment0Max;
            Moment1Min = moment1Min;
            Moment1Max = moment1Max;
            }
    }
}