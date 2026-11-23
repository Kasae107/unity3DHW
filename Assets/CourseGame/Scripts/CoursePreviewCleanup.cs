using UnityEngine;

namespace CourseGame
{
    public sealed class CoursePreviewCleanup : MonoBehaviour
    {
        void Awake()
        {
            foreach(var go in GameObject.FindGameObjectsWithTag("EditorOnly")) go.SetActive(false);
        }
    }
}
