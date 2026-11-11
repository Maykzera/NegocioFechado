using UnityEngine;

/// <summary>Guarda as poses de pé e sentada de um personagem com esqueleto (sem animação).</summary>
public class CharacterPoses : MonoBehaviour
{
    [SerializeField] private Transform hips;
    [SerializeField] private Vector3 standingHips;
    [SerializeField] private Vector3 seatedHips;
    [SerializeField] private Transform[] bones;
    [SerializeField] private Quaternion[] standing;
    [SerializeField] private Quaternion[] seated;

    public void Setup(Transform hipsBone, Vector3 standingHipsPos, Vector3 seatedHipsPos,
        Transform[] poseBones, Quaternion[] standingPose, Quaternion[] seatedPose)
    {
        hips = hipsBone;
        standingHips = standingHipsPos;
        seatedHips = seatedHipsPos;
        bones = poseBones;
        standing = standingPose;
        seated = seatedPose;
    }

    public void Apply(bool sit)
    {
        if (hips != null) hips.localPosition = sit ? seatedHips : standingHips;
        var pose = sit ? seated : standing;
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = pose[i];
    }
}
