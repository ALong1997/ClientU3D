using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Camera_Test : MonoBehaviour
{
    public Camera m_Camera;

    private Vector3 m_CameraNewPos;         //相机的新坐标

    private Vector3 m_FingerPrePos;         //上一帧屏幕坐标
    private Vector3 m_FingerNowPos;         //本帧屏幕坐标

    private Vector3 m_WorldPrePos;          //上一帧世界坐标
    private Vector3 m_WorldNowPos;          //本帧世界坐标

    private Vector3 m_PreVec;               //相机到上一帧世界坐标的向量
    private Vector3 m_NowVec;               //相机到本帧世界坐标的向量

    private Vector3 m_TransVec;             //辅助向量，m_NowVec延长/缩短至与m_PreVec的Y相等
    private Vector3 m_DragVec;              //辅助向量，为了构建相似三角形，得到与地面平行的向量
    private Vector3 m_DragOffset;           //相机需要移动的偏移量



    //四个顶点坐标
    private Vector2 m_Point1 = new Vector2(50, 50);
    private Vector2 m_Point2 = new Vector2(50, -50);
    private Vector2 m_Point3 = new Vector2(-50, -50);
    private Vector2 m_Point4 = new Vector2(-50, 50);
    //计算用到的四条边
    private Vector2 m_vecEdge12 = new Vector2(0, -100);
    private Vector2 m_vecEdge23 = new Vector2(-100, 0);
    private Vector2 m_vecEdge34 = new Vector2(0, 100);
    private Vector2 m_vecEdge41 = new Vector2(100, 0);
    //摄像机映射到地平面上的点与平面范围的关系
    private enum EPOINTWITHRECTANGlLE
    {
        eIN     = 1,        //表示点在矩形内
        eOUT    = 2,        //表示同时在四条边外侧
        eOUT1   = 3,        //表示点在边12外侧
        eOUT2   = 4,        //表示点在边23外侧
        eOUT3   = 5,        //表示点在边34外侧
        eOUT4   = 6,        //表示点在边41外侧
    }



    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_FingerNowPos = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            CameraDrag();
        }
    }

    /// <summary>
    /// 单点触控拖动屏幕
    /// </summary>
    private void CameraDrag()
    {
        m_FingerPrePos = m_FingerNowPos;
        m_FingerNowPos = Input.mousePosition;
        m_WorldPrePos = ScreenToWorldPoint(m_FingerPrePos);
        m_WorldNowPos = ScreenToWorldPoint(m_FingerNowPos);

        m_PreVec = m_WorldPrePos - m_Camera.transform.position;
        m_NowVec = m_WorldNowPos - m_Camera.transform.position;

        m_TransVec = SimilarTriangles(m_PreVec.y, m_NowVec.y, m_NowVec);
        m_DragVec = m_TransVec - m_PreVec;

        if (Mathf.Abs(m_DragVec.x) > 0.01f || Mathf.Abs(m_DragVec.z) > 0.01f)
        {
            m_DragOffset = SimilarTriangles(m_Camera.transform.position.y, m_PreVec.y, m_DragVec);
            if (m_DragOffset.sqrMagnitude > 1e-4)
            {
                m_CameraNewPos = m_Camera.transform.position - new Vector3(m_DragOffset.x, 0f, m_DragOffset.z);
                m_Camera.transform.position = CameraDragWithScope(m_CameraNewPos);
            }
        }
    }


    //======================================================== 接口 ========================================================//

    /// <summary>
    /// 屏幕坐标转换为世界坐标
    /// </summary>
    /// <param name="screenPos">屏幕坐标</param>
    /// <returns>转换后的世界坐标</returns>
    private Vector3 ScreenToWorldPoint(Vector2 screenPos)
    {
        return m_Camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, m_Camera.farClipPlane));
    }

    /// <summary>
    /// 相似三角形等比计算
    /// </summary>
    /// <param name="a">待求向量对应的边</param>
    /// <param name="b">vec对应的边</param>
    /// <param name="vec"></param>
    /// <returns>待求向量</returns>
    private Vector3 SimilarTriangles(float a, float b, Vector3 vec)
    {
        return Mathf.Abs(a / b) * vec;
    }

    /// <summary>
    /// 求二维向量的向量积
    /// </summary>
    /// <param name="a">向量a</param>
    /// <param name="b">向量b</param>
    /// <returns>a×b</returns>
    private float CrossProduct(Vector2 a, Vector2 b)
    {
        return (a.x * b.y) - (a.y * b.x);
    }

    /// <summary>
    /// 判断相机拖动后与地面边界的关系，若超出范围需相应沿着边界移动
    /// </summary>
    /// <param name="CameraPos">相机拖动后的位置</param>
    /// <returns>EPOINTWITHRECTANGlLE</returns>
    private Vector3 CameraDragWithScope(Vector3 CameraPos)
    {
        Vector3 vecMapWorldPos = CameraToPlanePos(CameraPos);
        Vector2 vecMapPos = new Vector2(vecMapWorldPos.x, vecMapWorldPos.z);
        switch (PointWithRectangle(vecMapPos))
        {
            case EPOINTWITHRECTANGlLE.eIN:
                {
                    break;
                }
            case EPOINTWITHRECTANGlLE.eOUT:
                {
                    CameraPos = m_Camera.transform.position;
                    break;
                }
            case EPOINTWITHRECTANGlLE.eOUT1:
                {
                    CameraPos = PointOutEdgeAmendPos(vecMapPos, m_Point1, m_vecEdge12, CameraPos);
                    break;
                }
            case EPOINTWITHRECTANGlLE.eOUT2:
                {
                    CameraPos = PointOutEdgeAmendPos(vecMapPos, m_Point2, m_vecEdge23, CameraPos);
                    break;
                }
            case EPOINTWITHRECTANGlLE.eOUT3:
                {
                    CameraPos = PointOutEdgeAmendPos(vecMapPos, m_Point3, m_vecEdge34, CameraPos);
                    break;
                }
            case EPOINTWITHRECTANGlLE.eOUT4:
                {
                    CameraPos = PointOutEdgeAmendPos(vecMapPos, m_Point4, m_vecEdge41, CameraPos);
                    break;
                }
            default: break;
        }
        return CameraPos;
    }

    /// <summary>
    /// 判断同一平面内点与矩形的关系,"夹线法"和"距离"。
    /// </summary>
    /// <param name="point">由摄像机映射到地平面上的点的的x和z组成的二维坐标</param>
    /// <returns>EPOINTWITHRECTANGlLE</returns>
    private EPOINTWITHRECTANGlLE PointWithRectangle(Vector2 point)
    {
        float CrossProduct12 = CrossProduct(m_vecEdge12, point - m_Point1);
        float CrossProduct23 = CrossProduct(m_vecEdge23, point - m_Point2);
        float CrossProduct34 = CrossProduct(m_vecEdge34, point - m_Point3);
        float CrossProduct41 = CrossProduct(m_vecEdge41, point - m_Point4);

        //Edge1234为true表示点在边12，34内侧,Edge2341同理
        bool Edge1234 = CrossProduct12 * CrossProduct34 > 0;
        bool Edge2341 = CrossProduct23 * CrossProduct41 > 0;

        EPOINTWITHRECTANGlLE ePointStatus;
        if (Edge1234 && Edge2341) ePointStatus = EPOINTWITHRECTANGlLE.eIN;
        else if (!Edge1234 && !Edge2341) ePointStatus = EPOINTWITHRECTANGlLE.eOUT;
        else if (Edge1234)
        {
            if (Mathf.Abs(CrossProduct23) < Mathf.Abs(CrossProduct41)) ePointStatus = EPOINTWITHRECTANGlLE.eOUT2;
            else ePointStatus = EPOINTWITHRECTANGlLE.eOUT4;
        }
        else
        {
            if (Mathf.Abs(CrossProduct12) < Mathf.Abs(CrossProduct34)) ePointStatus = EPOINTWITHRECTANGlLE.eOUT1;
            else ePointStatus = EPOINTWITHRECTANGlLE.eOUT3;
        }
        return ePointStatus;
    }

    /// <summary>
    /// 边界移动相机坐标修正:若point在边界内,则先修正到边界上;若point在边界上，则沿边界移动。
    /// </summary>
    /// <param name="point">由摄像机映射到地平面上的点的的x和z组成的二维坐标</param>
    /// <param name="vecFrom">边对应的起点</param>
    /// <param name="vecEdge">边对应的向量</param>
    /// <param name="CameraPos">相机新坐标</param>
    /// <returns></returns>
    private Vector3 PointOutEdgeAmendPos(Vector2 point, Vector2 vecFrom, Vector2 vecEdge, Vector3 CameraPos)
    {
        Vector2 vecOut = point - vecFrom;
        float length = Vector2.Dot(vecOut, vecEdge) / vecEdge.magnitude;
        Vector2 vecOutInEdge = length * vecEdge.normalized;
        Vector2 vecBoundaryAmend = vecOutInEdge - vecOut;

        Vector3 vecMapWorldPos = CameraToPlanePos(m_Camera.transform.position);
        Vector2 vecMapPos = new Vector2(vecMapWorldPos.x, vecMapWorldPos.z);
        if (EPOINTWITHRECTANGlLE.eIN == PointWithRectangle(vecMapPos))
        {
            Vector2 vecMove = point - vecMapPos;
            vecBoundaryAmend = vecMove * (vecBoundaryAmend.y / vecMove.y);
        }

        Vector3 vecCameraAmend = new Vector3 (vecBoundaryAmend.x, 0, vecBoundaryAmend.y);
        Vector3 vecCameraAmendPos = vecCameraAmend + CameraPos;
        return vecCameraAmendPos;
    }

    /// <summary>
    /// 某帧相机映射到地面上的坐标
    /// </summary>
    /// <param name="point">某帧相机坐标</param>
    /// <returns></returns>
    private Vector3 CameraToPlanePos(Vector3 point)
    {
        Vector3 vecCameraToPlane = SimilarTriangles(point.y, m_Camera.transform.forward.y, m_Camera.transform.forward);
        Vector3 vecMapWorldPos = vecCameraToPlane + point;
        return vecMapWorldPos;
    }
}
