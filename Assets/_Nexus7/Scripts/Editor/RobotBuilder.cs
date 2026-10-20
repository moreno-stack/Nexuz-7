using UnityEditor;
using UnityEngine;
using Nexus7.Characters;
using Nexus7.Enemies;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;
using B = Nexus7.EditorTools.SalaInicialBuilder;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Robots armados redondeados (sustituyen a los drones «caja»). Lo usan los constructores de las Fases 2–6:
    /// mismo colisionador, NavMeshAgent, visión, patrulla y sonido que A.Drone, más modelo, arma en la mano,
    /// animación procedural y ataque cuerpo a cuerpo (EnemyMeleeAttack).
    /// </summary>
    public static class RobotBuilder
    {
        private const string WeaponsFolder = "Assets/_Nexus7/Data/Weapons";

        /// <summary>Crea o actualiza el asset del arma con los valores de WeaponCatalog.</summary>
        internal static WeaponData Weapon(WeaponShape shape)
        {
            B.EnsureFolder("Assets/_Nexus7", "Data");
            B.EnsureFolder("Assets/_Nexus7/Data", "Weapons");
            string path = $"{WeaponsFolder}/Arma_{shape}.asset";
            var w = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (w == null)
            {
                w = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(w, path);
            }
            var s = WeaponCatalog.Get(shape);
            w.Configure(s.name, s.shape, s.damage, s.windup, s.range, s.recovery, s.stun);
            EditorUtility.SetDirty(w);
            return w;
        }

        /// <summary>Robot de seguridad: gris, detalles oscuros, ojo cian.</summary>
        internal static RigMaterials SecurityMaterials()
        {
            var metal = B.CreateMat("MR_Metal", new Color(0.45f, 0.48f, 0.52f), null, 0.6f, 0.9f);
            var eye = B.CreateMat("MR_OjoSeguridad", new Color(0.4f, 0.9f, 1f), new Color(0.4f, 0.9f, 1f) * 3f);
            return new RigMaterials
            {
                body   = B.CreateMat("MR_SeguridadCuerpo", new Color(0.55f, 0.57f, 0.6f), null, 0.55f, 0.6f),
                accent = B.CreateMat("MR_SeguridadDetalle", new Color(0.2f, 0.21f, 0.23f), null, 0.4f, 0.7f),
                metal  = metal,
                eye    = eye,
                glow   = eye,
                visor  = eye,
                suit   = metal,
                skin   = metal,
            };
        }

        /// <summary>Robot guardia: amarillo industrial, detalles oscuros, ojo ámbar.</summary>
        internal static RigMaterials GuardMaterials()
        {
            var metal = B.CreateMat("MR_Metal", new Color(0.45f, 0.48f, 0.52f), null, 0.6f, 0.9f);
            var eye = B.CreateMat("MR_OjoGuardia", new Color(1f, 0.7f, 0.2f), new Color(1f, 0.7f, 0.2f) * 3f);
            return new RigMaterials
            {
                body   = B.CreateMat("MR_GuardiaCuerpo", new Color(0.85f, 0.68f, 0.12f), null, 0.45f, 0.5f),
                accent = B.CreateMat("MR_GuardiaDetalle", new Color(0.18f, 0.18f, 0.2f), null, 0.4f, 0.7f),
                metal  = metal, eye = eye, glow = eye, visor = eye, suit = metal, skin = metal,
            };
        }

        /// <summary>Robot de investigación: blanco, visor morado emisivo, ojo morado.</summary>
        internal static RigMaterials ResearchMaterials()
        {
            var metal = B.CreateMat("MR_Metal", new Color(0.45f, 0.48f, 0.52f), null, 0.6f, 0.9f);
            var purple = B.CreateMat("MR_VisorInvestigacion", new Color(0.7f, 0.3f, 1f), new Color(0.7f, 0.3f, 1f) * 3f);
            return new RigMaterials
            {
                body   = B.CreateMat("MR_InvestigacionCuerpo", new Color(0.88f, 0.9f, 0.92f), null, 0.8f, 0.3f),
                accent = purple, eye = purple, glow = purple, visor = purple,
                metal  = metal, suit = metal, skin = metal,
            };
        }

        /// <summary>
        /// Extras del robot de investigación (como el antiguo ResearchDrone): foco de escaneo morado que se enciende al
        /// inspeccionar, efecto de aturdimiento (PEM) sobre la cabeza y oído más fino (×1,2).
        /// </summary>
        internal static void ResearchExtras(EnemyPatrol robot)
        {
            var scanGo = new GameObject("FocoEscaneo");
            scanGo.transform.SetParent(robot.transform, false);
            scanGo.transform.localPosition = new Vector3(0f, 1.3f, 0.45f);
            scanGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            var scan = scanGo.AddComponent<Light>();
            scan.type = LightType.Spot;
            scan.spotAngle = 55f;
            scan.range = 4f;
            scan.intensity = 4f;
            scan.color = new Color(0.75f, 0.35f, 1f);

            var stun = new GameObject("EfectoAturdido");
            stun.transform.SetParent(robot.transform, false);
            stun.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            A.PointLight("LuzAturdido", stun.transform, Vector3.zero, new Color(0.3f, 0.8f, 1f), 3f, 2f, false);
            SalaEnergiaBuilder.Arc("ArcoAturdido", stun.transform, new Vector3(-0.35f, 0f, 0f), new Vector3(0.7f, 0f, 0f), 0.2f, 0.03f);

            B.SetRef(robot, "scanLight", scan);
            B.SetRef(robot, "stunEffect", stun);
            A.SetFloat(robot, "hearingMultiplier", 1.2f);
        }

        /// <summary>Robot armado con el arma de su estilo (WeaponCatalog.ForRobot) en la mano derecha.</summary>
        internal static EnemyPatrol ArmedRobot(string name, Transform parent, Vector3 pos, float rotY, PatrolRoute route,
                                               RobotStyle style, RigMaterials mats)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.4f;

            var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.9f;
            agent.speed = 1.8f;
            agent.angularSpeed = 300f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.2f;
            agent.enabled = false; // se activa en Start, cuando el NavMesh ya existe

            // ---------- Modelo + arma ----------
            var model = CharacterRigFactory.BuildRobot(root.transform, style, mats);
            var hand = CharacterRigFactory.FindDeep(model, RigJoints.HandSocket);
            var weaponShape = WeaponCatalog.ForRobot(style);
            if (hand != null) CharacterRigFactory.BuildWeapon(hand, weaponShape, mats);
            var anim = root.AddComponent<ProceduralAnimator>();
            B.SetRef(anim, "modelRoot", model);

            // ---------- Ojo / foco de visión a la altura real de la cabeza ----------
            var head = CharacterRigFactory.FindDeep(model, RigJoints.Head);
            Vector3 eyeLocal = head != null ? root.transform.InverseTransformPoint(head.position) : new Vector3(0f, 1.6f, 0f);
            var eye = new GameObject("Ojo").transform;
            eye.SetParent(root.transform, false);
            eye.localPosition = eyeLocal + new Vector3(0f, 0f, 0.15f);
            eye.localRotation = Quaternion.Euler(8f, 0f, 0f); // ligeramente hacia abajo, como un foco

            var lightGo = new GameObject("FocoVision");
            lightGo.transform.SetParent(eye, false);
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 12f;
            spot.spotAngle = 80f;
            spot.intensity = 4f;
            spot.shadows = LightShadows.Soft;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var vision = root.AddComponent<EnemyVision>();
            B.SetRef(vision, "eye", eye);
            B.SetRef(vision, "visionLight", spot);

            // ---------- Ataque cuerpo a cuerpo ----------
            var melee = root.AddComponent<EnemyMeleeAttack>();
            B.SetRef(melee, "weapon", Weapon(weaponShape));
            B.SetRef(melee, "animator", anim);
            var eyePart = CharacterRigFactory.FindDeep(model, "Eye");
            if (eyePart != null) B.SetRef(melee, "eyeRenderer", eyePart.GetComponent<Renderer>());

            var patrol = root.AddComponent<EnemyPatrol>();
            B.SetRef(patrol, "route", route);
            B.SetRef(patrol, "audioSource", audio);
            B.SetString(patrol, "displayName", DisplayName(style));
            return patrol;
        }

        /// <summary>Nombre del robot en los mensajes («El robot de seguridad ha deducido el engaño»...).</summary>
        internal static string DisplayName(RobotStyle style)
        {
            switch (style)
            {
                case RobotStyle.Guard:    return "Robot guardia";
                case RobotStyle.Research: return "Robot de investigación";
                case RobotStyle.Elite:    return "Robot de élite";
                default:                  return "Robot de seguridad";
            }
        }
    }
}
