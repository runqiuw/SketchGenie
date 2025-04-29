using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.Calib3dModule;

public static class SketchUtils
{
    public static Mat ComputeHomography(List<Point> srcPoints, List<Point> dstPoints)
    {
        // Convert arrays of points to MatOfPoint2f
        MatOfPoint2f srcMat = new MatOfPoint2f(srcPoints.ToArray());
        MatOfPoint2f dstMat = new MatOfPoint2f(dstPoints.ToArray());

        // Compute the homography matrix
        Mat homography = Calib3d.findHomography(srcMat, dstMat);
        return homography;
    }

    // Returns sorted list of dest corners, top left, top right, bottom right, bottom left
    public static List<Point> DetectMarkers(Mat inputMat)
    {
        // Use the predefined dictionary from the Objdetect class
        Dictionary dictionary = Objdetect.getPredefinedDictionary(Objdetect.DICT_4X4_50);

        ArucoDetector detector = new ArucoDetector(dictionary); // TODO: Add detectorparams if not working well    
        // Prepare containers for output
        List<Mat> markerCorners = new List<Mat>(); // order of corners is clockwise
        Mat markerIds = new Mat();

        detector.detectMarkers(inputMat, markerCorners, markerIds);

        List<Point> output_list = new List<Point> {
            new Point(0, 0),
            new Point(0, 0),
            new Point(0, 0),
            new Point(0, 0)
        };

        // tested that get is correct order
        for (int i = 0; i < markerIds.total(); ++i) {
            double[] idArr = markerIds.get(i, 0);
            int current_id = (int) idArr[0];

            Mat curr_corners = markerCorners[i];
            Point markerPoint = null;
            if (current_id == 0) { // get bottom right
                double[] bottomRightArr = curr_corners.get(0, 2);
                markerPoint = new Point(bottomRightArr[0], bottomRightArr[1]);
            } else if (current_id == 1) { // get bottom left
                double[] bottomLeftArr = curr_corners.get(0, 3);
                markerPoint = new Point(bottomLeftArr[0], bottomLeftArr[1]);
            } else if (current_id == 2) { // get top left
                double[] topLeftArr = curr_corners.get(0, 0);
                markerPoint = new Point(topLeftArr[0], topLeftArr[1]);
            } else { // id is 3, get top right
                double[] topRightArr = curr_corners.get(0, 1);
                markerPoint = new Point(topRightArr[0], topRightArr[1]);
            }
            output_list[current_id] = markerPoint;

            Debug.Log("Marker " + current_id + " found");
        }

        if (markerIds.total() != 4) {
            return new List<Point>();
        }

        return output_list;
    }
}
