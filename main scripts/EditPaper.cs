using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;
using PassthroughCameraSamples;

public class EditPaper : MonoBehaviour
{
    public FrameCapture frameCapture;
    // public ImageProjector projector;
    public GameObject paperPlane;
    public Texture2D sourceOutlineTexture;
    public TextMeshProUGUI m_debugText;
    public int projectedWidth = 1024;
    public int projectedHeight = 1024;

    private MeshRenderer planeRenderer;
    // Start is called before the first frame update
    void Start()
    {
        planeRenderer = paperPlane.GetComponent<MeshRenderer>();
        m_debugText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        // m_debugText.text = "Update called";
        Mat captured_frame = frameCapture.CaptureFrame();
        // m_debugText.text = "Capture frame completed";
        List<Point> aruco_points = SketchUtils.DetectMarkers(captured_frame);
        // if (aruco_points.Count != 4){
        //     m_debugText.text = "Not Four markers detected\n";
        //     return;
        // } 
        // else {
        //     m_debugText.text = "Four markers detected\n";
        // }
        // clear or prepend so you don’t endlessly append old data
        // m_debugText.text = "Detected ArUco corners:\n";

        // for (int i = 0; i < aruco_points.Count; i++)
        // {
        //     Point p = aruco_points[i];
        //     m_debugText.text += $"  Corner {i}: x={p.x:F2}, y={p.y:F2}\n";
        // }

        // paper world scale
        Vector3 worldScale = paperPlane.transform.lossyScale;
        float paperLength = worldScale.x;
        // float paperHeight = worldScale.y;

        // These values are in meters
        var objPts = new MatOfPoint3f(
            new Point3(0.05f, 0.05f, 0), 
            new Point3(0.1659f, 0.05f, 0), 
            new Point3(0.1659f, 0.1659f, 0), 
            new Point3(0.05f, 0.1659f, 0)
        );
        // var objPts = new MatOfPoint3f(
        //     new Point3(-paperLength/2, paperLength/2, 0), 
        //     new Point3(paperLength/2, paperLength/2, 0), 
        //     new Point3(paperLength/2, -paperLength/2, 0), 
        //     new Point3(-paperLength/2, -paperLength/2, 0) 
        // );
        var imgPts = new MatOfPoint2f(aruco_points.ToArray());

        Mat intrinsic_mat = frameCapture.GetIntrinsicMat();
        // Passthrough API supposedly undistorts original feed
        MatOfDouble distCoeffs = new MatOfDouble(new double[]{ 0, 0, 0, 0, 0 });

        Mat rvec = new Mat(), tvec = new Mat();

        Calib3d.solvePnP(objPts, imgPts, intrinsic_mat, distCoeffs, rvec, tvec/*, false, Calib3d.SOLVEPNP_IPPE_SQUARE*/);
        // solvePnp gives object to camera

        // Convert rvec → rotation matrix
        Mat rotMat = new Mat();
        Calib3d.Rodrigues(rvec, rotMat);

        // Build a Unity Matrix4x4, inserting the CV rotMat
        var M = Matrix4x4.identity;
        M.m00 = (float)rotMat.get(0,0)[0];
        M.m01 = (float)rotMat.get(0,1)[0];
        M.m02 = (float)rotMat.get(0,2)[0];
        M.m10 = (float)rotMat.get(1,0)[0];
        M.m11 = (float)rotMat.get(1,1)[0];
        M.m12 = (float)rotMat.get(1,2)[0];
        M.m20 = (float)rotMat.get(2,0)[0];
        M.m21 = (float)rotMat.get(2,1)[0];
        M.m22 = (float)rotMat.get(2,2)[0];

        // Adjust for coord‑system flip (Unity’s Y is up, OpenCV’s is down)
        var cvToUnity = Matrix4x4.Scale(new Vector3(1, -1, 1)); // TODO: Check these
        M = cvToUnity * M * cvToUnity;
        // Extract Quaternion
        Quaternion poseRot = Quaternion.LookRotation(
            M.GetColumn(2),  // forward
            M.GetColumn(1)); // upward

        // Build Unity position: flip Y from tvec
        Vector3 posePos = new Vector3(
            (float)tvec.get(0,0)[0],
        -(float)tvec.get(1,0)[0],
            (float)tvec.get(2,0)[0]
        );

        // Rely on left eye pose to change position
        Pose leftEyePose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);

        // compute world position: start at the eye, then offset by your tvec (rotated into world)
        Vector3 worldPos = leftEyePose.position + leftEyePose.rotation * posePos;

        // compute world rotation: first the eye’s world rotation, then your marker orientation
        Quaternion worldRot = leftEyePose.rotation * poseRot;
        worldRot = worldRot * Quaternion.Euler(-90f, 0f, 0f); // account for plane rotation frame
        

        // manual offset for paper
        Vector3 centerOffset = new Vector3( 0.11f, 0, -0.11f );
        worldPos += worldRot * centerOffset;

        worldRot = worldRot * Quaternion.Euler(0f, 180f, 0f);

        // finally, set the paperPlane in world coordinates
        paperPlane.transform.position = worldPos;
        paperPlane.transform.rotation = worldRot;

        

        // m_debugText.text += $"\nWorld Position: {worldPos}\nWorld Rotation: {worldRot.eulerAngles}";

        if (planeRenderer != null)
        {
            planeRenderer.material.mainTexture = sourceOutlineTexture;
        }



        // 2D-Implementation w/ Homography, try this if 3D method does not work
        // List<Point> image_points = new List<Point> {
        //     new Point(0, 0),         // Top-left
        //     new Point(1024, 0),      // Top-right
        //     new Point(1024, 1024),   // Bottom-right
        //     new Point(0, 1024)       // Bottom-left
        // };
        // Mat homography = SketchUtils.ComputeHomography(image_points, aruco_points);

        // Texture2D projectedTexture = projector.ProjectImage(homography);

        // if (planeRenderer != null)
        // {
        //     planeRenderer.material.mainTexture = projectedTexture;
        // }
    }
}
