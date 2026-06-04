using UnityEngine;

[ExecuteAlways]
public class MazeIntroPreview : MonoBehaviour
{
    Camera targetCamera;
    MazeCinematicSet previewSet;
    Vector3 originalCameraPosition;
    Quaternion originalCameraRotation;
    int originalCullingMask;
    bool capturedCameraPose;

    void OnEnable()
    {
        if (!Application.isPlaying)
            BuildPreview();
    }

    void OnDisable()
    {
        ClearPreview();
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            ClearPreview();
            return;
        }

        BuildPreview();
    }

    void BuildPreview()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (previewSet != null)
            return;

        if (!capturedCameraPose)
        {
            originalCameraPosition = targetCamera.transform.position;
            originalCameraRotation = targetCamera.transform.rotation;
            originalCullingMask = targetCamera.cullingMask;
            capturedCameraPose = true;
        }

        previewSet = new MazeCinematicSet("EditMode_MazeIntroPreview3D", new Color(0f, 0.95f, 1f));
        previewSet.Root.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        targetCamera.cullingMask = 1 << MazeCinematicSet.CinematicLayer;
        targetCamera.transform.position = new Vector3(0f, 0.68f, -8.6f);
        targetCamera.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
    }

    void ClearPreview()
    {
        if (previewSet != null)
        {
            previewSet.Dispose();
            previewSet = null;
        }

        if (capturedCameraPose && targetCamera != null)
        {
            targetCamera.transform.position = originalCameraPosition;
            targetCamera.transform.rotation = originalCameraRotation;
            targetCamera.cullingMask = originalCullingMask;
            capturedCameraPose = false;
        }
    }
}
