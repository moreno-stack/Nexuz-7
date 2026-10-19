namespace Nexus7.Characters
{
    /// <summary>Nombres estándar de las articulaciones de los modelos (los usa ProceduralAnimator).</summary>
    public static class RigJoints
    {
        public const string Hips = "Hips", Spine = "Spine", Head = "Head";
        public const string LUpperArm = "L_UpperArm", LForearm = "L_Forearm", LHand = "L_Hand";
        public const string RUpperArm = "R_UpperArm", RForearm = "R_Forearm", RHand = "R_Hand";
        public const string LThigh = "L_Thigh", LShin = "L_Shin", RThigh = "R_Thigh", RShin = "R_Shin";
        /// <summary>La mano derecha sostiene el objeto o el arma.</summary>
        public const string HandSocket = RHand;

        public static readonly string[] All =
        {
            Hips, Spine, Head, LUpperArm, LForearm, LHand, RUpperArm, RForearm, RHand, LThigh, LShin, RThigh, RShin
        };
    }

    public enum RobotStyle { Security, Guard, Research, Elite }
    public enum WeaponShape { Bat, Wrench, Baton, Axe }
    public enum RigShape { Joint, Capsule, Sphere, Cylinder, Box }
    public enum RigMaterial { Body, Accent, Suit, Visor, Eye, Skin, Metal, Glow }
}
