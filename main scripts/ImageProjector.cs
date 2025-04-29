using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

public class ImageProjector : MonoBehaviour
{
    public Texture2D sourceOutlineTexture;
    public int projectedWidth = 1024;
    public int projectedHeight = 1024;

    public Texture2D ProjectImage(Mat homography)
    {
        // Convert Texture2D to Mat
        Mat sourceMat = new Mat(sourceOutlineTexture.height, sourceOutlineTexture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(sourceOutlineTexture, sourceMat);

        // Prepare an output Mat
        Mat projectedMat = new Mat(projectedHeight, projectedWidth, sourceMat.type());

        // Warp the source image using the computed homography
        Imgproc.warpPerspective(sourceMat, projectedMat, homography, new Size(projectedWidth, projectedHeight));

        // Convert result back to Texture2D
        Texture2D projectedTexture = new Texture2D(projectedMat.width(), projectedMat.height(), TextureFormat.RGB24, false);
        Utils.matToTexture2D(projectedMat, projectedTexture);
        return projectedTexture;
    }
}
