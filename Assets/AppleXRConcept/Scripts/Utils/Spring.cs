using UnityEngine;

/// <summary>
/// A simplified spring simulation used for evaluating the modeled position of a partical 
/// attached to a spring that follows Hooke's law <see cref="global::Spring"/> given a  
/// lerp position between [0, 1].
/// </summary>
[System.Serializable]
public struct SpringCurve
{
    /// <summary>
    /// The <see cref="Spring.Period"/> is defined as 2 * PI * sqrt(Mass / Stiffness), but the
    /// <see cref="SpringCurve"/> simplifies the spring parameters and just uses
    /// a default <see cref="Spring.Mass"/> and <see cref="Spring.Stiffness"/> of 1.
    /// </summary>
    private const float Period = Mathf.PI * 2;

    public static readonly SpringCurve Overshoot = new SpringCurve() { OvershootPercent = 0.025f, Oscillations = 2f };
    public static readonly SpringCurve Ease = new SpringCurve() { OvershootPercent = 0.001f, Oscillations = 2f };

    [Min(0)]
    public float Oscillations;
    [Range(0, 1)]
    public float OvershootPercent;

    private float UnscaledDuration => Period * Oscillations;

    public Spring Spring => Spring.WithOvershootPercent(OvershootPercent);

    public float GetPosition(float percentDone) => SpringUtil.GetPosition(percentDone * UnscaledDuration, Spring);

    public float GetVelocity(float percentDone) => SpringUtil.GetVelocity(percentDone * UnscaledDuration, Spring);

    public bool IsDone(float percentDone, float epsilon = 0.01f)
    {
        float px = Mathf.Abs(1 - Mathf.Abs(GetPosition(percentDone)));
        float vx = Mathf.Abs(GetVelocity(percentDone));

        return px <= epsilon && vx <= epsilon;
    }

    public bool IsSettling(float percentDone) => percentDone > 1 / (Oscillations * 2);

    /// <summary>
    /// Convert the spring to an animation curve.
    /// </summary>
    public static explicit operator AnimationCurve(SpringCurve spring)
    {
        AnimationCurve curve = new AnimationCurve();

        float duration = spring.UnscaledDuration;
        int numKeys =  Mathf.FloorToInt(2 * spring.Oscillations) + 2;

        for (int i = 0; i < numKeys; ++i)
        {
            float time = Mathf.Clamp01(i * Mathf.PI / duration);
            float position = spring.GetPosition(time);
            float velocity = spring.GetVelocity(time) * 10;
            velocity = Mathf.Approximately(velocity, 0) ? 0 : velocity;

            Keyframe k = new Keyframe(time, position, velocity, velocity);

            curve.AddKey(k);
        }

        return curve;
    }
}

/// <summary>
/// Structure that describes a spring's constants.
/// Used to configure a <see cref="SpringCurve"/>.
/// </summary>
[System.Serializable]
public struct Spring
{
    private const float PI_SQ = Mathf.PI * Mathf.PI;
    private const float TWO_PI = 2 * Mathf.PI;

    /// The mass of the spring (m). The units are arbitrary, but all springs
    /// within a system should use the same mass units.
    [Min(0)]
    public float Mass;

    /// The spring constant (k). The units of stiffness are M/T², where M is the
    /// mass unit used for the value of the [mass] property, and T is the time
    /// unit used for driving the [SpringSimulation].
    [Min(0)]
    public float Stiffness;

    /// The damping coefficient (c).
    /// The units of the damping coefficient are M/T, where M is the mass unit
    /// used for the value of the [mass] property, and T is the time unit used for
    /// driving the [SpringSimulation].
    public float Damping;

    /// Creates a spring given the mass, stiffness, and the damping coefficient.
    ///
    /// See [mass], [stiffness], and [damping] for the units of the arguments.
    public Spring(float mass, float stiffness, float damping)
    {
        Damping = damping;
        Mass = mass;
        Stiffness = stiffness;
    }

    public float Period => TWO_PI * Mathf.Sqrt(Mass / Stiffness);

    public float DampingRatioSq => Damping * Damping / (4 * Mass * Stiffness);

    /// <summary>
    /// https://en.wikipedia.org/wiki/Damping
    /// </summary>
    /// <remarks>
    /// Sometimes referred to as the "natural angular frequency" 
    /// or the "undamped angular frequency"
    /// </remarks>
    public float NaturalFrequency => Mathf.Sqrt(Stiffness / Mass);

    /// <summary>
    /// https://en.wikipedia.org/wiki/Damping
    /// </summary>
    public static Spring WithOvershootPercent(float overshoot, float mass = 1, float stiffness = 1)
    {
        float dampingRatio = 1;

        if (overshoot > 0)
        {
            float lnOvershoot = Mathf.Log(overshoot);
            dampingRatio = -lnOvershoot / Mathf.Sqrt(PI_SQ + (lnOvershoot * lnOvershoot));
        }

        float damping = dampingRatio * 2 * Mathf.Sqrt(mass * stiffness);

        return new Spring(mass, stiffness, damping);
    }
}

public static class SpringUtil
{
    public static float GetPosition(float time, Spring spring) => GetPosition(time, spring, 0, 1, 0);
    public static float GetPosition(float time, Spring spring, float startPosition, float endPosition, float startingVelocity)
    {
        float ratio = spring.DampingRatioSq;

        float distance = startPosition - endPosition;

        if (ratio == 1.0f)
        {
            return endPosition + new CriticallyDampedPhysics(spring, distance, startingVelocity).GetPosition(time);
        }

        if (ratio > 1.0f)
        {
            return endPosition + new OverDampedPhysics(spring, distance, startingVelocity).GetPosition(time);
        }

        return endPosition + new UnderDampedPhysics(spring, distance, startingVelocity).GetPosition(time);
    }

    public static float GetVelocity(float time, Spring spring) => GetVelocty(time, spring, -1, 0);
    public static float GetVelocty(float time, Spring spring, float startingDistance, float startingVelocity)
    {
        float ratio = spring.DampingRatioSq;

        if (ratio == 1.0f)
        {
            return new CriticallyDampedPhysics(spring, startingDistance, startingVelocity).GetVelocity(time);
        }

        if (ratio > 1.0f)
        {
            return new OverDampedPhysics(spring, startingDistance, startingVelocity).GetVelocity(time);
        }

        return new UnderDampedPhysics(spring, startingDistance, startingVelocity).GetVelocity(time);
    }
}

// SPRING IMPLEMENTATIONS

public interface ISpringPhysics
{
    float GetPosition(float time);
    float GetVelocity(float time);
}

public struct OverDampedPhysics : ISpringPhysics
{
    float r1, r2, c1, c2;

    public OverDampedPhysics(Spring spring) : this(spring, -1, 0) { }

    public OverDampedPhysics(Spring spring, float distance, float velocity)
    {
        float cmk = spring.Damping * spring.Damping - 4 * spring.Mass * spring.Stiffness;
        r1 = (-spring.Damping - Mathf.Sqrt(cmk)) / (2.0f * spring.Mass);
        r2 = (-spring.Damping + Mathf.Sqrt(cmk)) / (2.0f * spring.Mass);
        c2 = (velocity - r1 * distance) / (r2 - r1);
        c1 = distance - c2;
    }

    public float GetPosition(float time)
    {
        return c1 * Mathf.Exp(r1 * time) +
               c2 * Mathf.Exp(r2 * time);
    }


    public float GetVelocity(float time)
    {
        return c1 * r1 * Mathf.Exp(r1 * time) +
               c2 * r2 * Mathf.Exp(r2 * time);
    }
}

public struct CriticallyDampedPhysics : ISpringPhysics
{
    float r, c1, c2;

    public CriticallyDampedPhysics(Spring spring) : this(spring, -1, 0) { }

    public CriticallyDampedPhysics(Spring spring, float distance, float velocity)
    {
        r = -spring.Damping / (2.0f * spring.Mass);
        c1 = distance;
        c2 = velocity - (r * distance);
    }

    public float GetPosition(float time)
    {
        return (c1 + c2 * time) * Mathf.Exp(r * time);
    }

    public float GetVelocity(float time)
    {
        float power = Mathf.Exp(r * time);
        return r * (c1 + c2 * time) * power + c2 * power;
    }
}

public struct UnderDampedPhysics : ISpringPhysics
{
    float angularFrequency, r, c1, c2;

    public UnderDampedPhysics(Spring spring) : this(spring, -1, 0) { }

    public UnderDampedPhysics(Spring spring, float distance, float velocity)
    {
        angularFrequency = Mathf.Sqrt(4.0f * spring.Mass * spring.Stiffness - spring.Damping * spring.Damping) / (2.0f * spring.Mass);

        r = -spring.Damping / (2.0f * spring.Mass);
        c1 = distance;
        c2 = (velocity - r * distance) / angularFrequency;
    }

    public float GetPosition(float time)
    {
        return Mathf.Exp(r * time) * (c1 * Mathf.Cos(angularFrequency * time) + c2 * Mathf.Sin(angularFrequency * time));
    }

    public float GetVelocity(float time)
    {
        float power = Mathf.Exp(r * time);
        float cosine = Mathf.Cos(angularFrequency * time);
        float sine = Mathf.Sin(angularFrequency * time);

        return power * (c2 * angularFrequency * cosine - c1 * angularFrequency * sine) + r * power * (c2 * sine + c1 * cosine);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(SpringCurve))]
public class SpringAnimationEditor : UnityEditor.PropertyDrawer
{
    private const string Green = "Green: Animation will end within 0.1% of the target value";
    private const string Yellow = "Yellow: Animation will end within 1% of the target value";
    private const string Red = "Red: Animation will end more than 1% off the target value";

    private static readonly string Tooltip = $"The curve is generated by the spring animation and is not directly editable.\n\nColor Key:\n{Green}\n{Yellow}\n{Red}";
    private static readonly GUIContent PreviewLabel = UnityEditor.EditorGUIUtility.TrTextContent("Spring Preview", Tooltip);

    public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
    {
        return !property.isExpanded ? UnityEditor.EditorGUIUtility.singleLineHeight : 8 * UnityEditor.EditorGUIUtility.singleLineHeight + 4 * UnityEditor.EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        position.height = UnityEditor.EditorGUIUtility.singleLineHeight;

        UnityEditor.EditorGUI.BeginChangeCheck();
        bool foldout = UnityEditor.EditorGUI.Foldout(position, property.isExpanded, label);
        if (UnityEditor.EditorGUI.EndChangeCheck())
        {
            property.isExpanded = foldout;
            property.serializedObject.ApplyModifiedProperties();
        }

        if (!foldout)
        {
            return;
        }

        using (new UnityEditor.EditorGUI.IndentLevelScope(1))
        {
            UnityEditor.SerializedProperty prop = property.Copy();

            foreach (UnityEditor.SerializedProperty p in prop)
            {
                position.y += position.height + UnityEditor.EditorGUIUtility.standardVerticalSpacing;
                UnityEditor.EditorGUI.PropertyField(position, p);
            }

            position.y += position.height + UnityEditor.EditorGUIUtility.standardVerticalSpacing;

            position.height *= 5;

            SpringCurve spring = Deserialize(property);
            float distance = Mathf.Abs(spring.GetPosition(1) - 1);
            Color color = distance <= 0.001f ? Color.green : distance <= 0.01f ? Color.yellow : Color.red;
            Rect ranges = new Rect(Vector2.zero, new Vector2(1f, spring.OvershootPercent + 1.01f));
            UnityEditor.EditorGUI.CurveField(position, PreviewLabel, (AnimationCurve)spring, color, ranges);
        }
    }

    private static SpringCurve Deserialize(UnityEditor.SerializedProperty property)
    {
        float oscillations = property.FindPropertyRelative(nameof(SpringCurve.Oscillations)).floatValue;
        float overshoot = property.FindPropertyRelative(nameof(SpringCurve.OvershootPercent)).floatValue;

        return new SpringCurve()
        {
            OvershootPercent = overshoot,
            Oscillations = oscillations,
        };
    }
}
#endif