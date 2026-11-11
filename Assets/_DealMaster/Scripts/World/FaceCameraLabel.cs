using UnityEngine;

/// <summary>Faz um texto (nome, balão de fala) sempre olhar para a câmera.</summary>
public class FaceCameraLabel : MonoBehaviour
{
    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
