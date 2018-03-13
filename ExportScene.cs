using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 导处场景到.obj文件（包括物件和地形），支持局部地形到处和自动裁剪
/// 
/// author by monitor1394@gmail.com
/// 
/// </summary>
public class ExportScene : ScriptableObject
{
    private const string CUT_LB_OBJ_PATH = "export/bound_lb";
    private const string CUT_RT_OBJ_PATH = "export/bound_rt";

    private static float autoCutMinX = 1000;
    private static float autoCutMaxX = 0;
    private static float autoCutMinY = 1000;
    private static float autoCutMaxY = 0;

    private static float cutMinX = 0;
    private static float cutMaxX = 0;
    private static float cutMinY = 0;
    private static float cutMaxY = 0;

    [MenuItem("ExportScene/ExportSceneToObj")]
    public static void Export()
    {
        ExportSceneToObj(false);
    }

    [MenuItem("ExportScene/ExportSceneToObj(AutoCut)")]
    public static void ExportAutoCut()
    {
        ExportSceneToObj(true);
    }

    public static void ExportSceneToObj(bool autoCut)
    {
        int vertexOffset = 0;
        StringBuilder sb = new StringBuilder();
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        GameObject[] objs = UnityEngine.Object.FindObjectsOfType<GameObject>();
        UpdateCutRect(autoCut);
        foreach (GameObject obj in objs)
        {
            if (obj.transform.parent == null) // root obj
            {
                MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
                if (mfs == null || mfs.Length <= 0)
                {
                    continue;
                }
                foreach (var mf in mfs)
                {
                    if (IsInCutRect(mf.gameObject))
                    {
                        vertexOffset += ExportMeshToObj(sb, mf, vertexOffset);
                    }
                }
            }
        }
        if (terrain)
        {
            vertexOffset += ExportTerrianToObj(terrain.terrainData, 
                terrain.GetPosition(), 
                sb, vertexOffset, autoCut);
        }
        SaveObjToFile(sb.ToString(), 
            Application.dataPath +"/obj/"+ (autoCut?"/scene(autoCut).obj": "/scene.obj"));
    }

    private static void UpdateCutRect(bool autoCut)
    {
        cutMinX = cutMaxX = cutMinY = cutMaxY = 0;
        if (!autoCut)
        {
            Vector3 lbPos = GetObjPos(CUT_LB_OBJ_PATH);
            Vector3 rtPos = GetObjPos(CUT_RT_OBJ_PATH);
            cutMinX = lbPos.x;
            cutMaxX = rtPos.x;
            cutMinY = lbPos.z;
            cutMaxY = rtPos.z;
        }
    }

    private static bool IsInCutRect(GameObject obj)
    {
        if (cutMinX == 0 && cutMaxX == 0 && cutMinY == 0 && cutMaxY == 0) return true;
        Vector3 pos = obj.transform.position;
        if (pos.x >= cutMinX && pos.x <= cutMaxX && pos.z >= cutMinY && pos.z <= cutMaxY) return true;
        else return false;
    }

    private static void SaveObjToFile(string objInfo, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.Write(objInfo);
        }
        Debug.Log("ExportSceneToObj SUCCESS:" + path);
    }

    private static int ExportMeshToObj(StringBuilder sb, MeshFilter mf, int vertexOffset)
    {
        Mesh mesh = mf.sharedMesh;
        foreach (Vector3 vertice in mesh.vertices)
        {
            Vector3 v = mf.transform.TransformPoint(vertice);
            UpdateAutoCutRect(v);
            sb.AppendFormat("v {0:f1} {1:f1} {2:f1}\n", -v.x, v.y, v.z);
        }
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                sb.AppendFormat("f {1} {0} {2}\n", 
                    triangles[j] + 1 + vertexOffset, 
                    triangles[j + 1] + 1 + vertexOffset, 
                    triangles[j + 2] + 1 + vertexOffset);
            }
        }
        return mesh.vertices.Length;
    }

    private static void UpdateAutoCutRect(Vector3 v)
    {
        if (v.x < autoCutMinX) autoCutMinX = v.x;
        if (v.x > autoCutMaxX) autoCutMaxX = v.x;
        if (v.z < autoCutMinY) autoCutMinY = v.z;
        if (v.z > autoCutMaxY) autoCutMaxY = v.z;
    }

    private static int ExportTerrianToObj(TerrainData terrain, Vector3 terrainPos, 
        StringBuilder sb, int vertexOffset, bool autoCut)
    {
        int tw = terrain.heightmapWidth;
        int th = terrain.heightmapHeight;

        Vector3 meshScale = terrain.size;
        meshScale = new Vector3(meshScale.x / (tw - 1), meshScale.y, meshScale.z / (th - 1));

        Vector2 terrainBoundLB, terrainBoundRT;
        if (autoCut)
        {
            terrainBoundLB = GetTerrainBoundPos(new Vector3(autoCutMinX, 0, autoCutMinY), terrain, terrainPos);
            terrainBoundRT = GetTerrainBoundPos(new Vector3(autoCutMaxX, 0, autoCutMaxY), terrain, terrainPos);
        }
        else
        {
            terrainBoundLB = GetTerrainBoundPos(CUT_LB_OBJ_PATH, terrain, terrainPos);
            terrainBoundRT = GetTerrainBoundPos(CUT_RT_OBJ_PATH, terrain, terrainPos);
        }

        int bw = (int)(terrainBoundRT.x - terrainBoundLB.x);
        int bh = (int)(terrainBoundRT.y - terrainBoundLB.y);

        int w = bh != 0 && bh < th ? bh : th;
        int h = bw != 0 && bw < tw ? bw : tw;

        int startX = (int)terrainBoundLB.y;
        int startY = (int)terrainBoundLB.x;
        if (startX < 0) startX = 0;
        if (startY < 0) startY = 0;

        Debug.Log(string.Format("Terrian:tw={0},th={1},sw={2},sh={3},startX={4},startY={5}",
            tw, th, bw, bh, startX, startY));

        float[,] tData = terrain.GetHeights(0, 0, tw, th);
        Vector3[] tVertices = new Vector3[w * h];
        int[] tPolys = new int[(w - 1) * (h - 1) * 6];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector3 pos = new Vector3(-(startY + y), tData[startX + x, startY + y], (startX + x));
                tVertices[y * w + x] = Vector3.Scale(meshScale, pos) + terrainPos;
            }
        }
        int index = 0;
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                tPolys[index++] = (y * w) + x;
                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = (y * w) + x + 1;
                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = ((y + 1) * w) + x + 1;
                tPolys[index++] = (y * w) + x + 1;
            }
        }
        for (int i = 0; i < tVertices.Length; i++)
        {
            sb.AppendFormat("v {0:f1} {1:f1} {2:f1}\n", tVertices[i].x, tVertices[i].y, tVertices[i].z);
        }
        for (int i = 0; i < tPolys.Length; i += 3)
        {
            int x = tPolys[i] + 1 + vertexOffset;
            int y = tPolys[i + 1] + 1 + vertexOffset;
            int z = tPolys[i + 2] + 1 + vertexOffset;
            sb.AppendFormat("f {0} {1} {2}\n", x, y, z);
        }
        return tVertices.Length;
    }

    private static Vector2 GetTerrainBoundPos(string path, TerrainData terrain, Vector3 terrainPos)
    {
        var go = GameObject.Find(path);
        if (go)
        {
            Vector3 pos = go.transform.position;
            return GetTerrainBoundPos(pos, terrain, terrainPos);
        }
        return Vector2.zero;
    }

    private static Vector2 GetTerrainBoundPos(Vector3 worldPos, TerrainData terrain, Vector3 terrainPos)
    {
        Vector3 tpos = worldPos - terrainPos;
        return new Vector2((int)(tpos.x / terrain.size.x * terrain.heightmapWidth),
            (int)(tpos.z / terrain.size.z * terrain.heightmapHeight));
    }

    private static Vector3 GetObjPos(string path)
    {
        var go = GameObject.Find(path);
        if (go)
        {
            return go.transform.position;
        }
        return Vector3.zero;
    }
}
