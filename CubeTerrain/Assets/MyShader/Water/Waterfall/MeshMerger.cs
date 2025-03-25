using UnityEngine;

public class MeshMerger : MonoBehaviour
{
    public MeshFilter[] meshFilters; // 병합할 MeshFilter 배열
    public bool mergeSubMeshes = true; // 서브 메시 포함 여부

    void Start()
    {
        MergeMeshes();
    }

    void MergeMeshes()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = gameObject.AddComponent<MeshRenderer>();
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, mergeSubMeshes);

        mf.mesh = mesh;

        // 병합된 메시만 남기고 기존 메시 삭제
        foreach (MeshFilter m in meshFilters)
        {
            Destroy(m.gameObject);
        }
    }
}
