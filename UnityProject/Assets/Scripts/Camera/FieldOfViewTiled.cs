﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewTiled : MonoBehaviour
{
    public float ViewRadius;
    [Range(0, 360)]
    public float ViewAngle;
    
    public LayerMask ObstacleMask;

    public float MeshResolution;
    public int EdgeResolveIterations;
    public float EdgeDistanceThreshhold;

    public float MaskCutawayDst;

	List<Vector3> gizmos = new List<Vector3>();
	float timeG = 0f;

	public float extend = 1f;
	public Vector3 extendDir;

    public MeshFilter ViewMeshFilter;
    Mesh ViewMesh;

    void Start()
    {
        ViewMesh = new Mesh();
        ViewMesh.name = "View Mesh";
        ViewMeshFilter.mesh = ViewMesh;
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(ViewAngle * MeshResolution);
        float stepAngleSize = ViewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();

        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.z - ViewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            if (i > 0)
            {
                bool edgeDstThreshholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > EdgeDistanceThreshhold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThreshholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }

            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] verticies = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        verticies[0] = Vector3.zero;

        for (int i = 0; i < vertexCount - 1; i++)
        {
            verticies[i + 1] = transform.InverseTransformPoint(viewPoints[i]);// + Vector3.up * maskCutawayDst;

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        ViewMesh.Clear();

        ViewMesh.vertices = verticies;
        ViewMesh.triangles = triangles;
        ViewMesh.RecalculateNormals();

    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < EdgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThreshholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > EdgeDistanceThreshhold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThreshholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);

    }

	ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, ViewRadius, ObstacleMask);
	//	Debug.DrawRay(transform.position, dir * 40f, Color.red, 0.1f);
        if (hit && hit.collider != null)
        {
			Vector3 hitPosition = Vector3.zero;
			hitPosition = hit.point + ((Vector2)dir * 0.5f);
			UnityEngine.Tilemaps.Tilemap tileMap = hit.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>();

			if(tileMap != null){
				tileMap.SetTileFlags(tileMap.WorldToCell(hitPosition), UnityEngine.Tilemaps.TileFlags.None);
				tileMap.SetColor(tileMap.WorldToCell(hitPosition),Color.white);

			}

			return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, transform.position + dir * ViewRadius, ViewRadius, globalAngle);
        }
    }

    public Vector3 DirFromAngle(float angle, bool angleIsGlobal)
    {
        if (angleIsGlobal)
        {
            angle -= transform.eulerAngles.z;
        }

        angle -= transform.eulerAngles.z;

        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0);
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
}